using Cgsrl.Shared.Networking;

using PER.Abstractions.Rendering;
using PER.Util;

namespace Cgsrl.Shared.Environment;

public class FloorObject : SyncedLevelObject {
    private const float PerlinScale = 10f;
    private const float PerlinValue = 0.05f;

    protected override RenderCharacter character { get; } =
        new('.', Color.transparent, new Color(0.1f, 0.1f, 0.1f, 1f));

    public override void Update(TimeSpan time) { }
    public override void Tick(TimeSpan time) { }

    public override void Draw() {
        float p = Perlin.Get(MathF.Abs(position.x / PerlinScale), MathF.Abs(position.y / PerlinScale), 0f) *
            PerlinValue * 2f - PerlinValue;
        renderer.DrawCharacter(level.LevelToScreenPosition(position),
            new RenderCharacter(character.character, character.background,
                character.foreground + new Color(p, p, p, 0f)));
    }
}
