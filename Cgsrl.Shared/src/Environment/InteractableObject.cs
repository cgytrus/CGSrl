using Cgsrl.Shared.Networking;

using Lidgren.Network;

namespace Cgsrl.Shared.Environment;

public abstract class InteractableObject : SyncedLevelObject {
    public abstract string prompt { get; }

    public void Interact(PlayerObject player) {
        if(!level.isClient) {
            OnInteract(player);
            return;
        }
        if(player.connection is null)
            return;
        NetOutgoingMessage msg = player.connection.Peer.CreateMessage();
        msg.Write((byte)CtsDataType.PlayerInteract);
        msg.Write(id);
        player.connection.SendMessage(msg, NetDeliveryMethod.ReliableOrdered, 0);
    }

    protected abstract void OnInteract(PlayerObject player);
}
