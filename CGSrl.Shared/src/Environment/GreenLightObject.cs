using CGSrl.Shared.Networking;

using PER.Abstractions.Environment;
using PER.Abstractions.Rendering;
using PER.Util;

namespace CGSrl.Shared.Environment;

public class GreenLightObject : SyncedLevelObject, ILight {
    public override int layer => -1;
    public override RenderCharacter character { get; } = new('*', Color.transparent, new Color(0f, 1f, 0f));
    public override bool blocksLight => false;

    public Color3 color => new(0f, 1f, 0f);
    public byte emission => 24;
}
