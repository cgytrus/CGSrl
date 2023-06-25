using PER.Abstractions.Environment;
using PER.Util;

namespace Cgsrl.Shared.Networking.Packets.ServerToClient;

public record ObjectAddedPacket(LevelObject obj) : Packet {
    public const string GlobalId = "objectAdded";

    public void Process(Level level) {
        level.Add(obj);
    }

    public byte[] Serialize() {
        MemoryStream stream = new();
        BinaryWriter writer = new(stream);
        writer.Write(GlobalId);
        writer.Write(obj.GetType().AssemblyQualifiedName ?? "");
        writer.Write(obj.id.ToByteArray());
        writer.Write(obj.layer);
        writer.Write(obj.position.x);
        writer.Write(obj.position.y);
        obj.CustomSerialize(writer);
        return stream.ToArray();
    }

    public static ObjectAddedPacket Deserialize(BinaryReader reader) {
        Type? type = Type.GetType(reader.ReadString());
        if(type is null)
            throw new InvalidDataException();
        if(Activator.CreateInstance(type) is not LevelObject obj)
            throw new InvalidDataException();
        obj.id = new Guid(reader.ReadBytes(16));
        obj.layer = reader.ReadInt32();
        obj.position = new Vector2Int(reader.ReadInt32(), reader.ReadInt32());
        obj.CustomDeserialize(reader);
        return new ObjectAddedPacket(obj);
    }
}
