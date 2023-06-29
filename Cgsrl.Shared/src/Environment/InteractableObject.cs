using Cgsrl.Shared.Networking;

namespace Cgsrl.Shared.Environment;

public abstract class InteractableObject : SyncedLevelObject {
    public abstract string prompt { get; }
    public abstract void Interact(PlayerObject player);
}
