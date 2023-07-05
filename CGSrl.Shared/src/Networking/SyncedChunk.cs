using PER.Abstractions.Environment;

namespace CGSrl.Shared.Networking;

public class SyncedChunk : Chunk<SyncedLevel, SyncedChunk, SyncedLevelObject> {
    protected override bool shouldUpdate => level.isClient;
    protected override bool shouldTick => !level.isClient;
}
