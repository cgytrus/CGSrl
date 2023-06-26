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
        public Template(PlayerListTemplate resource) : base(resource) { }

        public override void UpdateWithItem(int index, PlayerObject item, int width) {
            Text nameButton = GetElement<Text>("name");
            nameButton.text = string.Format(nameButton.text ?? "{1}", item.username, item.displayName);
        }
    }

    public override IListBoxTemplateFactory<PlayerObject>.Template CreateTemplate() => new Template(this);
}
