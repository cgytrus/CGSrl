using Cgsrl.Shared.Environment;

using PER.Abstractions.Environment;

namespace Cgsrl.Shared.Networking.Packets.ClientToServer;

public record RemoveObjectPacket(Guid objId) : Packet {
    public const string GlobalId = "removeObject";

    public void Process(Level level) {
        // prevent players from removing other players xd
        if(level.objects.TryGetValue(objId, out LevelObject? obj) && obj is not PlayerObject)
            level.Remove(objId);
    }

    public byte[] Serialize() {
        MemoryStream stream = new();
        BinaryWriter writer = new(stream);
        writer.Write(GlobalId);
        writer.Write(objId.ToByteArray());
        return stream.ToArray();
    }

    public static RemoveObjectPacket Deserialize(BinaryReader reader) => new(new Guid(reader.ReadBytes(16)));
}
