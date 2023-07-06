using System;

using CGSrl.Shared.Environment;
using CGSrl.Shared.Networking;

using JetBrains.Annotations;

using Lidgren.Network;

using NLog;

using PER.Abstractions.Audio;
using PER.Abstractions.Input;
using PER.Abstractions.Rendering;
using PER.Abstractions.Resources;
using PER.Util;

using PRR.UI;

namespace CGSrl.Client.Networking;

public class GameClient {
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    [PublicAPI]
    public string username { get; private set; } = "";
    [PublicAPI]
    public string displayName { get; private set; } = "";

    public NetClient peer { get; }

    public event Action? onConnect;
    public event Action? onJoin;
    public event Action<string, bool>? onDisconnect;

    public int totalJoinedObjectCount { get; private set; }
    public int leftJoinedObjectCount { get; private set; }

    public SyncedLevel? level { get; private set; }

    private readonly IRenderer _renderer;
    private readonly IInput _input;
    private readonly IAudio _audio;
    private readonly IResources _resources;

    private readonly ListBox<PlayerObject> _players;
    private readonly ListBox<ChatMessage> _messages;

    private readonly Stopwatch _timer = new();

    private NetIncomingMessage? _joinedMessage;

    private Vector2Int _lastObjPosition;

    public GameClient(IRenderer renderer, IInput input, IAudio audio, IResources resources,
        ListBox<PlayerObject> players, ListBox<ChatMessage> messages) {
        _renderer = renderer;
        _input = input;
        _audio = audio;
        _resources = resources;

        _players = players;
        _messages = messages;

        NetPeerConfiguration config = new("CGSrl");
        config.EnableMessageType(NetIncomingMessageType.StatusChanged);
        config.EnableMessageType(NetIncomingMessageType.Data);

        peer = new NetClient(config);
        peer.Start();
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
    }

    public void Disconnect() {
        if(peer.ConnectionStatus == NetConnectionStatus.Connected)
            peer.Disconnect("Player left");
    }

    public void ProcessMessages(TimeSpan maxTime) {
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
        while(leftJoinedObjectCount > 0 && (maxTime == TimeSpan.Zero || _timer.time - start < maxTime)) {
            ProcessObjectAdded(_joinedMessage);
            leftJoinedObjectCount--;
            if(maxTime == TimeSpan.Zero)
                break;
        }
        if(leftJoinedObjectCount > 0)
            return;
        peer.Recycle(_joinedMessage);
        _joinedMessage = null;
        leftJoinedObjectCount = 0;
        onJoin?.Invoke();
    }

    private void ProcessStatusChanged(NetConnectionStatus status, string reason) {
        switch(status) {
            case NetConnectionStatus.Connected:
                onConnect?.Invoke();
                logger.Info("Connected");
                break;
            case NetConnectionStatus.Disconnected:
                _joinedMessage = null;
                onDisconnect?.Invoke(reason, reason != "Player left");
                level = null;
                logger.Info($"Disconnected ({reason})");
                break;
        }
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
                _players.Add(player);
        };
        level.objectRemoved += obj => {
            if(obj is PlayerObject player)
                _players.Remove(player);
        };

        leftJoinedObjectCount = msg.ReadInt32();
        totalJoinedObjectCount = leftJoinedObjectCount;
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
            _messages.Insert(0, new ChatMessage(null, NetTime.Now, $"\f2{text}"));
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
        Guid id = msg.ReadGuid();
        if(id == Guid.Empty) {
            _messages.Insert(0, new ChatMessage(null, msg.ReadTime(false), msg.ReadString()));
            logger.Info($"[CHAT] [SYSTEM] {_messages[0].text}");
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
        _messages.Insert(0, new ChatMessage(player, msg.ReadTime(false), msg.ReadString()));
        logger.Info($"[CHAT] [{player.username}] {_messages[0].text}");
    }
}
