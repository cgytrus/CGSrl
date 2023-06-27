using Cgsrl.Server.Networking;
using Cgsrl.Shared.Environment;
using Cgsrl.Shared.Networking;

using PER.Abstractions;
using PER.Abstractions.Environment;
using PER.Abstractions.Rendering;
using PER.Util;

namespace Cgsrl.Server;

public class Game : IGame {
    private Level<SyncedLevelObject>? _level;
    private GameServer? _server;

    public void Unload() { }
    public void Load() { }
    public RendererSettings Loaded() => new();

    public void Setup() {
        _level = new Level<SyncedLevelObject>(Core.engine.renderer, Core.engine.input, Core.engine.audio,
            Core.engine.resources);

        for(int y = -20; y <= 20; y++)
            for(int x = -20; x <= 20; x++)
                _level.Add(new FloorObject { layer = -1, position = new Vector2Int(x, y) });

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
    }

    public void Update(TimeSpan time) => throw new InvalidOperationException();
    public IScreen? currentScreen => null;
    public void SwitchScreen(IScreen? screen, Func<bool>? middleCallback = null) => throw new InvalidOperationException();
    public void SwitchScreen(IScreen? screen, float fadeOutTime, float fadeInTime, Func<bool>? middleCallback = null) => throw new InvalidOperationException();
    public void FadeScreen(Action middleCallback) => throw new InvalidOperationException();
    public void FadeScreen(float fadeOutTime, float fadeInTime, Action middleCallback) => throw new InvalidOperationException();
}
