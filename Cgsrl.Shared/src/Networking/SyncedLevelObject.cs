using Cgsrl.Shared.Environment;

using Lidgren.Network;

using PER.Abstractions.Environment;

namespace Cgsrl.Shared.Networking;

public abstract class SyncedLevelObject : LevelObject<Level<SyncedLevelObject>> {
    private enum ObjectType { Player, Floor, Wall, Box, Effect, Ice, Message }

    //                              id   layer         position          extra
    public const int PreallocSize = 16 + sizeof(int) + sizeof(int) * 2 + 8;

    public void WriteTo(NetBuffer buffer) {
        buffer.Write(this switch {
            PlayerObject => (int)ObjectType.Player,
            FloorObject => (int)ObjectType.Floor,
            WallObject => (int)ObjectType.Wall,
            BoxObject => (int)ObjectType.Box,
            EffectObject => (int)ObjectType.Effect,
            IceObject => (int)ObjectType.Ice,
            MessageObject => (int)ObjectType.Message,
            _ => int.MaxValue
        });
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
        try {
            int type = buffer.ReadInt32();
            SyncedLevelObject obj = type switch {
                (int)ObjectType.Player => new PlayerObject(),
                (int)ObjectType.Floor => new FloorObject(),
                (int)ObjectType.Wall => new WallObject(),
                (int)ObjectType.Box => new BoxObject(),
                (int)ObjectType.Effect => new EffectObject(),
                (int)ObjectType.Ice => new IceObject(),
                (int)ObjectType.Message => new MessageObject(),
                _ => new CorruptedObject()
            };
            obj.id = buffer.ReadGuid();
            obj.ReadDataFrom(buffer);
            return obj;
        }
        catch {
            return new CorruptedObject { layer = int.MaxValue };
        }
    }
}
