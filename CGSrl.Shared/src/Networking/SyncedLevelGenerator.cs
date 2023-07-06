using CGSrl.Shared.Environment.Generation;

namespace CGSrl.Shared.Networking;

public abstract class SyncedLevelGenerator : LevelGenerator<SyncedLevel, SyncedChunk, SyncedLevelObject> {
    protected SyncedLevelGenerator(SyncedLevel level) : base(level) { }
}
