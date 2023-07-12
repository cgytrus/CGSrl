using System;
using System.Linq;

using CGSrl.Shared.Environment;
using CGSrl.Shared.Networking;

using JetBrains.Annotations;

using Lidgren.Network;

using NLog;

using PER.Abstractions;
using PER.Abstractions.Audio;
using PER.Abstractions.Input;
using PER.Abstractions.Rendering;
using PER.Abstractions.Resources;
using PER.Util;

using PRR.UI;

namespace CGSrl.Client.Networking;

public class GameClient : IUpdatable {
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    [PublicAPI]
    public string username { get; private set; } = "";
    [PublicAPI]
    public string displayName { get; private set; } = "";

    public NetClient peer { get; }

    public event Action? onConnect;
    public event Action? onJoin;
    public event Action<string, bool>? onDisconnect;

    public SyncedLevel? level { get; private set; }

    [PublicAPI]
    public bool connecting { get; private set; }
    public bool joining { get; private set; }

    private readonly IRenderer _renderer;
    private readonly IInput _input;
    private readonly IAudio _audio;
    private readonly IResources _resources;

    private Text? _loadingText;
    private string _loadingTextFormat = "{0}";
    private ProgressBar? _loadingProgress;
    private ListBox<PlayerObject>? _players;
    private ListBox<ChatMessage>? _messages;

    private readonly Stopwatch _timer = new();

    private NetIncomingMessage? _joinedMessage;
    private int _totalJoinedObjectCount;
    private int _remainingJoinedObjectCount;

    private Vector2Int _lastObjPosition;

    public GameClient(IRenderer renderer, IInput input, IAudio audio, IResources resources) {
        _renderer = renderer;
        _input = input;
        _audio = audio;
        _resources = resources;

        NetPeerConfiguration config = new("CGSrl");
        config.EnableMessageType(NetIncomingMessageType.StatusChanged);
        config.EnableMessageType(NetIncomingMessageType.Data);

        peer = new NetClient(config);
        peer.Start();
    }

    public void SetUi(Text loadingText, string loadingTextFormat, ProgressBar loadingProgress,
        ListBox<PlayerObject>? players, ListBox<ChatMessage>? messages) {
        _loadingText = loadingText;
        _loadingTextFormat = loadingTextFormat;
        _loadingProgress = loadingProgress;
        _players = players;
        _messages = messages;

        loadingText.enabled = true;
        loadingProgress.enabled = true;

        if(players is null || level is null)
            return;
        players.Clear();
        foreach(PlayerObject player in level.objects.OfType<PlayerObject>())
            players.Add(player);
    }

    public void Finish() {
        peer.Shutdown("Player left");
    }

    public void Connect(string host, int port, string username, string displayName) {
        this.username = username;
        this.displayName = displayName;
        NetOutgoingMessage msg = peer.CreateMessage();
        msg.Write(username);
        msg.Write(displayName);
        peer.Connect(host, port, msg);
        connecting = true;
    }

    public void Disconnect() {
        if(peer.ConnectionStatus == NetConnectionStatus.Connected)
            peer.Disconnect("Player left");
    }

    public void Update(TimeSpan time) {
        ProcessMessages(Core.engine.updateInterval > TimeSpan.Zero ? Core.engine.updateInterval :
            Core.engine.frameTime.averageFrameTime);
        if(connecting) {
            UpdateConnectingProgress();
            return;
        }
        if(joining) {
            UpdateJoiningProgress();
            return;
        }
        if(_loadingText is not null) {
            _loadingText.text = "";
            _loadingText.enabled = false;
            _loadingText = null;
        }
        // ReSharper disable once InvertIf
        if(_loadingProgress is not null) {
            _loadingProgress.value = 0f;
            _loadingProgress.enabled = false;
            _loadingProgress = null;
        }
        UpdatePlayerList();
        UpdateChatMessageList(time);
    }

    private void UpdateConnectingProgress() {
        if(_loadingText is null || _loadingProgress is null)
            return;
        string text = peer.ConnectionStatus switch {
            NetConnectionStatus.None or NetConnectionStatus.Disconnected => "Connecting...",
            NetConnectionStatus.InitiatedConnect => "Waiting for response...",
            NetConnectionStatus.ReceivedInitiation => "huh ????",
            NetConnectionStatus.RespondedAwaitingApproval => "Waiting for approval...",
            NetConnectionStatus.RespondedConnect => "huh 2 ???",
            NetConnectionStatus.Connected => "Connected",
            NetConnectionStatus.Disconnecting => "Disconnecting...",
            _ => "Unknow..."
        };
        float progress = peer.ConnectionStatus switch {
            NetConnectionStatus.None or NetConnectionStatus.Disconnected => 0f / 3f,
            NetConnectionStatus.InitiatedConnect => 1f / 3f,
            NetConnectionStatus.RespondedAwaitingApproval => 2f / 3f,
            NetConnectionStatus.Connected => 3f / 3f,
            _ => 0f
        };
        _loadingText.text = string.Format(_loadingTextFormat, text);
        _loadingProgress.value = progress;
    }

    private void UpdateJoiningProgress() {
        if(_loadingText is null || _loadingProgress is null)
            return;
        int received = _totalJoinedObjectCount - _remainingJoinedObjectCount;
        int total = _totalJoinedObjectCount;
        string text = $"Receiving objects... ({received}/{total})";
        _loadingText.text = string.Format(_loadingTextFormat, text);
        float progress = Math.Clamp(received / (float)total, 0f, 1f);
        if(!float.IsNormal(progress))
            progress = 0f;
        _loadingProgress.value = progress;
    }

    private void UpdatePlayerList() {
        if(_players is null)
            return;
        foreach(PlayerObject player in _players.items) {
            if(player.text is not Text text) {
                logger.Warn($"{player.username} doesnt have text????");
                continue;
            }
            player.highlighted = _input.mousePosition.InBounds(text.bounds);
            if(!player.pingDirty)
                continue;
            _players.UpdateItem(player);
            player.pingDirty = false;
        }
    }

    private void UpdateChatMessageList(TimeSpan time) {
        if(_messages is null)
            return;
        for(int i = _messages.items.Count - 1; i >= 0; i--)
            _messages.items[i].Update(time, _input, _messages);
    }

    private void ProcessMessages(TimeSpan maxTime) {
        if(_joinedMessage is not null) {
            AddJoinedObjects(maxTime);
            return;
        }
        TimeSpan start = _timer.time;
        while(peer.ReadMessage(out NetIncomingMessage msg)) {
            ProcessMessage(msg);
            if(_timer.time - start > maxTime)
                break;
        }
    }

    private void ProcessMessage(NetIncomingMessage msg) {
        switch(msg.MessageType) {
            case NetIncomingMessageType.WarningMessage:
                logger.Warn(msg.ReadString());
                break;
            case NetIncomingMessageType.ErrorMessage:
                logger.Error(msg.ReadString());
                break;
            case NetIncomingMessageType.StatusChanged:
                ProcessStatusChanged((NetConnectionStatus)msg.ReadByte(), msg.ReadString());
                break;
            case NetIncomingMessageType.Data:
                StcDataType type = (StcDataType)msg.ReadByte();
                if(type == StcDataType.Joined) {
                    ProcessJoined(msg);
                    return;
                }
                ProcessData(type, msg);
                break;
            default:
                logger.Error("Unhandled message type: {}", msg.MessageType);
                break;
        }
        peer.Recycle(msg);
    }

    private void AddJoinedObjects(TimeSpan maxTime) {
        if(_joinedMessage is null)
            return;
        TimeSpan start = _timer.time;
        while(_remainingJoinedObjectCount > 0 && (maxTime == TimeSpan.Zero || _timer.time - start < maxTime)) {
            ProcessObjectAdded(_joinedMessage);
            _remainingJoinedObjectCount--;
            if(maxTime == TimeSpan.Zero)
                break;
        }
        if(_remainingJoinedObjectCount > 0)
            return;
        peer.Recycle(_joinedMessage);
        _joinedMessage = null;
        _remainingJoinedObjectCount = 0;
        onJoin?.Invoke();
        joining = false;
    }

    private void ProcessStatusChanged(NetConnectionStatus status, string reason) {
        switch(status) {
            case NetConnectionStatus.Connected:
                ProcessConnected();
                break;
            case NetConnectionStatus.Disconnected:
                ProcessDisconnected(reason);
                break;
        }
    }

    private void ProcessConnected() {
        onConnect?.Invoke();
        connecting = false;
        joining = true;
        logger.Info("Connected");
    }

    private void ProcessDisconnected(string reason) {
        _joinedMessage = null;
        _players?.Clear();
        _messages?.Clear();
        onDisconnect?.Invoke(reason, reason != "Player left");
        connecting = false;
        joining = false;
        level = null;
        logger.Info($"Disconnected ({reason})");
    }

    private void ProcessData(StcDataType type, NetIncomingMessage msg) {
        switch(type) {
            case StcDataType.ObjectsUpdated:
                ProcessObjectsUpdated(msg);
                break;
            case StcDataType.ChatMessage:
                ProcessChatMessage(msg);
                break;
            default:
                logger.Error("Unhandled STC data type: {}", type);
                break;
        }
    }
    private void ProcessJoined(NetIncomingMessage msg) {
        Type? gameModeType = Type.GetType(msg.ReadString());
        bool isGameModeType = false;
        for(Type? baseType = gameModeType; baseType is not null; baseType = baseType.BaseType) {
            if(baseType != typeof(SyncedGameMode))
                continue;
            isGameModeType = true;
            break;
        }
        if(!isGameModeType || gameModeType is null ||
            Activator.CreateInstance(gameModeType) is not SyncedGameMode gameMode) {
            logger.Error("Game mode is kinda sus ngl.");
            return;
        }
        Vector2Int chunkSize = msg.ReadVector2Int();
        level = new SyncedLevel(true, _renderer, _input, _audio, _resources, chunkSize, gameMode);
        level.objectAdded += obj => {
            if(obj is PlayerObject player)
                _players?.Add(player);
        };
        level.objectRemoved += obj => {
            if(obj is PlayerObject player)
                _players?.Remove(player);
        };

        _totalJoinedObjectCount = msg.ReadInt32();
        _remainingJoinedObjectCount = _totalJoinedObjectCount;
        _joinedMessage = msg;
    }

    private void ProcessObjectsUpdated(NetIncomingMessage msg) {
        int addedCount = msg.ReadInt32();
        for(int i = 0; i < addedCount; i++)
            ProcessObjectAdded(msg);

        int removedCount = msg.ReadInt32();
        for(int i = 0; i < removedCount; i++)
            ProcessObjectRemoved(msg);

        int changedCount = msg.ReadInt32();
        for(int i = 0; i < changedCount; i++)
            ProcessObjectChanged(msg);
    }

    private void ProcessObjectAdded(NetIncomingMessage msg) {
        if(level is null) {
            logger.Warn("Level was null, ignoring object added!");
            return;
        }
        SyncedLevelObject obj = SyncedLevelObject.Read(msg);
        if(level.objects.ContainsKey(obj.id)) {
            logger.Warn("Object {} (of type {}) already exists, ignoring object added!", obj.id, obj.GetType().Name);
            return;
        }
        // if we received a player and it's us, set its connection so it can send messages
        if(obj is PlayerObject player && player.username == username)
            player.connection = msg.SenderConnection;
        level.Add(obj);
        if(obj is CorruptedObject) {
            obj.position = _lastObjPosition;
            string text = $"Received corrupted object, placing it at {obj.position}!";
            logger.Warn(text);
            _messages?.Insert(0, new ChatMessage(null, NetTime.Now, $"\f2{text}"));
        }
        _lastObjPosition = obj.position;
        level.CheckDirty(obj);
    }

    private void ProcessObjectRemoved(NetBuffer msg) {
        if(level is null) {
            logger.Warn("Level was null, ignoring object removed!");
            return;
        }
        Guid id = msg.ReadGuid();
        if(!level.objects.ContainsKey(id)) {
            logger.Warn("Object {} doesn't exist, ignoring object removed!", id);
            return;
        }
        level.Remove(id);
    }

    private void ProcessObjectChanged(NetBuffer msg) {
        if(level is null) {
            logger.Warn("Level was null, ignoring object changed!");
            return;
        }
        Guid id = msg.ReadGuid();
        if(!level.objects.TryGetValue(id, out SyncedLevelObject? obj)) {
            logger.Warn("Object {} doesn't exist, ignoring object changed!", id);
            return;
        }
        obj.ReadDynamicDataFrom(msg);
        level.CheckDirty(obj);
    }

    private void ProcessChatMessage(NetIncomingMessage msg) {
        if(level is null) {
            logger.Warn("Level was null, ignoring chat message!");
            return;
        }
        ChatMessage message;
        Guid id = msg.ReadGuid();
        if(id == Guid.Empty) {
            message = new ChatMessage(null, msg.ReadTime(false), msg.ReadString());
            _messages?.Insert(0, message);
            logger.Info($"[CHAT] [SYSTEM] {message.text}");
            return;
        }
        if(!level.objects.TryGetValue(id, out SyncedLevelObject? obj)) {
            logger.Warn("Object {} doesn't exist, ignoring chat message!", id);
            return;
        }
        if(obj is not PlayerObject player) {
            logger.Warn("Object {} is not a player, ignoring chat message!", id);
            return;
        }
        message = new ChatMessage(player, msg.ReadTime(false), msg.ReadString());
        _messages?.Insert(0, message);
        logger.Info($"[CHAT] [{player.username}] {message.text}");
    }
}
