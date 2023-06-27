using System;
using System.Collections.Generic;

using Cgsrl.Networking;

using PER.Abstractions.Audio;
using PER.Abstractions.Input;
using PER.Abstractions.Rendering;
using PER.Abstractions.UI;
using PER.Common.Effects;
using PER.Util;

using PRR.UI;
using PRR.UI.Resources;

namespace Cgsrl.Screens.Templates;

public class ChatMessageListTemplate : ListBoxTemplateResource<ChatMessage> {
    public const string GlobalId = "layouts/templates/chatItem";

    protected override IRenderer renderer => Core.engine.renderer;
    protected override IInput input => Core.engine.input;
    protected override IAudio audio => Core.engine.audio;
    protected override string layoutName => "chatItem";
    protected override IReadOnlyDictionary<string, Type> elementTypes { get; } = new Dictionary<string, Type> {
        { "text", typeof(LayoutResourceText) }
    };

    private class Template : BasicTemplate {
        private readonly string _formatStr;

        public Template(ChatMessageListTemplate resource) : base(resource) {
            Text text = GetElement<Text>("text");
            _formatStr = text.text ?? "{1}";
        }

        public override void UpdateWithItem(int index, ChatMessage item, int width) {
            Text text = GetElement<Text>("text");
            text.text = string.Format(_formatStr, item.player?.displayName ?? "\f0SYSTEM\f\0", item.text);
            if(item.element == text)
                return;
            item.element = text;
            FadeEffect fade = new();
            text.effect = fade;
            text.formatting['\0'] = text.formatting['\0'] with { effect = fade };
            text.formatting['0'] = text.formatting['0'] with { effect = fade };
            text.formatting['1'] = text.formatting['1'] with { effect = fade };
            text.formatting['2'] = text.formatting['2'] with { effect = fade };
        }

        public override void MoveTo(Vector2Int origin, int index, Vector2Int size) {
            int yOffset = size.y - 1 - height * index;
            foreach((string? id, Element? element) in idElements)
                element.position = origin + offsets[id] + new Vector2Int(0, yOffset);
        }
    }

    public override IListBoxTemplateFactory<ChatMessage>.Template CreateTemplate() => new Template(this);
}
