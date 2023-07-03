using PER.Abstractions.Rendering;
using PER.Util;

namespace CGSrl.Shared.Environment;

public class BoxObject : MovableObject {
    protected override bool canPush => true;
    protected override float mass => 1f;
    public override int layer => 1;
    protected override RenderCharacter character { get; } = new('&', Color.transparent, new Color(1f, 1f, 0f, 1f));
}
