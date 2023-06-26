using PER.Abstractions.Environment;
using PER.Util;

namespace Cgsrl.Shared.Networking.Packets.ClientToServer;

public record CreateObjectPacket(LevelObject obj) : Packet {
    public const string GlobalId = "createObject";

    public void Process(Level level) {
        if(!level.HasObjectAt(obj.position, obj.GetType()))
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

    public static CreateObjectPacket Deserialize(BinaryReader reader) {
        Type? type = Type.GetType(reader.ReadString());
        if(type is null)
            throw new InvalidDataException();
        if(Activator.CreateInstance(type) is not LevelObject obj)
            throw new InvalidDataException();
        obj.id = new Guid(reader.ReadBytes(16));
        obj.layer = reader.ReadInt32();
        obj.position = new Vector2Int(reader.ReadInt32(), reader.ReadInt32());
        obj.CustomDeserialize(reader);
        return new CreateObjectPacket(obj);
    }
}
