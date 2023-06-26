using System;
using System.Collections.Generic;

using PER.Abstractions.Audio;
using PER.Abstractions.Input;
using PER.Abstractions.Rendering;
using PER.Abstractions.Resources;
using PER.Util;

using PRR.UI;
using PRR.UI.Screens;

namespace Cgsrl.Screens;

public class ConnectionErrorDialogBoxScreen : DialogBoxScreenResource {
    public const string GlobalId = "layouts/connectionErrorDialog";

    public Action? onOk { get; set; }

    protected override IResources resources => Core.engine.resources;
    protected override IRenderer renderer => Core.engine.renderer;
    protected override IInput input => Core.engine.input;
    protected override IAudio audio => Core.engine.audio;

    protected override string layoutName => "connectionErrorDialog";
    protected override IReadOnlyDictionary<string, Type> elementTypes { get; } = new Dictionary<string, Type> {
        { "title", typeof(LayoutResourceText) },
        { "text", typeof(LayoutResourceText) },
        { "ok", typeof(LayoutResourceButton) }
    };

    public string text { get; set; } = "";

    private string _formatStr = "{0}";

    public ConnectionErrorDialogBoxScreen() : base(new Vector2Int(72, 18)) { }

    public override void Load(string id) {
        base.Load(id);
        GetElement<Button>("ok").onClick += (_, _) => { onOk?.Invoke(); };
        _formatStr = GetElement<Text>("text").text ?? "{0}";
    }

    public override void Open() {
        base.Open();
        GetElement<Text>("text").text = string.Format(_formatStr, text);
    }

    public override void Close() {
        base.Close();
        onOk = null;
    }

    public override void Tick(TimeSpan time) { }
}
