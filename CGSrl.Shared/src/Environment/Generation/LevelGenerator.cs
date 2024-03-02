using PER.Abstractions.Environment;
using PER.Util;

namespace CGSrl.Shared.Environment.Generation;

public abstract class LevelGenerator<TLevel, TChunk, TObject>(TLevel level)
    where TLevel : Level<TLevel, TChunk, TObject>
    where TChunk : Chunk<TLevel, TChunk, TObject>, new()
    where TObject : LevelObject<TLevel, TChunk, TObject> {
    protected TLevel level { get; } = level;

    public abstract void GenerateChunk(Vector2Int start);
}
