using CGSrl.Server.Networking;
using CGSrl.Shared.Environment;
using CGSrl.Shared.Networking;

using DeBroglie;
using DeBroglie.Models;
using DeBroglie.Topo;

using Lidgren.Network;

using NLog;

using PER.Abstractions;
using PER.Abstractions.Rendering;
using PER.Util;

namespace CGSrl.Server;

public class Game : IGame, ISetupable, ITickable {
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    private SyncedLevel? _level;
    private GameServer? _server;

    private TilePropagator? _wfcPropagator;
    private const int WfcExtra = 2;

    public void Unload() { }
    public void Load() { }
    public RendererSettings Loaded() => new();

    public void Setup() {
        _level = new SyncedLevel(false, Core.engine.renderer, Core.engine.input, Core.engine.audio,
            Core.engine.resources, new Vector2Int(16, 16));

        SyncedLevel wfcLevel = new(false, Core.engine.renderer, Core.engine.input, Core.engine.audio,
            Core.engine.resources, new Vector2Int(16, 16));
        LoadLevel(wfcLevel, "wfc.bin");
        ITopoArray<Tile> sample = ReadSampleFrom(wfcLevel);
        TileModel model = new OverlappingModel(sample, WfcExtra + 1, 4, true);
        ITopology topology = new GridTopology(16 + WfcExtra * 2, 16 + WfcExtra * 2, false);
        _wfcPropagator = new TilePropagator(model, topology);

        if(File.Exists("level.bin")) {
            LoadLevel(_level, "level.bin");
            _level.chunkCreated += GenerateChunk;
        }
        _level.chunkCreated += GenerateChunk;

        _server = new GameServer(_level, 12420);
    }

    private void GenerateChunk(Vector2Int start, Vector2Int size) {
        if(_level is null || _wfcPropagator is null)
            return;
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
        if(_level is null || _wfcPropagator is null)
            return Resolution.Undecided;
        _wfcPropagator.Clear();
        if(useNeighbors)
            WfcSelectContext(start, size);
        return _wfcPropagator.Run();
    }
    private void WfcSelectContext(Vector2Int start, Vector2Int size) {
        if(_level is null || _wfcPropagator is null)
            return;
        for(int y = -WfcExtra; y < size.y + WfcExtra; y++) {
            for(int x = -WfcExtra; x < size.x + WfcExtra; x++) {
                if(x >= 0 && y >= 0 && x < size.x && y < size.y ||
                    !_level.TryGetObjectAt(new Vector2Int(start.x + x, start.y + y), out SyncedLevelObject? obj))
                    continue;
                _wfcPropagator.Select(WfcExtra + x, WfcExtra + y, 0, new Tile(obj.GetType()));
            }
        }
    }

    private void FillWithFloor(Vector2Int start, Vector2Int size) {
        if(_level is null)
            return;
        for(int y = 0; y < size.y; y++)
            for(int x = 0; x < size.x; x++)
                _level.Add(new FloorObject { position = new Vector2Int(start.x + x, start.y + y) });
    }
    private void FillWithResult(Vector2Int start, Vector2Int size) {
        if(_level is null || _wfcPropagator is null)
            return;
        ITopoArray<Type?> tiles = _wfcPropagator.ToValueArray<Type?>();
        for(int y = 0; y < size.y; y++) {
            for(int x = 0; x < size.x; x++) {
                Vector2Int position = new(start.x + x, start.y + y);
                Type? type = tiles.Get(WfcExtra + x, WfcExtra + y);
                if(type is null || Activator.CreateInstance(type) is not SyncedLevelObject obj)
                    continue;
                obj.position = position;
                _level.Add(obj);
            }
        }
    }

    public void Tick(TimeSpan time) {
        if(_server is null || _level is null)
            return;
        _server.ProcessMessages();
        _level.Tick(time);
    }

    public void Finish() {
        _server?.Finish();
        if(_level is not null)
            SaveLevel(_level, "level.bin");
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

    private static void LoadLevel(SyncedLevel level, string path) {
        logger.Info("Loading level");
        byte[] bytes = File.ReadAllBytes(path);
        NetBuffer buffer = new() {
            Data = bytes,
            LengthBytes = bytes.Length,
            Position = 0
        };
        logger.Info("Adding objects");
        int objCount = buffer.ReadInt32();
        for(int i = 0; i < objCount; i++) {
            SyncedLevelObject obj = SyncedLevelObject.Read(buffer);
            if(obj is PlayerObject)
                continue;
            level.Add(obj);
        }
        logger.Info("Level loaded");
    }

    public static void SaveLevel(SyncedLevel level, string path) {
        logger.Info("Saving level");
        NetBuffer buffer = new();
        List<SyncedLevelObject> objs = level.objects.Values.Where(obj => obj is not PlayerObject).ToList();
        buffer.Write(objs.Count);
        foreach(SyncedLevelObject obj in objs)
            obj.WriteTo(buffer);
        logger.Info("Writing level file");
        File.WriteAllBytes(path, buffer.Data[..buffer.LengthBytes]);
        logger.Info("Level saved");
    }
}
