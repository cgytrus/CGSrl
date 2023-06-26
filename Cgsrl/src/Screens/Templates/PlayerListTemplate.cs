using System;
using System.Collections.Generic;

using Cgsrl.Shared.Environment;

using PER.Abstractions.Audio;
using PER.Abstractions.Input;
using PER.Abstractions.Rendering;

using PRR.UI;
using PRR.UI.Resources;

namespace Cgsrl.Screens.Templates;

public class PlayerListTemplate : ListBoxTemplateResource<PlayerObject> {
    public const string GlobalId = "layouts/templates/playerItem";

    protected override IRenderer renderer => Core.engine.renderer;
    protected override IInput input => Core.engine.input;
    protected override IAudio audio => Core.engine.audio;
    protected override string layoutName => "playerItem";
    protected override IReadOnlyDictionary<string, Type> elementTypes { get; } = new Dictionary<string, Type> {
        { "name", typeof(LayoutResourceText) }
    };

    private class Template : BasicTemplate {
        private readonly string _formatStr;

        public Template(PlayerListTemplate resource) : base(resource) =>
            _formatStr = GetElement<Text>("name").text ?? "{1}";

        public override void UpdateWithItem(int index, PlayerObject item, int width) {
            Text nameText = GetElement<Text>("name");
            nameText.text = string.Format(_formatStr, item.username, item.displayName);
            item.text = nameText;
        }
    }

    public override IListBoxTemplateFactory<PlayerObject>.Template CreateTemplate() => new Template(this);
}
