using CGSrl.Shared.Networking;

using PER.Abstractions.Rendering;
using PER.Util;

namespace CGSrl.Shared.Environment;

public class IceObject : SyncedLevelObject {
    public override int layer => -2;

    protected override RenderCharacter character { get; } =
        new('~', Color.transparent, new Color(0f, 0.2f, 0.2f, 1f));
}
