using Cgsrl.Shared.Networking.Packets.ClientToServer;
using Cgsrl.Shared.Networking.Packets.ServerToClient;

namespace Cgsrl.Shared.Networking.Packets;

public abstract record Packet {
    public static Packet Deserialize(byte[] data) {
        MemoryStream stream = new(data);
        BinaryReader reader = new(stream);
        return reader.ReadString() switch {
            // client to server
            PlayerMovePacket.GlobalId => PlayerMovePacket.Deserialize(reader),

            // server to client
            JoinedPacket.GlobalId => JoinedPacket.Deserialize(reader),
            ObjectAddedPacket.GlobalId => ObjectAddedPacket.Deserialize(reader),
            ObjectRemovedPacket.GlobalId => ObjectRemovedPacket.Deserialize(reader),
            ObjectChangedPacket.GlobalId => ObjectChangedPacket.Deserialize(reader),
            _ => throw new InvalidDataException()
        };
    }
}
