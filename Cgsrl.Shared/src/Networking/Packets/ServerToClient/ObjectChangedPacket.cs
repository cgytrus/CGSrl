using PER.Abstractions.Environment;
using PER.Util;

namespace Cgsrl.Shared.Networking.Packets.ServerToClient;

public record ObjectChangedPacket(Guid objId, int layer, Vector2Int position, byte[] customData) : Packet {
    public const string GlobalId = "objectChanged";

    private static readonly MemoryStream serializeStream = new();
    private static readonly BinaryWriter writer = new(serializeStream);

    public void Process(Level level) {
        if(!level.objects.TryGetValue(objId, out LevelObject? obj))
            return;
        obj.layer = layer;
        obj.position = position;
        if(customData.Length == 0)
            return;
        MemoryStream stream = new(customData);
        BinaryReader reader = new(stream);
        obj.CustomDeserialize(reader);
    }

    public byte[] Serialize() {
        serializeStream.SetLength(0);
        writer.Write(GlobalId);
        writer.Write(objId.ToByteArray());
        writer.Write(layer);
        writer.Write(position.x);
        writer.Write(position.y);
        writer.Write(customData.Length);
        if(customData.Length > 0)
            writer.Write(customData);
        return serializeStream.ToArray();
    }

    public static ObjectChangedPacket Deserialize(BinaryReader reader) => new(new Guid(reader.ReadBytes(16)),
        reader.ReadInt32(), new Vector2Int(reader.ReadInt32(), reader.ReadInt32()),
        reader.ReadBytes(reader.ReadInt32()));
}
