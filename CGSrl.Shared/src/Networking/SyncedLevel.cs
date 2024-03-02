using CGSrl.Shared.Environment;
using CGSrl.Shared.Environment.GameModes;

using Lidgren.Network;

using NLog;

using PER.Abstractions;
using PER.Abstractions.Environment;
using PER.Abstractions.Resources;
using PER.Util;

namespace CGSrl.Shared.Networking;

public class SyncedLevel : Level<SyncedLevel, SyncedChunk, SyncedLevelObject> {
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    protected override TimeSpan maxGenerationTime { get; }

    public GameMode<SyncedLevel, SyncedChunk, SyncedLevelObject> gameMode { get; }

    public SyncedLevel(LevelClientData? client, IResources resources,
        Vector2Int chunkSize, GameMode<SyncedLevel, SyncedChunk, SyncedLevelObject> gameMode) :
        base(client, resources, chunkSize) {
        this.gameMode = gameMode;
        gameMode.SetLevel(this);
    }

    public SyncedLevel(LevelClientData? client, IResources resources,
        Vector2Int chunkSize, GameMode<SyncedLevel, SyncedChunk, SyncedLevelObject> gameMode,
        TimeSpan maxGenerationTime) : this(client, resources, chunkSize, gameMode) =>
        this.maxGenerationTime = maxGenerationTime;

    protected override void GenerateChunk(Vector2Int start) => gameMode.GenerateChunk(start);

    public override void Update(TimeSpan time) {
        if(gameMode is IUpdatable updatable)
            updatable.Update(time);
        base.Update(time);
    }

    public override void Tick(TimeSpan time) {
        if(gameMode is ITickable tickable)
            tickable.Tick(time);
        base.Tick(time);
    }

    public void Load(string path) {
        logger.Info($"Loading level {path}");
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
            Add(obj);
        }
        logger.Info("Level loaded");
    }

    public void Save(string path) {
        logger.Info($"Saving level {path}");
        NetBuffer buffer = new();
        List<SyncedLevelObject> objs = objects.Values.Where(obj => obj is not PlayerObject).ToList();
        buffer.Write(objs.Count);
        foreach(SyncedLevelObject obj in objs)
            obj.WriteTo(buffer);
        logger.Info("Writing level file");
        File.WriteAllBytes(path, buffer.Data[..buffer.LengthBytes]);
        logger.Info("Level saved");
    }
}
