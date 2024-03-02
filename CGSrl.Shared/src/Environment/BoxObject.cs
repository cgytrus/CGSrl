using PER.Abstractions.Rendering;
using PER.Util;

namespace CGSrl.Shared.Environment;

public class BoxObject : MovableObject {
    public class Broken : MovableObject {
        public override int layer => -2;
        public override RenderCharacter character { get; } = new('%', Color.transparent, new Color(0.5f, 0.5f, 0f));
        public override bool blocksLight => false;
        protected override bool canPush => true;
        protected override float mass => 1f;
        protected override float strength => float.PositiveInfinity;
    }

    protected override bool canPush => true;
    protected override float mass => 1f;
    protected override float strength => 4f;
    protected override MovableObject CreateBroken() => new Broken { position = position };
    public override int layer => 0;
    public override RenderCharacter character { get; } = new('&', Color.transparent, new Color(1f, 1f, 0f));
    public override bool blocksLight => true;
}
