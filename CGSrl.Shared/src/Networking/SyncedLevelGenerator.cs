using CGSrl.Shared.Environment.Generation;

namespace CGSrl.Shared.Networking;

public abstract class SyncedLevelGenerator(SyncedLevel level)
    : LevelGenerator<SyncedLevel, SyncedChunk, SyncedLevelObject>(level);
