using CGSrl.Server.Networking;
using CGSrl.Shared.Environment.GameModes;
using CGSrl.Shared.Networking;

using PER.Abstractions;
using PER.Util;

namespace CGSrl.Server;

public class Game : IGame, ISetupable, ITickable {
    private SyncedLevel? _level;
    private GameServer? _server;

    public void Unload() { }
    public void Load() { }
    public void Loaded() { }

    public void Setup() {
        _level = new SyncedLevel(false, null!, null!, null!,
            Core.engine.resources, new Vector2Int(16, 16), new SandboxGameMode(),
            Core.engine.tickInterval);
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
        _level?.Save("level.bin");
    }
}
