using PER.Abstractions.Environment;
using PER.Util;

namespace CGSrl.Shared.Environment.Generation;

public abstract class LevelGenerator<TLevel, TChunk, TObject>
    where TLevel : Level<TLevel, TChunk, TObject>
    where TChunk : Chunk<TLevel, TChunk, TObject>, new()
    where TObject : LevelObject<TLevel, TChunk, TObject> {
    protected TLevel level { get; }

    protected LevelGenerator(TLevel level) => this.level = level;

    public abstract void GenerateChunk(Vector2Int start, Vector2Int size);
}
