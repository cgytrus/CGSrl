using System;

using PER.Abstractions.Audio;
using PER.Abstractions.Input;
using PER.Abstractions.Rendering;
using PER.Abstractions.Resources;
using PER.Util;

using PRR.UI;
using PRR.UI.Screens;

namespace CGSrl.Client.Screens;

public class ConnectionErrorDialogBoxScreen : DialogBoxScreenResource {
    public const string GlobalId = "layouts/connectionErrorDialog";

    public Action? onOk { get; set; }

    protected override IResources resources => Core.engine.resources;
    protected override IRenderer renderer => Core.engine.renderer;
    protected override IInput input => Core.engine.input;
    protected override IAudio audio => Core.engine.audio;

    public string text { get; set; } = "";

    private string _formatStr = "{0}";

    public ConnectionErrorDialogBoxScreen() : base(new Vector2Int(72, 18)) { }

    public override void Preload() {
        base.Preload();
        AddLayout("connectionErrorDialog");
        AddElement<Text>("title");
        AddElement<Text>("text");
        AddElement<Button>("ok");
    }

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
}
