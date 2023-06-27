using Cgsrl.Shared.Environment;

using Lidgren.Network;

using PER.Abstractions.Environment;

namespace Cgsrl.Shared.Networking;

public abstract class SyncedLevelObject : LevelObject<Level<SyncedLevelObject>> {
    private enum ObjectType { Player, Floor, Wall, Box, Effect }

    //                              id   layer         position          extra
    public const int PreallocSize = 16 + sizeof(int) + sizeof(int) * 2 + 8;

    public void WriteTo(NetBuffer buffer) {
        buffer.Write((int)(this switch {
            PlayerObject => ObjectType.Player,
            FloorObject => ObjectType.Floor,
            WallObject => ObjectType.Wall,
            BoxObject => ObjectType.Box,
            EffectObject => ObjectType.Effect,
            _ => throw new InvalidOperationException()
        }));
        WriteDataTo(buffer);
    }
    public virtual void WriteDataTo(NetBuffer buffer) {
        buffer.Write(id);
        buffer.Write(layer);
        buffer.Write(position);
    }

    public virtual void ReadDataFrom(NetBuffer buffer) {
        layer = buffer.ReadInt32();
        position = buffer.ReadVector2Int();
    }

    public static SyncedLevelObject Read(NetBuffer buffer) {
        ObjectType type = (ObjectType)buffer.ReadInt32();
        SyncedLevelObject obj = type switch {
            ObjectType.Player => new PlayerObject(),
            ObjectType.Floor => new FloorObject(),
            ObjectType.Wall => new WallObject(),
            ObjectType.Box => new BoxObject(),
            ObjectType.Effect => new EffectObject(),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "")
        };
        obj.id = buffer.ReadGuid();
        obj.ReadDataFrom(buffer);
        return obj;
    }
}
