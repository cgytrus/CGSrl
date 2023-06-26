using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

using Cgsrl.Shared.Networking.Packets;
using Cgsrl.Shared.Networking.Packets.ServerToClient;

using LiteNetwork.Client;

using PER.Abstractions.Environment;

namespace Cgsrl.Networking;

public class TcpClient : LiteClient {
    private readonly ConcurrentQueue<Packet> _packets = new();

    public TcpClient(LiteClientOptions options) : base(options) { }

    public override Task HandleMessageAsync(byte[] packetBuffer) {
        _packets.Enqueue(Packet.Deserialize(packetBuffer));
        return Task.CompletedTask;
    }

    public bool ProcessPackets(Level level, [NotNullWhen(false)] out string? error) {
        error = null;
        while(_packets.TryDequeue(out Packet? packet)) {
            switch(packet) {
                case AuthorizeErrorPacket authorizeErrorPacket:
                    error = authorizeErrorPacket.error;
                    _packets.Clear();
                    return false;
                case JoinedPacket joinedPacket:
                    joinedPacket.Process(this, level);
                    break;
                case ObjectAddedPacket objectAddedPacket:
                    objectAddedPacket.Process(level);
                    break;
                case ObjectRemovedPacket objectRemovedPacket:
                    objectRemovedPacket.Process(level);
                    break;
                case ObjectChangedPacket objectChangedPacket:
                    objectChangedPacket.Process(level);
                    break;
            }
        }
        return true;
    }
}
