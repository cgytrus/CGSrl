using CGSrl.Shared.Environment;

using Lidgren.Network;

using PER.Abstractions.Environment;

namespace CGSrl.Shared.Networking;

public abstract class SyncedLevelObject : LevelObject<SyncedLevel, SyncedChunk, SyncedLevelObject> {
    private enum ObjectType { Player, Floor, Wall, Box, Effect, Ice, Message, Grass, Bomb, BrokenWall, BrokenBox }

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
            BombObject => (int)ObjectType.Bomb,
            WallObject.Broken => (int)ObjectType.BrokenWall,
            BoxObject.Broken => (int)ObjectType.BrokenBox,
            _ => int.MaxValue
        });
        buffer.Write(id);
        WriteStaticDataTo(buffer);
        WriteDynamicDataTo(buffer);
    }

    protected virtual void WriteStaticDataTo(NetBuffer buffer) { }
    public virtual void WriteDynamicDataTo(NetBuffer buffer) {
        buffer.Write(position);
    }

    protected virtual void ReadStaticDataFrom(NetBuffer buffer) { }
    public virtual void ReadDynamicDataFrom(NetBuffer buffer) {
        position = buffer.ReadVector2Int();
    }

    public static SyncedLevelObject Read(NetBuffer buffer) {
        try {
            int type = buffer.ReadInt32();
            Guid id = buffer.ReadGuid();
            SyncedLevelObject obj = type switch {
                (int)ObjectType.Player => new PlayerObject { id = id },
                (int)ObjectType.Floor => new FloorObject { id = id },
                (int)ObjectType.Wall => new WallObject { id = id },
                (int)ObjectType.Box => new BoxObject { id = id },
                (int)ObjectType.Effect => new EffectObject { id = id },
                (int)ObjectType.Ice => new IceObject { id = id },
                (int)ObjectType.Message => new MessageObject { id = id },
                (int)ObjectType.Grass => new GrassObject { id = id },
                (int)ObjectType.Bomb => new BombObject { id = id },
                (int)ObjectType.BrokenWall => new WallObject.Broken { id = id },
                (int)ObjectType.BrokenBox => new BoxObject.Broken { id = id },
                _ => new CorruptedObject { id = id }
            };
            obj.ReadStaticDataFrom(buffer);
            obj.ReadDynamicDataFrom(buffer);
            return obj;
        }
        catch {
            return new CorruptedObject();
        }
    }
}
