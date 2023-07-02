using Cgsrl.Shared.Networking;

using Lidgren.Network;

using PER.Abstractions.Rendering;
using PER.Util;

namespace Cgsrl.Shared.Environment;

public class EffectObject : SyncedLevelObject {
    public string effect { get; set; } = "none";

    protected override RenderCharacter character { get; } = new('a', Color.transparent, Color.white);
    public override void Draw() {
        renderer.AddEffect(level.LevelToScreenPosition(position),
            renderer.formattingEffects.TryGetValue(this.effect, out IDisplayEffect? effect) ? effect : null);
    }

    public override void WriteDynamicDataTo(NetBuffer buffer) {
        base.WriteDynamicDataTo(buffer);
        buffer.Write(effect);
    }

    public override void ReadDynamicDataFrom(NetBuffer buffer) {
        base.ReadDynamicDataFrom(buffer);
        effect = buffer.ReadString();
    }
}
