using System;

using Cgsrl.Shared.Environment;
using Cgsrl.Shared.Networking;

using Lidgren.Network;

using NLog;

using PER.Abstractions.Environment;

using PRR.UI;

namespace Cgsrl.Networking;

public class GameClient {
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    public string username { get; private set; } = "";
    public string displayName { get; private set; } = "";

    public NetClient peer { get; }

    public event Action? onConnect;
    public event Action<string, bool>? onDisconnect;

    private readonly Level<SyncedLevelObject> _level;
    private readonly ListBox<ChatMessage> _messages;

    public GameClient(Level<SyncedLevelObject> level, ListBox<ChatMessage> messages) {
        _level = level;
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

    public void ProcessMessages() {
        while(peer.ReadMessage(out NetIncomingMessage msg)) {
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
                    ProcessData(type, msg);
                    break;
                default:
                    logger.Error("Unhandled message type: {}", msg.MessageType);
                    break;
            }
            peer.Recycle(msg);
        }
    }

    private void ProcessStatusChanged(NetConnectionStatus status, string reason) {
        switch(status) {
            case NetConnectionStatus.Connected:
                onConnect?.Invoke();
                logger.Info("Connected");
                break;
            case NetConnectionStatus.Disconnected:
                onDisconnect?.Invoke(reason, reason != "Player left");
                logger.Info($"Disconnected ({reason})");
                break;
        }
    }

    private void ProcessData(StcDataType type, NetIncomingMessage msg) {
        switch(type) {
            case StcDataType.Joined:
                ProcessJoined(msg);
                break;
            case StcDataType.ObjectAdded:
                ProcessObjectAdded(msg);
                break;
            case StcDataType.ObjectRemoved:
                ProcessObjectRemoved(msg);
                break;
            case StcDataType.ObjectChanged:
                ProcessObjectChanged(msg);
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
        int objCount = msg.ReadInt32();
        for(int i = 0; i < objCount; i++)
            ProcessObjectAdded(msg);
    }

    private void ProcessObjectAdded(NetIncomingMessage msg) {
        SyncedLevelObject obj = SyncedLevelObject.Read(msg);
        if(_level.objects.ContainsKey(obj.id)) {
            logger.Warn("Object {} (of type {}) already exists, ignoring object added!", obj.id, obj.GetType().Name);
            return;
        }
        // if we received a player and it's us, set its connection so it can send messages
        if(obj is PlayerObject player && player.username == username)
            player.connection = msg.SenderConnection;
        _level.Add(obj);
    }

    private void ProcessObjectRemoved(NetBuffer msg) {
        Guid id = msg.ReadGuid();
        if(!_level.objects.ContainsKey(id)) {
            logger.Warn("Object {} doesn't exist, ignoring object removed!", id);
            return;
        }
        _level.Remove(id);
    }

    private void ProcessObjectChanged(NetBuffer msg) {
        Guid id = msg.ReadGuid();
        if(!_level.objects.TryGetValue(id, out SyncedLevelObject? obj)) {
            logger.Warn("Object {} doesn't exist, ignoring object changed!", id);
            return;
        }
        obj.ReadDataFrom(msg);
    }

    private void ProcessChatMessage(NetIncomingMessage msg) {
        Guid id = msg.ReadGuid();
        if(id == Guid.Empty) {
            _messages.Insert(0, new ChatMessage(null, msg.ReadTime(false), msg.ReadString()));
            return;
        }
        if(!_level.objects.TryGetValue(id, out SyncedLevelObject? obj)) {
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
