using CGSrl.Shared.Networking;

using Lidgren.Network;

using PER.Abstractions.Rendering;
using PER.Util;

namespace CGSrl.Shared.Environment;

public class MessageObject : InteractableObject {
    public override string prompt => "send";

    public override int layer => 1;
    public override RenderCharacter character { get; } = new('!', Color.transparent, Color.white);
    public override bool blocksLight => false;

    protected override void OnInteract(PlayerObject player) {
        if(player.connection is null)
            return;
        NetOutgoingMessage msg = player.connection.Peer.CreateMessage();
        msg.Write((byte)StcDataType.ChatMessage);
        msg.Write(Guid.Empty);
        msg.WriteTime(false);
        msg.Write("message object clicked");
        player.connection.SendMessage(msg, NetDeliveryMethod.ReliableOrdered, 0);
    }
}
