using System;

using Cgsrl.Shared.Environment;

using PRR.UI;

namespace Cgsrl.Networking;

public class ChatMessage {
    public ChatMessage(PlayerObject? player, double time, string text) {
        this.player = player;
        this.time = time;
        this.text = text;
        fadeOutCallback = () => isNew = false;
    }

    public Text? element { get; set; }
    public bool isNew { get; set; } = true;
    public PlayerObject? player { get; }
    public double time { get; }
    public string text { get; }
    public Action fadeOutCallback { get; }
}
