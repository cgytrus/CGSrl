using Cgsrl.Shared.Networking;

using PER.Abstractions.Rendering;
using PER.Util;

namespace Cgsrl.Shared.Environment;

public class IceObject : SyncedLevelObject {
    protected override RenderCharacter character { get; } =
        new('~', Color.transparent, new Color(0f, 0.2f, 0.2f, 1f));

    public override void Update(TimeSpan time) { }

    public override void Tick(TimeSpan time) { }
}
