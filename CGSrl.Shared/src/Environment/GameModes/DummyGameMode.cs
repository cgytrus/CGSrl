using CGSrl.Shared.Networking;

using PER.Util;

namespace CGSrl.Shared.Environment.GameModes;

public class DummyGameMode : SyncedGameMode {
    public override bool allowAddingObjects => true;
    public override bool allowRemovingObjects => true;

    protected override void Initialize() { }
    public override void GenerateChunk(Vector2Int start, Vector2Int size) { }
}
