using PER.Abstractions.Audio;
using PER.Abstractions.Environment;
using PER.Abstractions.Input;
using PER.Abstractions.Rendering;
using PER.Abstractions.Resources;
using PER.Util;

namespace CGSrl.Shared.Networking;

public class SyncedLevel : Level<SyncedLevel, SyncedChunk, SyncedLevelObject> {
    public bool isClient { get; }

    public SyncedLevel(bool isClient, IRenderer renderer, IInput input, IAudio audio, IResources resources,
        Vector2Int chunkSize) : base(renderer, input, audio, resources, chunkSize) => this.isClient = isClient;
}
