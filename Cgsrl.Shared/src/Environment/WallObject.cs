using PER.Abstractions.Rendering;
using PER.Util;

namespace Cgsrl.Shared.Environment;

public class WallObject : MovableObject {
    protected override RenderCharacter character { get; } = new('#', Color.transparent, Color.white);
    protected override bool canPush => false;
    protected override float mass => float.PositiveInfinity;
}
