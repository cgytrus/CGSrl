using CGSrl.Shared.Networking;

using Lidgren.Network;

using PER.Abstractions.Rendering;
using PER.Util;

namespace CGSrl.Shared.Environment;

public class GrassObject : InteractableObject {
    public override string prompt => "touch";

    public override int layer => -1;
    public override RenderCharacter character { get; } = new('"', Color.transparent, new Color(0f, 0.4f, 0f, 1f));
    public override bool blocksLight => false;

    protected override void OnInteract(PlayerObject player) {
        if(player.connection is null)
            return;
        NetOutgoingMessage msg = player.connection.Peer.CreateMessage();
        msg.Write((byte)StcDataType.ChatMessage);
        msg.WriteTime(false);
        msg.Write("grass touched !!!!!!!!!!!!!!!!!");
        player.connection.SendMessage(msg, NetDeliveryMethod.ReliableOrdered, 0);
    }
}
