﻿using Cgsrl.Shared.Networking;

using Lidgren.Network;

using PER.Abstractions.Rendering;
using PER.Util;

namespace Cgsrl.Shared.Environment;

public class GrassObject : InteractableObject {
    public override string prompt => "touch";

    protected override RenderCharacter character { get; } = new('"', Color.transparent, new Color(0f, 0.4f, 0f, 1f));

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
