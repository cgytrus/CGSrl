﻿using PER.Abstractions.Environment;
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
        Vector2Int pos = level.LevelToScreenPosition(position);
        for(int y = Math.Clamp(pos.y, 0, renderer.height); y < Math.Clamp(pos.y + size.y, 0, renderer.height); y++)
            for(int x = Math.Clamp(pos.x, 0, renderer.width); x < Math.Clamp(pos.x + size.x, 0, renderer.width); x++)
                renderer.AddEffect(new Vector2Int(x, y),
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
