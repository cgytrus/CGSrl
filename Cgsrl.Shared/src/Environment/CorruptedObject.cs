using Cgsrl.Shared.Networking;

using PER.Abstractions.Rendering;
using PER.Util;

namespace Cgsrl.Shared.Environment;

// use only when something really really really really really really bad happens
public class CorruptedObject : SyncedLevelObject {
    protected override RenderCharacter character { get; } = new('\0', Color.white, Color.transparent);
    public override void Update(TimeSpan time) { }
    public override void Tick(TimeSpan time) { }
    public override void Draw() => renderer.DrawCharacter(level.LevelToScreenPosition(position), character,
        RenderOptions.Default, renderer.formattingEffects["glitch"]);
}
