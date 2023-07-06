using CGSrl.Shared.Networking;

using DeBroglie;
using DeBroglie.Models;
using DeBroglie.Topo;

using NLog;

using PER.Util;

namespace CGSrl.Shared.Environment.Generation;

public class WfcLevelGenerator : SyncedLevelGenerator {
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    private readonly int _extra;
    private readonly TilePropagator _wfcPropagator;

    public WfcLevelGenerator(SyncedLevel level, SyncedLevel sampleLevel, int n) : base(level) {
        _extra = n - 1;
        ITopoArray<Tile> sample = ReadSampleFrom(sampleLevel);
        TileModel model = new OverlappingModel(sample, n, 4, true);
        ITopology topology =
            new GridTopology(level.chunkSize.x + _extra * 2, level.chunkSize.y + _extra * 2, false);
        _wfcPropagator = new TilePropagator(model, topology);
    }

    private static ITopoArray<Tile> ReadSampleFrom(SyncedLevel level) {
        Bounds bounds = level.GetBounds();
        Tile[,] grid = new Tile[bounds.max.y - bounds.min.y + 1, bounds.max.x - bounds.min.x + 1];
        for(int y = 0; y <= bounds.max.y - bounds.min.y; y++)
            for(int x = 0; x <= bounds.max.x - bounds.min.x; x++)
                if(level.TryGetObjectAt(new Vector2Int(bounds.min.x + x, bounds.min.y + y), out SyncedLevelObject? obj))
                    grid[y, x] = new Tile(obj.GetType());
                else
                    grid[y, x] = new Tile(null);
        return TopoArray.Create(grid, false);
    }

    public override void GenerateChunk(Vector2Int start, Vector2Int size) {
        Resolution res = RunWfc(start, size, true);
        if(res != Resolution.Decided) {
            //logger.Warn("Undecided, generating without neighbors");
            res = RunWfc(start, size, false);
        }
        FillWithFloor(start, size);
        if(res == Resolution.Decided)
            FillWithResult(start, size);
        else
            logger.Error("Failed to generate chunk");
    }

    private Resolution RunWfc(Vector2Int start, Vector2Int size, bool useNeighbors) {
        _wfcPropagator.Clear();
        if(useNeighbors)
            WfcSelectContext(start, size);
        return _wfcPropagator.Run();
    }
    private void WfcSelectContext(Vector2Int start, Vector2Int size) {
        for(int y = -_extra; y < size.y + _extra; y++) {
            for(int x = -_extra; x < size.x + _extra; x++) {
                if(x >= 0 && y >= 0 && x < size.x && y < size.y ||
                    !level.TryGetObjectAt(new Vector2Int(start.x + x, start.y + y), out SyncedLevelObject? obj))
                    continue;
                _wfcPropagator.Select(_extra + x, _extra + y, 0, new Tile(obj.GetType()));
            }
        }
    }

    private void FillWithFloor(Vector2Int start, Vector2Int size) {
        for(int y = 0; y < size.y; y++)
            for(int x = 0; x < size.x; x++)
                level.Add(new FloorObject { position = new Vector2Int(start.x + x, start.y + y) });
    }
    private void FillWithResult(Vector2Int start, Vector2Int size) {
        ITopoArray<Type?> tiles = _wfcPropagator.ToValueArray<Type?>();
        for(int y = 0; y < size.y; y++) {
            for(int x = 0; x < size.x; x++) {
                Vector2Int position = new(start.x + x, start.y + y);
                Type? type = tiles.Get(_extra + x, _extra + y);
                if(type is null || Activator.CreateInstance(type) is not SyncedLevelObject obj)
                    continue;
                obj.position = position;
                level.Add(obj);
            }
        }
    }
}
