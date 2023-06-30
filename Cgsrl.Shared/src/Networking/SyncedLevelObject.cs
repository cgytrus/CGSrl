using Cgsrl.Shared.Environment;

using Lidgren.Network;

using PER.Abstractions.Environment;

namespace Cgsrl.Shared.Networking;

public abstract class SyncedLevelObject : LevelObject<Level<SyncedLevelObject>> {
    private enum ObjectType { Player, Floor, Wall, Box, Effect, Ice, Message, Grass }

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
            GrassObject => (int)ObjectType.Grass,
            _ => int.MaxValue
        });
        WriteStaticDataTo(buffer);
        WriteDynamicDataTo(buffer);
    }

    protected virtual void WriteStaticDataTo(NetBuffer buffer) {
        buffer.Write(id);
        buffer.Write(layer);
    }
    public virtual void WriteDynamicDataTo(NetBuffer buffer) {
        buffer.Write(position);
    }

    protected virtual void ReadStaticDataFrom(NetBuffer buffer) {
        id = buffer.ReadGuid();
        layer = buffer.ReadInt32();
    }
    public virtual void ReadDynamicDataFrom(NetBuffer buffer) {
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
                (int)ObjectType.Grass => new GrassObject(),
                _ => new CorruptedObject()
            };
            obj.ReadStaticDataFrom(buffer);
            obj.ReadDynamicDataFrom(buffer);
            return obj;
        }
        catch {
            return new CorruptedObject { layer = int.MaxValue };
        }
    }
}
