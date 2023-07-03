using CGSrl.Shared.Networking;

using PER.Abstractions.Environment;
using PER.Abstractions.Rendering;
using PER.Util;

namespace CGSrl.Shared.Environment;

public class FloorObject : SyncedLevelObject, IAddable {
    private const float PerlinValue = 0.05f;

    private static readonly FastNoiseLite noise = new();

    static FloorObject() {
        noise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
        noise.SetFrequency(0.1f);
    }

    protected override RenderCharacter character => _character;
    private RenderCharacter _character;

    public void Added() {
        if(!level.isClient)
            return;
        float p = noise.GetNoise(MathF.Abs(position.x), MathF.Abs(position.y)) * PerlinValue;
        _character = new RenderCharacter('.', Color.transparent, new Color(0.1f + p, 0.1f + p, 0.1f + p, 1f));
    }
}
