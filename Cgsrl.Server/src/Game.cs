using Cgsrl.Server.Networking;
using Cgsrl.Shared.Environment;
using Cgsrl.Shared.Networking;

using Lidgren.Network;

using NLog;

using PER.Abstractions;
using PER.Abstractions.Environment;
using PER.Abstractions.Rendering;
using PER.Util;

namespace Cgsrl.Server;

public class Game : IGame {
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    private const string LevelPath = "level.bin";

    private Level<SyncedLevelObject>? _level;
    private GameServer? _server;

    public void Unload() { }
    public void Load() { }
    public RendererSettings Loaded() => new();

    public void Setup() {
        _level = new Level<SyncedLevelObject>(Core.engine.renderer, Core.engine.input, Core.engine.audio,
            Core.engine.resources);

        if(File.Exists(LevelPath))
            LoadLevel();
        else
            CreateTestLevel();

        _server = new GameServer(_level, 12420);
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
                _level.Add(new FloorObject { layer = -1, position = new Vector2Int(x, y) });
                _level.Add(new IceObject { layer = -1, position = new Vector2Int(x, y + 41) });
            }
        }

        _level.Add(new BoxObject { layer = 1, position = new Vector2Int(2, 0) });
        _level.Add(new BoxObject { layer = 1, position = new Vector2Int(2, 1) });
        _level.Add(new BoxObject { layer = 1, position = new Vector2Int(2, 3) });

        for(int i = -5; i <= 5; i++)
            _level.Add(new WallObject { layer = 1, position = new Vector2Int(i, -5) });
        for(int i = 0; i < 100; i++)
            _level.Add(new WallObject { layer = 1, position = new Vector2Int(i, -8) });
        for(int i = 0; i < 30; i++)
            _level.Add(new WallObject { layer = 1, position = new Vector2Int(8, i - 7) });

        _level.Add(new EffectObject {
            layer = 2,
            position = new Vector2Int(3, -10),
            size = new Vector2Int(10, 10),
            effect = "glitch"
        });
        _level.Add(new EffectObject {
            layer = 2,
            position = new Vector2Int(-12, -24),
            size = new Vector2Int(6, 9),
            effect = "glitch"
        });

        for(int i = 0; i < 1000; i++)
            _level.Add(new BoxObject { layer = 1, position = new Vector2Int(-i - 20, 0) });
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

    public void Update(TimeSpan time) => throw new InvalidOperationException();
    public IScreen? currentScreen => null;
    public void SwitchScreen(IScreen? screen, Func<bool>? middleCallback = null) => throw new InvalidOperationException();
    public void SwitchScreen(IScreen? screen, float fadeOutTime, float fadeInTime, Func<bool>? middleCallback = null) => throw new InvalidOperationException();
    public void FadeScreen(Action middleCallback) => throw new InvalidOperationException();
    public void FadeScreen(float fadeOutTime, float fadeInTime, Action middleCallback) => throw new InvalidOperationException();
}
