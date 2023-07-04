using PER.Abstractions.Rendering;
using PER.Util;

namespace CGSrl.Shared.Environment;

public class BoxObject : MovableObject {
    public class Broken : MovableObject {
        public override int layer => -1;
        protected override RenderCharacter character { get; } = new('%', Color.transparent, new Color(0.5f, 0.5f, 0f, 1f));
        protected override bool canPush => true;
        protected override float mass => 1f;
        protected override float strength => float.PositiveInfinity;
    }

    protected override bool canPush => true;
    protected override float mass => 1f;
    protected override float strength => 4f;
    protected override MovableObject CreateBroken() => new Broken { position = position };
    public override int layer => 0;
    protected override RenderCharacter character { get; } = new('&', Color.transparent, new Color(1f, 1f, 0f, 1f));
}
