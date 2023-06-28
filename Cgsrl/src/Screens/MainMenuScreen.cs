using System;
using System.Collections.Generic;

using PER.Abstractions;
using PER.Abstractions.Audio;
using PER.Abstractions.Input;
using PER.Abstractions.Rendering;
using PER.Abstractions.UI;
using PER.Util;

using PRR.UI;
using PRR.UI.Resources;

namespace Cgsrl.Screens;

public class MainMenuScreen : LayoutResource, IScreen {
    public const string GlobalId = "layouts/mainMenu";

    protected override IRenderer renderer => Core.engine.renderer;
    protected override IInput input => Core.engine.input;
    protected override IAudio audio => Core.engine.audio;

    protected override string layoutName => "mainMenu";
    protected override IReadOnlyDictionary<string, Type> elementTypes { get; } = new Dictionary<string, Type> {
        { "frameLeft", typeof(LayoutResourceText) },
        { "frameRight", typeof(LayoutResourceText) },
        { "title", typeof(LayoutResourceText) },
        { "play", typeof(LayoutResourceButton) },
        { "play.address", typeof(LayoutResourceInputField) },
        { "play.username", typeof(LayoutResourceInputField) },
        { "play.displayName", typeof(LayoutResourceInputField) },
        { "settings", typeof(LayoutResourceButton) },
        { "exit", typeof(LayoutResourceButton) },
        { "sfml", typeof(LayoutResourceButton) },
        { "github", typeof(LayoutResourceButton) },
        { "discord", typeof(LayoutResourceButton) },
        { "versions", typeof(LayoutResourceText) }
    };

    protected override IEnumerable<KeyValuePair<string, string>> paths {
        get {
            foreach(KeyValuePair<string, string> pair in base.paths)
                yield return pair;
            yield return new KeyValuePair<string, string>("frameLeft.text", $"{layoutsPath}/{layoutName}Left.txt");
            yield return new KeyValuePair<string, string>("frameRight.text", $"{layoutsPath}/{layoutName}Right.txt");
        }
    }

    private readonly Settings _settings;

    private ConnectionErrorDialogBoxScreen? _connectionErrorDialogBox;

    public MainMenuScreen(Settings settings) => _settings = settings;

    public override void Load(string id) {
        base.Load(id);

        InputField playAddress = GetElement<InputField>("play.address");
        InputField playUsername = GetElement<InputField>("play.username");
        InputField playDisplayName = GetElement<InputField>("play.displayName");
        GetElement<Button>("play").onClick += (_, _) => {
            if(Core.engine.resources.TryGetResource(GameScreen.GlobalId, out GameScreen? screen))
                Core.engine.game.SwitchScreen(screen, () => {
                    screen.Connect(playAddress.value ?? "", playUsername.value ?? "", playDisplayName.value ?? "");
                    return true;
                });
        };

        playAddress.onSubmit += (_, _) => _settings.address = playAddress.value ?? "";
        playAddress.onCancel += (_, _) => _settings.address = playAddress.value ?? "";
        playUsername.onSubmit += (_, _) => _settings.username = playUsername.value ?? "";
        playUsername.onCancel += (_, _) => _settings.username = playUsername.value ?? "";
        playDisplayName.onSubmit += (_, _) => _settings.displayName = playDisplayName.value ?? "";
        playDisplayName.onCancel += (_, _) => _settings.displayName = playDisplayName.value ?? "";

        GetElement<Button>("settings").onClick += (_, _) => {
            if(Core.engine.resources.TryGetResource(SettingsScreen.GlobalId, out SettingsScreen? screen))
                Core.engine.game.SwitchScreen(screen);
        };

        GetElement<Button>("exit").onClick += (_, _) => {
            Core.engine.game.SwitchScreen(null);
        };

        GetElement<Button>("sfml").onClick += (_, _) => {
            Helper.OpenUrl("https://sfml-dev.org");
        };

        GetElement<Button>("github").onClick += (_, _) => {
            Helper.OpenUrl("https://github.com/cgytrus/Cgsrl");
        };

        GetElement<Button>("discord").onClick += (_, _) => {
            Helper.OpenUrl("https://discord.gg/AuYUVs5");
        };

        Text versions = GetElement<Text>("versions");
        versions.text =
            string.Format(versions.text ?? string.Empty, Core.version, Core.engineVersion, Core.abstractionsVersion,
                Core.utilVersion, Core.commonVersion, Core.audioVersion, Core.rendererVersion, Core.uiVersion);
    }

    public void Open() {
        GetElement<InputField>("play.address").value = _settings.address;
        GetElement<InputField>("play.username").value = _settings.username;
        GetElement<InputField>("play.displayName").value = _settings.displayName;
    }

    public void Close() { }

    public void Update(TimeSpan time) {
        bool prevInputBlock = input.block;
        input.block = _connectionErrorDialogBox is not null;

        foreach((string _, Element element) in elements)
            element.Update(time);

        input.block = prevInputBlock;

        _connectionErrorDialogBox?.Update(time);
    }

    public void Tick(TimeSpan time) { }

    public void ShowConnectionError(string error) {
        if(!Core.engine.resources.TryGetResource(ConnectionErrorDialogBoxScreen.GlobalId,
            out _connectionErrorDialogBox)) {
            return;
        }
        _connectionErrorDialogBox.onOk += () => Core.engine.game.FadeScreen(CloseConnectionError);
        _connectionErrorDialogBox.text = error;
        _connectionErrorDialogBox.Open();
    }

    private void CloseConnectionError() {
        _connectionErrorDialogBox?.Close();
        _connectionErrorDialogBox = null;
    }
}
