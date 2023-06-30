using System.Net;

using Cgsrl.Shared.Environment;
using Cgsrl.Shared.Networking;

using Lidgren.Network;

using NLog;

using PER.Abstractions.Environment;
using PER.Util;

namespace Cgsrl.Server.Networking;

public class GameServer {
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    public TimeSpan uptime => _uptimeStopwatch.time;

    private readonly Stopwatch _uptimeStopwatch = new();

    private readonly NetServer _peer;
    private readonly Level<SyncedLevelObject> _level;
    private readonly Commands _commands;

    private readonly List<SyncedLevelObject> _addedObjects = new();
    private readonly List<SyncedLevelObject> _removedObjects = new();
    private readonly List<SyncedLevelObject> _changedObjects = new();

    public GameServer(Level<SyncedLevelObject> level, int port) {
        _level = level;
        _commands = new Commands(this, level);

        NetPeerConfiguration config = new("CGSrl") {
            LocalAddress = IPAddress.Any,
            Port = port,
            EnableUPnP = false
        };

        config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
        config.EnableMessageType(NetIncomingMessageType.StatusChanged);
        config.EnableMessageType(NetIncomingMessageType.Data);
        config.EnableMessageType(NetIncomingMessageType.UnconnectedData);
        config.EnableMessageType(NetIncomingMessageType.ConnectionLatencyUpdated);

#if DEBUG
        config.EnableMessageType(NetIncomingMessageType.DebugMessage);
        config.EnableMessageType(NetIncomingMessageType.VerboseDebugMessage);
#else
        config.DisableMessageType(NetIncomingMessageType.DebugMessage);
        config.DisableMessageType(NetIncomingMessageType.VerboseDebugMessage);
#endif

        config.DisableMessageType(NetIncomingMessageType.Receipt);
        config.DisableMessageType(NetIncomingMessageType.DiscoveryRequest); // enable later
        config.DisableMessageType(NetIncomingMessageType.DiscoveryResponse); // enable later
        config.DisableMessageType(NetIncomingMessageType.NatIntroductionSuccess);

        _peer = new NetServer(config);

        _level.objectAdded += obj => {
            _addedObjects.Add(obj);
        };
        _level.objectRemoved += obj => {
            _removedObjects.Add(obj);
        };
        _level.objectChanged += obj => {
            _changedObjects.Add(obj);
        };

        _peer.Start();
        _uptimeStopwatch.Reset();
        logger.Info("Server running on port {}", port);
    }

    public void Finish() {
        _addedObjects.Clear();
        _removedObjects.Clear();
        _changedObjects.Clear();

        _uptimeStopwatch.Reset();
        _peer.Shutdown("Server closed");
        logger.Info("Server stopped");
    }

    public void ProcessMessages() {
        ProcessObjectUpdates();
        while(_peer.ReadMessage(out NetIncomingMessage msg)) {
            switch(msg.MessageType) {
                case NetIncomingMessageType.WarningMessage:
                    logger.Warn(msg.ReadString);
                    break;
                case NetIncomingMessageType.ErrorMessage:
                    logger.Error(msg.ReadString);
                    break;
                case NetIncomingMessageType.ConnectionApproval:
                    ProcessConnectionApproval(msg);
                    break;
                case NetIncomingMessageType.StatusChanged:
                    ProcessStatusChanged((NetConnectionStatus)msg.ReadByte(), msg.ReadString(), msg.SenderConnection);
                    break;
                case NetIncomingMessageType.ConnectionLatencyUpdated:
                    ProcessConnectionLatencyUpdated(msg.SenderConnection, msg.ReadFloat());
                    break;
                case NetIncomingMessageType.Data:
                    CtsDataType type = (CtsDataType)msg.ReadByte();
                    ProcessData(type, msg);
                    break;
                default:
                    logger.Error("Unhandled message type: {}", msg.MessageType);
                    break;
            }
            _peer.Recycle(msg);
        }
    }

    private void ProcessObjectUpdates() {
        if(_addedObjects.Count == 0 && _removedObjects.Count == 0 && _changedObjects.Count == 0)
            return;

        NetOutgoingMessage msg = _peer.CreateMessage(SyncedLevelObject.PreallocSize);
        msg.Write((byte)StcDataType.ObjectsUpdated);

        msg.Write(_addedObjects.Count);
        foreach(SyncedLevelObject obj in _addedObjects)
            obj.WriteTo(msg);

        msg.Write(_removedObjects.Count);
        foreach(SyncedLevelObject obj in _removedObjects)
            msg.Write(obj.id);

        msg.Write(_changedObjects.Count);
        foreach(SyncedLevelObject obj in _changedObjects) {
            msg.Write(obj.id);
            obj.WriteDynamicDataTo(msg);
        }

        _peer.SendToAll(msg, NetDeliveryMethod.ReliableOrdered, 0);

        _addedObjects.Clear();
        _removedObjects.Clear();
        _changedObjects.Clear();
    }

    private void ProcessConnectionApproval(NetIncomingMessage msg) {
        NetConnection connection = msg.SenderConnection;
        string username = msg.ReadString();
        string displayName = msg.ReadString();
        if(string.IsNullOrEmpty(username)) {
            connection.Deny("Empty username");
            return;
        }
        if(string.IsNullOrEmpty(displayName)) {
            connection.Deny("Empty display name");
            return;
        }
        if(username.Any(c => !char.IsAsciiLetterLower(c) && !char.IsAsciiDigit(c) && c != '_' && c != '-')) {
            connection.Deny("Invalid username: only lowercase letters, digits, _ and - are allowed");
            return;
        }
        if(_level.objects.Values.OfType<PlayerObject>().Any(p => p.username == username)) {
            connection.Deny("Player with this username already exists");
            return;
        }

        connection.Tag = new PlayerObject { connection = connection, username = username, displayName = displayName };

        // hail message can't be too large so send it after the client connects
        connection.Approve();

        logger.Info($"[{username}] connection approved");
        SendChatMessage(null, null, $"\f1{displayName} is joining the game");
    }

    private void ProcessStatusChanged(NetConnectionStatus status, string reason, NetConnection connection) {
        switch(status) {
            case NetConnectionStatus.Connected: {
                if(connection.Tag is not PlayerObject player) {
                    logger.Warn("Tag was not player, ignoring connect!");
                    return;
                }

                int prealloc = 1 + sizeof(int) + _level.objects.Count * SyncedLevelObject.PreallocSize;
                NetOutgoingMessage hail = _peer.CreateMessage(prealloc);
                hail.Write((byte)StcDataType.Joined);
                hail.Write(_level.objects.Count);
                foreach(SyncedLevelObject obj in _level.objects.Values)
                    obj.WriteTo(hail);
                connection.SendMessage(hail, NetDeliveryMethod.ReliableOrdered, 0);

                _level.Add(player);
                logger.Info($"[{player.username}] connected");
                SendChatMessage(null, null, $"{player.displayName} \f2joined the game");
                SendChatMessage(null, player, "welcome :>"); // TODO: chat motd
                break;
            }
            case NetConnectionStatus.Disconnected: {
                if(connection.Tag is not PlayerObject player) {
                    logger.Warn("Tag was not player, ignoring disconnect!");
                    return;
                }
                _level.Remove(player);
                logger.Info($"[{player.username}] disconnected ({reason})");
                SendChatMessage(null, null, $"{player.displayName} \f2left the game \f1({reason})");
                break;
            }
        }
    }

    private static void ProcessConnectionLatencyUpdated(NetConnection connection, float roundTripTime) {
        if(connection.Tag is not PlayerObject player) {
            logger.Warn("Tag was not player, ignoring ping update!");
            return;
        }
        player.ping = roundTripTime / 2f;
    }

    private void ProcessData(CtsDataType type, NetIncomingMessage msg) {
        switch(type) {
            case CtsDataType.AddObject:
                ProcessAddObject(msg);
                break;
            case CtsDataType.RemoveObject:
                ProcessRemoveObject(msg);
                break;
            case CtsDataType.PlayerMove:
                ProcessPlayerMove(msg);
                break;
            case CtsDataType.ChatMessage:
                ProcessChatMessage(msg);
                break;
            default:
                logger.Error("Unhandled CTS data type: {}", type);
                break;
        }
    }

    private void ProcessAddObject(NetBuffer msg) {
        SyncedLevelObject obj = SyncedLevelObject.Read(msg);
        if(_level.objects.ContainsKey(obj.id)) {
            logger.Warn("Object {} (of type {}) already exists, ignoring add object!", obj.id, obj.GetType().Name);
            return;
        }
        _level.Add(obj);
    }

    private void ProcessRemoveObject(NetBuffer msg) {
        Guid id = msg.ReadGuid();
        if(!_level.objects.TryGetValue(id, out SyncedLevelObject? obj)) {
            logger.Warn("Object {} doesn't exist, ignoring remove object!", id);
            return;
        }
        if(obj is PlayerObject) {
            logger.Warn("Object {} is a player, ignoring remove object!", id);
            return;
        }
        _level.Remove(id);
    }

    private static void ProcessPlayerMove(NetIncomingMessage msg) {
        if(msg.SenderConnection.Tag is not PlayerObject player) {
            logger.Warn("Tag was not player, ignoring player move!");
            return;
        }
        player.move = msg.ReadVector2Int();
    }

    private void ProcessChatMessage(NetIncomingMessage msg) {
        if(msg.SenderConnection.Tag is not PlayerObject player) {
            logger.Warn("Tag was not player, ignoring chat message!");
            return;
        }
        double time = msg.ReadTime(false);
        string text = msg.ReadString();
        if(text.StartsWith('/')) {
            string command = text[1..];
            logger.Info($"[{player.username}] Command received: '{command}'");
            SendChatMessage(player, player, $"\f4{command}");
            try { _commands.dispatcher.Execute(command, player); }
            catch(Exception ex) { SendChatMessage(null, player, ErrorMessage(ex.Message)); }
            return;
        }
        NetOutgoingMessage message = _peer.CreateMessage();
        message.Write((byte)StcDataType.ChatMessage);
        message.Write(player.id);
        message.WriteTime(time, false);
        message.Write(text);
        _peer.SendToAll(message, NetDeliveryMethod.ReliableOrdered, 0);
        logger.Info($"[CHAT] [{player.username}] {text}");
    }

    // from == null = SYSTEM
    // to == null = everyone
    public void SendChatMessage(PlayerObject? from, PlayerObject? to, string text) {
        NetOutgoingMessage message = _peer.CreateMessage();
        message.Write((byte)StcDataType.ChatMessage);
        message.Write(from?.id ?? Guid.Empty);
        message.WriteTime(false);
        message.Write(text);
        if(to?.connection is null) {
            logger.Info($"[CHAT] [{from?.username ?? "SYSTEM"}] {text}");
            _peer.SendToAll(message, NetDeliveryMethod.ReliableOrdered, 0);
        }
        else {
            logger.Info($"[CHAT] [{from?.username ?? "SYSTEM"} > {to.username}] {text}");
            to.connection.SendMessage(message, NetDeliveryMethod.ReliableOrdered, 0);
        }
    }

    public static string ErrorMessage(string text) => $"\f3{text}";
}
