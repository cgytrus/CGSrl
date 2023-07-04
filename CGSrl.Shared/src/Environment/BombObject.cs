using System.Numerics;

using PER.Abstractions.Rendering;
using PER.Util;

namespace CGSrl.Shared.Environment;

public class BombObject : InteractableObject {
    private const float Range = 10f;
    private const float Force = 10f;

    public override string prompt => "detonate";

    public override int layer => 1;
    protected override RenderCharacter character { get; } = new('O', Color.transparent, Color.white);

    protected override void OnInteract(PlayerObject player) {
        int range = (int)MathF.Ceiling(Range);
        for(int y = -range; y <= range; y++) {
            for(int x = -range; x <= range; x++) {
                if(x == 0 && y == 0)
                    continue;
                Vector2Int pos = position + new Vector2Int(x, y);
                Vector2 dir = new(x, y);
                float dist = dir.Length();
                if(dist > Range)
                    continue;
                ApplyForceAt(pos, dir, dist);
            }
        }
    }
    private void ApplyForceAt(Vector2Int pos, Vector2 dir, float dist) {
        foreach(MovableObject movable in level.GetObjectsAt<MovableObject>(pos))
            movable.AddForce(dir / dist * (1f - (dist - 1f) / Range) * Force);
    }
}
