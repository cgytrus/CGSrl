using PER.Abstractions.Environment;
using PER.Abstractions.Rendering;
using PER.Util;

namespace Cgsrl.Shared.Environment;

public class EffectObject : LevelObject {
    public Vector2Int size { get; set; }
    public string effect { get; set; } = "none";

    protected override RenderCharacter character { get; } = new('a', Color.transparent, Color.white);
    public override void Update(TimeSpan time) { }
    public override void Tick(TimeSpan time) { }
    public override void Draw() {
        for(int y = 0; y < size.y; y++)
            for(int x = 0; x < size.x; x++)
                renderer.AddEffect(drawPosition + new Vector2Int(x, y),
                    renderer.formattingEffects.TryGetValue(this.effect, out IEffect? effect) ? effect : null);
    }

    public override void CustomSerialize(BinaryWriter writer) {
        writer.Write(size.x);
        writer.Write(size.y);
        writer.Write(effect);
    }

    public override void CustomDeserialize(BinaryReader reader) {
        size = new Vector2Int(reader.ReadInt32(), reader.ReadInt32());
        effect = reader.ReadString();
    }
}
