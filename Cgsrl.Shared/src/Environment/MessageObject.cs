﻿using Cgsrl.Shared.Networking;

using Lidgren.Network;

using PER.Abstractions.Rendering;
using PER.Util;

namespace Cgsrl.Shared.Environment;

public class MessageObject : InteractableObject {
    public override string prompt => "send";

    protected override RenderCharacter character { get; } = new('!', Color.transparent, Color.white);
    public override void Update(TimeSpan time) { }
    public override void Tick(TimeSpan time) { }

    public override void Interact(PlayerObject player) {
        if(player.connection is null)
            return;
        NetOutgoingMessage msg = player.connection.Peer.CreateMessage();
        msg.Write((byte)CtsDataType.ChatMessage);
        msg.WriteTime(false);
        msg.Write("message object clicked");
        player.connection.SendMessage(msg, NetDeliveryMethod.ReliableOrdered, 0);
    }
}
