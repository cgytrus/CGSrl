using Cgsrl.Shared.Networking;

using PER.Abstractions.Rendering;
using PER.Util;

namespace Cgsrl.Shared.Environment;

public class WallObject : SyncedLevelObject {
    protected override RenderCharacter character { get; } = new('#', Color.transparent, Color.white);
    public override void Update(TimeSpan time) { }
    public override void Tick(TimeSpan time) { }
}
