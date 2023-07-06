using PER.Abstractions.Environment;
using PER.Util;

namespace CGSrl.Shared.Environment.GameModes;

public abstract class GameMode<TLevel, TChunk, TObject>
    where TLevel : Level<TLevel, TChunk, TObject>
    where TChunk : Chunk<TLevel, TChunk, TObject>, new()
    where TObject : LevelObject<TLevel, TChunk, TObject> {
    protected TLevel level => _level!;
    private TLevel? _level;

    public abstract bool allowAddingObjects { get; }
    public abstract bool allowRemovingObjects { get; }

    protected abstract void Initialize();

    public abstract void GenerateChunk(Vector2Int start);

    internal void SetLevel(Level<TLevel, TChunk, TObject>? level) {
        _level = level as TLevel;
        Initialize();
    }
}
