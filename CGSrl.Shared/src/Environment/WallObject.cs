using PER.Abstractions.Rendering;
using PER.Util;

namespace CGSrl.Shared.Environment;

public class WallObject : MovableObject {
    public class Broken : MovableObject {
        public override int layer => -1;
        protected override RenderCharacter character { get; } =
            new('%', Color.transparent, new Color(0.5f, 0.5f, 0.5f, 1f));
        protected override bool canPush => true;
        protected override float mass => 1f;
        protected override float strength => float.PositiveInfinity;
    }

    public override int layer => 0;
    protected override RenderCharacter character { get; } = new('#', Color.transparent, Color.white);
    protected override bool canPush => false;
    protected override float mass => float.PositiveInfinity;
    protected override float strength => 8f;
    protected override MovableObject CreateBroken() => new Broken { position = position };
}
