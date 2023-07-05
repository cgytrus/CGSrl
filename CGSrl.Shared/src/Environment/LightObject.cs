using CGSrl.Shared.Networking;

using PER.Abstractions.Environment;
using PER.Abstractions.Rendering;
using PER.Util;

namespace CGSrl.Shared.Environment;

public class LightObject : SyncedLevelObject, ILight {
    public override int layer => -1;
    public override RenderCharacter character { get; } = new('*', Color.transparent, new Color(0f, 0.8f, 0.8f, 1f));
    public override bool blocksLight => false;

    public float brightness => 1f;
    public byte emission => 24;
}
