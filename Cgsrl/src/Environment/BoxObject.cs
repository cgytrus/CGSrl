using System;

using PER.Abstractions.Rendering;
using PER.Util;

namespace Cgsrl.Environment;

public class BoxObject : PushableObject {
    protected override RenderCharacter character { get; } = new('&', Color.transparent, new Color(1f, 1f, 0f, 1f));
    public override void Update(TimeSpan time) { }
    public override void Tick(TimeSpan time) { }
}
