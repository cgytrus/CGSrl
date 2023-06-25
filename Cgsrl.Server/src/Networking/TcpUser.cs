using System.Collections.Concurrent;

using Cgsrl.Shared.Environment;
using Cgsrl.Shared.Networking.Packets;
using Cgsrl.Shared.Networking.Packets.ClientToServer;
using Cgsrl.Shared.Networking.Packets.ServerToClient;

using JetBrains.Annotations;

using LiteNetwork.Server;

using NLog;

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
        if(Context?.Options is not TcpServerOptions options)
            return;
        _player = new PlayerObject { connection = this };
        options.level.Add(_player);
        Send(new JoinedPacket(_player.id, options.level.objects.Count, options.level.objects.Values).Serialize());
    }

    protected override void OnDisconnected() {
        logger.Info("disconnected");
        if(Context?.Options is not TcpServerOptions options)
            return;
        if(_player is not null)
            options.level.Remove(_player);
    }

    public void ProcessPackets() {
        while(_packets.TryDequeue(out Packet? packet)) {
            switch(packet) {
                case PlayerMovePacket playerMovePacket:
                    if(_player is not null)
                        playerMovePacket.Process(_player);
                    break;
            }
        }
    }
}
