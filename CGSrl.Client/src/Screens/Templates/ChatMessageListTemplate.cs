using System.Collections.Immutable;

using CGSrl.Client.Networking;

using PER.Abstractions.Audio;
using PER.Abstractions.Input;
using PER.Abstractions.Rendering;
using PER.Common.Effects;
using PER.Util;

using PRR.UI;
using PRR.UI.Resources;

namespace CGSrl.Client.Screens.Templates;

public class ChatMessageListTemplate : ListBoxTemplateResource<ChatMessage> {
    public const string GlobalId = "layouts/templates/chatItem";

    protected override IRenderer renderer => Core.engine.renderer;
    protected override IInput input => Core.engine.input;
    protected override IAudio audio => Core.engine.audio;

    public override void Preload() {
        base.Preload();
        AddLayout("chatItem");
        AddElement<Text>("text");
    }

    private class Template : BasicTemplate {
        private readonly string _formatStr;

        public Template(ChatMessageListTemplate resource) : base(resource) {
            Text text = GetElement<Text>("text");
            _formatStr = text.text ?? "{1}";
        }

        public override void UpdateWithItem(int index, ChatMessage item, int width) {
            if(item.fade is null) {
                item.fade = new FadeEffect();
                item.fade.Start(0f, ChatMessage.FadeInTime, () => { });
            }

            Text text = GetElement<Text>("text");
            text.text = string.Format(_formatStr, item.player?.displayName ?? "\f0SYSTEM\f\0", item.text);
            item.element = text;
            text.effect = item.fade;
            ImmutableArray<char> formatters = text.formatting.Keys.ToImmutableArray();
            foreach(char formatter in formatters)
                text.formatting[formatter] = text.formatting[formatter] with { effect = item.fade };
        }

        public override void MoveTo(Vector2Int origin, int index, Vector2Int size) {
            int yOffset = size.y - 1 - height * index;
            foreach((string? id, Element? element) in idElements)
                element.position = origin + offsets[id] + new Vector2Int(0, yOffset);
        }
    }

    public override IListBoxTemplateFactory<ChatMessage>.Template CreateTemplate() => new Template(this);
}
