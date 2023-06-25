using PER.Abstractions.Environment;

namespace Cgsrl.Shared.Networking.Packets.ServerToClient;

public record ObjectRemovedPacket(Guid objGuid) : Packet {
    public const string GlobalId = "objectRemoved";

    public void Process(Level level) {
        if(level.objects.ContainsKey(objGuid))
            level.Remove(objGuid);
    }

    public byte[] Serialize() {
        MemoryStream stream = new();
        BinaryWriter writer = new(stream);
        writer.Write(GlobalId);
        writer.Write(objGuid.ToByteArray());
        return stream.ToArray();
    }

    public static ObjectRemovedPacket Deserialize(BinaryReader reader) => new(new Guid(reader.ReadBytes(16)));
}
