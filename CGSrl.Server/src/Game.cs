using CGSrl.Server.Networking;
using CGSrl.Shared.Environment;
using CGSrl.Shared.Networking;

using Lidgren.Network;

using NLog;

using PER.Abstractions;
using PER.Abstractions.Rendering;
using PER.Util;

namespace CGSrl.Server;

public class Game : IGame, ISetupable, ITickable {
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    private const string LevelPath = "level.bin";

    private SyncedLevel? _level;
    private GameServer? _server;

    public void Unload() { }
    public void Load() { }
    public RendererSettings Loaded() => new();

    public void Setup() {
        _level = new SyncedLevel(false, Core.engine.renderer, Core.engine.input, Core.engine.audio,
            Core.engine.resources, new Vector2Int(16, 16));

        if(File.Exists(LevelPath)) {
            LoadLevel();
            _level.chunkCreated += GenerateChunk;
        }
        else {
            _level.chunkCreated += GenerateChunk;
            CreateTestLevel();
        }

        _server = new GameServer(_level, 12420);
    }

    private void GenerateChunk(Vector2Int start, Vector2Int size) {
        if(_level is null)
            return;
        for(int x = 0; x < size.x; x++)
            for(int y = 0; y < size.y; y++)
                _level.Add(new FloorObject { position = start + new Vector2Int(x, y) });
    }

    public void Tick(TimeSpan time) {
        if(_server is null || _level is null)
            return;
        _server.ProcessMessages();
        _level.Tick(time);
    }

    public void Finish() {
        _server?.Finish();
        SaveLevel();
    }

    private void CreateTestLevel() {
        if(_level is null)
            return;

        for(int y = -20; y <= 20; y++) {
            for(int x = -20; x <= 20; x++) {
                _level.Add(new IceObject { position = new Vector2Int(x, y + 41) });
                _level.Add(new GrassObject { position = new Vector2Int(x, y + 41 + 41) });
            }
        }

        _level.Add(new BoxObject { position = new Vector2Int(2, 0) });
        _level.Add(new BoxObject { position = new Vector2Int(2, 1) });
        _level.Add(new BoxObject { position = new Vector2Int(2, 3) });
        _level.Add(new MessageObject { position = new Vector2Int(1, 5) });

        for(int i = -5; i <= 5; i++)
            _level.Add(new WallObject { position = new Vector2Int(i, -5) });
        for(int i = 0; i < 100; i++)
            _level.Add(new WallObject { position = new Vector2Int(i, -8) });
        for(int i = 0; i < 30; i++)
            _level.Add(new WallObject { position = new Vector2Int(8, i - 7) });

        for(int y = 0; y < 10; y++)
            for(int x = 0; x < 10; x++)
                _level.Add(new EffectObject {
                    position = new Vector2Int(3, -10) + new Vector2Int(x, y),
                    effect = "glitch"
                });
        for(int y = 0; y < 6; y++)
            for(int x = 0; x < 9; x++)
                _level.Add(new EffectObject {
                    position = new Vector2Int(-12, -24) + new Vector2Int(x, y),
                    effect = "glitch"
                });

        for(int i = 0; i < 1000; i++)
            _level.Add(new BoxObject { position = new Vector2Int(-i - 20, 0) });
    }

    private void LoadLevel() {
        if(_level is null)
            return;
        logger.Info("Loading level");
        byte[] bytes = File.ReadAllBytes(LevelPath);
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
            _level.Add(obj);
        }
        logger.Info("Level loaded");
    }

    public void SaveLevel() {
        if(_level is null)
            return;
        logger.Info("Saving level");
        NetBuffer buffer = new();
        List<SyncedLevelObject> objs = _level.objects.Values.Where(obj => obj is not PlayerObject).ToList();
        buffer.Write(objs.Count);
        foreach(SyncedLevelObject obj in objs)
            obj.WriteTo(buffer);
        logger.Info("Writing level file");
        File.WriteAllBytes(LevelPath, buffer.Data[..buffer.LengthBytes]);
        logger.Info("Level saved");
    }
}
