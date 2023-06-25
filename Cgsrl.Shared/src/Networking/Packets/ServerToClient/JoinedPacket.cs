using Cgsrl.Shared.Environment;

using LiteNetwork;

using PER.Abstractions.Environment;
using PER.Util;

namespace Cgsrl.Shared.Networking.Packets.ServerToClient;

public record JoinedPacket(Guid playerObjId, int objCount, IEnumerable<LevelObject> objs) : Packet {
    public const string GlobalId = "joined";

    public void Process(LiteConnection connection, Level level) {
        foreach(LevelObject obj in objs) {
            level.Add(obj);
            if(obj is PlayerObject player && player.id == playerObjId)
                player.connection = connection;
        }
    }

    public byte[] Serialize() {
        MemoryStream stream = new();
        BinaryWriter writer = new(stream);
        writer.Write(GlobalId);
        writer.Write(playerObjId.ToByteArray());
        writer.Write(objCount);
        foreach(LevelObject obj in objs) {
            writer.Write(obj.GetType().AssemblyQualifiedName ?? "");
            writer.Write(obj.id.ToByteArray());
            writer.Write(obj.layer);
            writer.Write(obj.position.x);
            writer.Write(obj.position.y);
            obj.CustomSerialize(writer);
        }
        return stream.ToArray();
    }

    public static JoinedPacket Deserialize(BinaryReader reader) {
        Guid playerObjId = new(reader.ReadBytes(16));
        int objCount = reader.ReadInt32();
        List<LevelObject> objs = new(objCount);
        for(int i = 0; i < objCount; i++) {
            Type? type = Type.GetType(reader.ReadString());
            if(type is null)
                throw new InvalidDataException();
            if(Activator.CreateInstance(type) is not LevelObject obj)
                throw new InvalidDataException();
            obj.id = new Guid(reader.ReadBytes(16));
            obj.layer = reader.ReadInt32();
            obj.position = new Vector2Int(reader.ReadInt32(), reader.ReadInt32());
            obj.CustomDeserialize(reader);
            objs.Add(obj);
        }
        return new JoinedPacket(playerObjId, objCount, objs);
    }
}
