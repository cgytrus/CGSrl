using Cgsrl.Shared.Environment;

using PER.Util;

namespace Cgsrl.Shared.Networking.Packets.ClientToServer;

public record PlayerMovePacket(Vector2Int move) : Packet {
    public const string GlobalId = "playerMove";

    public void Process(PlayerObject player) {
        player.move = move;
    }

    public byte[] Serialize() {
        MemoryStream stream = new();
        BinaryWriter writer = new(stream);
        writer.Write(GlobalId);
        writer.Write(move.x);
        writer.Write(move.y);
        return stream.ToArray();
    }

    public static PlayerMovePacket Deserialize(BinaryReader reader) =>
        new(new Vector2Int(reader.ReadInt32(), reader.ReadInt32()));
}
