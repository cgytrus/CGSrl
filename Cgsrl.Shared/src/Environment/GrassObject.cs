using Cgsrl.Shared.Networking;

using Lidgren.Network;

using PER.Abstractions.Rendering;
using PER.Util;

namespace Cgsrl.Shared.Environment;

public class GrassObject : InteractableObject {
    public override string prompt => "touch";

    protected override RenderCharacter character { get; } = new('"', Color.transparent, new Color(0f, 0.4f, 0f, 1f));
    public override void Update(TimeSpan time) { }
    public override void Tick(TimeSpan time) { }

    public override void Interact(PlayerObject player) {
        if(player.connection is null)
            return;
        NetOutgoingMessage msg = player.connection.Peer.CreateMessage();
        msg.Write((byte)CtsDataType.ChatMessage);
        msg.WriteTime(false);
        msg.Write("i touched grass !!!!!!!!!!!!!!!!!");
        player.connection.SendMessage(msg, NetDeliveryMethod.ReliableOrdered, 0);
    }
}
