using System;

using PER.Abstractions;
using PER.Abstractions.Audio;
using PER.Abstractions.Input;
using PER.Abstractions.Rendering;
using PER.Abstractions.Screens;
using PER.Util;

using PRR.UI;
using PRR.UI.Resources;

namespace CGSrl.Client.Screens;

public class MainMenuScreen(Settings settings) : LayoutResource, IScreen, IUpdatable {
    public const string GlobalId = "layouts/mainMenu";

    protected override IRenderer renderer => Core.engine.renderer;
    protected override IInput input => Core.engine.input;
    protected override IAudio audio => Core.engine.audio;

    private ConnectionErrorDialogBoxScreen? _connectionErrorDialogBox;

    public override void Preload() {
        base.Preload();
        AddLayout("mainMenu");

        AddPath("frameLeft.text", $"{layoutsPath}/mainMenuLeft.txt");
        AddPath("frameRight.text", $"{layoutsPath}/mainMenuRight.txt");

        AddElement<Text>("frameLeft");
        AddElement<Text>("frameRight");
        AddElement<Text>("title");
        AddElement<Button>("play");
        AddElement<InputField>("play.address");
        AddElement<InputField>("play.username");
        AddElement<InputField>("play.displayName");
        AddElement<Button>("settings");
        AddElement<Button>("exit");
        AddElement<Button>("sfml");
        AddElement<Button>("github");
        AddElement<Button>("discord");
        AddElement<Text>("versions");
    }

    public override void Load(string id) {
        base.Load(id);

        InputField playAddress = GetElement<InputField>("play.address");
        InputField playUsername = GetElement<InputField>("play.username");
        InputField playDisplayName = GetElement<InputField>("play.displayName");
        GetElement<Button>("play").hotkey = KeyCode.Enter;
        GetElement<Button>("play").onClick += (_, _) => {
            if(Core.engine.resources.TryGetResource(ConnectingScreen.GlobalId, out ConnectingScreen? screen))
                Core.engine.screens.SwitchScreen(screen, () => {
                    screen.Connect(playAddress.value ?? "", playUsername.value ?? "",
                        string.IsNullOrWhiteSpace(playDisplayName.value) ? playUsername.value ?? "" :
                            playDisplayName.value ?? "");
                    return true;
                });
        };

        playAddress.onSubmit += (_, _) => settings.address = playAddress.value ?? "";
        playAddress.onCancel += (_, _) => settings.address = playAddress.value ?? "";
        playUsername.onSubmit += (_, _) => settings.username = playUsername.value ?? "";
        playUsername.onCancel += (_, _) => settings.username = playUsername.value ?? "";
        playDisplayName.onSubmit += (_, _) => settings.displayName = playDisplayName.value ?? "";
        playDisplayName.onCancel += (_, _) => settings.displayName = playDisplayName.value ?? "";

        GetElement<Button>("settings").onClick += (_, _) => {
            if(Core.engine.resources.TryGetResource(SettingsScreen.GlobalId, out SettingsScreen? screen))
                Core.engine.screens.SwitchScreen(screen);
        };

        GetElement<Button>("exit").onClick += (_, _) => {
            Core.engine.screens.SwitchScreen(null);
        };

        GetElement<Button>("sfml").onClick += (_, _) => {
            Helper.OpenUrl("https://sfml-dev.org");
        };

        GetElement<Button>("github").onClick += (_, _) => {
            Helper.OpenUrl("https://github.com/cgytrus/CGSrl");
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
        GetElement<InputField>("play.address").value = settings.address;
        GetElement<InputField>("play.username").value = settings.username;
        GetElement<InputField>("play.displayName").value = settings.displayName;
    }

    public void Close() { }

    public void Update(TimeSpan time) {
        bool prevInputBlock = input.block;
        input.block = _connectionErrorDialogBox is not null;

        // ReSharper disable once ForCanBeConvertedToForeach
        for(int i = 0; i < elementList.Count; i++)
            elementList[i].Update(time);

        input.block = prevInputBlock;

        _connectionErrorDialogBox?.Update(time);
    }

    public void ShowConnectionError(string error) {
        if(!Core.engine.resources.TryGetResource(ConnectionErrorDialogBoxScreen.GlobalId,
            out _connectionErrorDialogBox)) {
            return;
        }
        _connectionErrorDialogBox.onOk += () => Core.engine.screens.FadeScreen(CloseConnectionError);
        _connectionErrorDialogBox.text = error;
        _connectionErrorDialogBox.Open();
    }

    private void CloseConnectionError() {
        _connectionErrorDialogBox?.Close();
        _connectionErrorDialogBox = null;
    }
}
