using Cgsrl.Shared.Networking;

using PER.Abstractions.Rendering;
using PER.Util;

namespace Cgsrl.Shared.Environment;

public class FloorObject : SyncedLevelObject {
    private const float PerlinScale = 10f;
    private const float PerlinValue = 0.05f;

    protected override RenderCharacter character => _character;
    private RenderCharacter _character;

    public override void Added() {
        base.Added();
        float p = Perlin.Get(MathF.Abs(position.x / PerlinScale), MathF.Abs(position.y / PerlinScale), 0f) *
            PerlinValue * 2f - PerlinValue;
        _character = new RenderCharacter('.', Color.transparent, new Color(0.1f + p, 0.1f + p, 0.1f + p, 1f));
    }

    public override void Update(TimeSpan time) { }
    public override void Tick(TimeSpan time) { }
}
