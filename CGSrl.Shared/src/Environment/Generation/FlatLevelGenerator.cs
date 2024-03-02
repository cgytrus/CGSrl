using CGSrl.Shared.Networking;

using PER.Util;

namespace CGSrl.Shared.Environment.Generation;

public class FlatLevelGenerator(SyncedLevel level) : SyncedLevelGenerator(level) {
    public override void GenerateChunk(Vector2Int start) {
        for(int y = 0; y < level.chunkSize.y; y++) {
            for(int x = 0; x < level.chunkSize.x; x++) {
                level.Add(new FloorObject { position = new Vector2Int(start.x + x, start.y + y) });
            }
        }
    }
}
