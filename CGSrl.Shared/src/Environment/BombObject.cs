using System.Numerics;

using PER.Abstractions;
using PER.Abstractions.Rendering;
using PER.Util;

namespace CGSrl.Shared.Environment;

public class BombObject : InteractableObject, ITickable {
    private const float Range = 10f;
    private const float Force = 10f;

    public override string prompt => "detonate";

    public override int layer => 1;
    public override RenderCharacter character { get; } = new('O', Color.transparent, Color.white);
    public override bool blocksLight => false;

    private int _explodeInTicks;

    public void Tick(TimeSpan time) {
        _explodeInTicks--;
        if(_explodeInTicks == 0)
            Explode();
    }

    protected override void OnInteract(PlayerObject player) => Explode();

    private void Explode() {
        int range = (int)MathF.Ceiling(Range);
        for(int y = -range; y <= range; y++) {
            for(int x = -range; x <= range; x++) {
                if(x == 0 && y == 0)
                    continue;
                ExplodeAt(x, y);
            }
        }
        if(inLevel)
            level.Remove(this);
    }

    private void ExplodeAt(int x, int y) {
        Vector2Int pos = position + new Vector2Int(x, y);
        Vector2 dir = new(x, y);
        float dist = dir.Length();
        if(dist > Range)
            return;
        foreach(MovableObject movable in level.GetObjectsAt<MovableObject>(pos))
            movable.AddForce(dir / dist * (1f - (dist - 1f) / Range) * Force);
        foreach(BombObject bomb in level.GetObjectsAt<BombObject>(pos))
            if(bomb._explodeInTicks < 0)
                bomb._explodeInTicks = 2;
    }
}
