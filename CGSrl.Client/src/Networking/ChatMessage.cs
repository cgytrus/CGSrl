using CGSrl.Shared.Environment;

using Lidgren.Network;

using PER.Abstractions.Input;
using PER.Common.Effects;

using PRR.UI;

namespace CGSrl.Client.Networking;

public class ChatMessage {
    public const float FadeInTime = 0.5f;
    private const double StayTime = 60d;
    private const float FadeOutTime = 5f;

    public ChatMessage(PlayerObject? player, double time, string text) {
        this.player = player;
        this.text = text;
        _time = time;
    }

    public Text? element { get; set; }
    public FadeEffect? fade { get; set; }
    public PlayerObject? player { get; }
    public string text { get; }

    private readonly double _time;

    public void Update(IInput input, ListBox<ChatMessage> messages) {
        if(element is null || fade is null)
            return;
        if(player is not null && !player.highlighted)
            player.highlighted = input.mousePosition.InBounds(messages.bounds) &&
                input.mousePosition.InBounds(element.bounds);
        if(!fade.fading && NetTime.Now - _time >= StayTime)
            fade.Start(FadeOutTime, float.PositiveInfinity, () => messages.Remove(this));
    }
}
