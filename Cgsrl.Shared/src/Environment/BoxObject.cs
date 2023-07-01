using PER.Abstractions.Rendering;
using PER.Util;

namespace Cgsrl.Shared.Environment;

public class BoxObject : PushableObject {
    protected override RenderCharacter character { get; } = new('&', Color.transparent, new Color(1f, 1f, 0f, 1f));
}
