using CGSrl.Shared.Environment.Generation;
using CGSrl.Shared.Networking;

using PER.Util;

namespace CGSrl.Shared.Environment.GameModes;

public class SandboxGameMode : SyncedGameMode {
    public override bool allowAddingObjects => true;
    public override bool allowRemovingObjects => true;

    private SyncedLevelGenerator? _generator;

    protected override void Initialize() {
        if(level.isClient)
            return;

        if(File.Exists("level.bin"))
            level.Load("level.bin");

        SyncedLevel wfcLevel = new(level.client, level.resources, level.chunkSize, new DummyGameMode()) {
            doLighting = false
        };
        wfcLevel.Load("wfc.bin");
        _generator = new WfcLevelGenerator(level, wfcLevel, 3);
    }

    public override void GenerateChunk(Vector2Int start) => _generator?.GenerateChunk(start);
}
