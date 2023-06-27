using Cgsrl.Shared.Networking;

using Lidgren.Network;

using PER.Abstractions.Rendering;
using PER.Util;

namespace Cgsrl.Shared.Environment;

public class EffectObject : SyncedLevelObject {
    public Vector2Int size { get; set; }
    public string effect { get; set; } = "none";

    protected override RenderCharacter character { get; } = new('a', Color.transparent, Color.white);
    public override void Update(TimeSpan time) { }
    public override void Tick(TimeSpan time) { }
    public override void Draw() {
        Vector2Int pos = level.LevelToScreenPosition(position);
        for(int y = Math.Clamp(pos.y, 0, renderer.height); y < Math.Clamp(pos.y + size.y, 0, renderer.height); y++)
            for(int x = Math.Clamp(pos.x, 0, renderer.width); x < Math.Clamp(pos.x + size.x, 0, renderer.width); x++)
                renderer.AddEffect(new Vector2Int(x, y),
                    renderer.formattingEffects.TryGetValue(this.effect, out IEffect? effect) ? effect : null);
    }

    public override void WriteDataTo(NetBuffer buffer) {
        base.WriteDataTo(buffer);
        buffer.Write(size);
        buffer.Write(effect);
    }

    public override void ReadDataFrom(NetBuffer buffer) {
        base.ReadDataFrom(buffer);
        size = buffer.ReadVector2Int();
        effect = buffer.ReadString();
    }
}
