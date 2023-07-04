using PER.Abstractions.Rendering;
using PER.Util;

namespace CGSrl.Shared.Environment;

public class WallObject : MovableObject {
    public class Broken : MovableObject {
        public override int layer => -1;
        public override RenderCharacter character { get; } =
            new('%', Color.transparent, new Color(0.5f, 0.5f, 0.5f, 1f));
        public override bool blocksLight => false;
        protected override bool canPush => true;
        protected override float mass => 8f;
        protected override float strength => float.PositiveInfinity;
    }

    public override int layer => 0;
    public override RenderCharacter character { get; } = new('#', Color.transparent, Color.white);
    public override bool blocksLight => true;
    protected override bool canPush => false;
    protected override float mass => 4f;
    protected override float strength => 2f;
    protected override MovableObject CreateBroken() => new Broken { position = position };
}
