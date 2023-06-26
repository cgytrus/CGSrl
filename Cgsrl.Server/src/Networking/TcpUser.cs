using System.Collections.Concurrent;

using Cgsrl.Shared.Environment;
using Cgsrl.Shared.Networking.Packets;
using Cgsrl.Shared.Networking.Packets.ClientToServer;

using JetBrains.Annotations;

using LiteNetwork.Server;

using NLog;

using PER.Abstractions.Environment;

namespace Cgsrl.Server.Networking;

[UsedImplicitly]
public class TcpUser : LiteServerUser {
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    private readonly ConcurrentQueue<Packet> _packets = new();
    private PlayerObject? _player;

    public override Task HandleMessageAsync(byte[] packetBuffer) {
        _packets.Enqueue(Packet.Deserialize(packetBuffer));
        return Task.CompletedTask;
    }

    protected override void OnConnected() {
        logger.Info("connected");
    }

    protected override void OnDisconnected() {
        if(Context?.Options is not TcpServerOptions options || _player is null)
            return;
        options.level.Remove(_player);
        logger.Info($"{_player.displayName} ({_player.username}) disconnected");
    }

    public bool ProcessPackets(Level level) {
        while(_packets.TryDequeue(out Packet? packet)) {
            if(ProcessPacket(level, packet))
                continue;
            _packets.Clear();
            return false;
        }
        return true;
    }

    private bool ProcessPacket(Level level, Packet packet) {
        switch(packet) {
            case AuthorizePacket authorizePacket:
                if(!ProcessAuthorizePacket(level, authorizePacket))
                    return false;
                break;
            case PlayerMovePacket playerMovePacket:
                if(_player is not null)
                    playerMovePacket.Process(_player);
                break;
            case CreateObjectPacket createObjectPacket:
                createObjectPacket.Process(level);
                break;
            case RemoveObjectPacket removeObjectPacket:
                removeObjectPacket.Process(level);
                break;
        }
        return true;
    }

    private bool ProcessAuthorizePacket(Level level, AuthorizePacket authorizePacket) {
        AuthorizePacket.AuthError error = authorizePacket.Process(this, level, out _player);
        switch(error) {
            case AuthorizePacket.AuthError.None:
                logger.Info($"{_player.displayName} ({_player.username}) authorized");
                break;
            case AuthorizePacket.AuthError.EmptyUsername:
                logger.Info($"{authorizePacket.displayName} has an empty username.");
                return false;
            case AuthorizePacket.AuthError.EmptyDisplayName:
                logger.Info($"{authorizePacket.username} has an empty display name.");
                return false;
            case AuthorizePacket.AuthError.InvalidUsername:
                logger.Info($"{authorizePacket.displayName} ({authorizePacket.username}) has an invalid username.");
                return false;
            case AuthorizePacket.AuthError.DuplicateUsername:
                logger.Info($"{authorizePacket.displayName} ({authorizePacket.username}) has a duplicate username.");
                return false;
            default:
                logger.Info($"{_player.displayName} ({_player.username}) not authorized (unknown error)");
                return false;
        }
        return true;
    }
}
