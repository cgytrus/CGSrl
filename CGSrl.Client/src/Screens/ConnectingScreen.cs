using System;

using CGSrl.Client.Networking;
using CGSrl.Client.Screens.Templates;
using CGSrl.Shared.Environment.GameModes;

using PER.Abstractions;
using PER.Abstractions.Audio;
using PER.Abstractions.Input;
using PER.Abstractions.Rendering;
using PER.Abstractions.Resources;
using PER.Abstractions.Screens;

using PRR.UI;
using PRR.UI.Resources;

namespace CGSrl.Client.Screens;

public class ConnectingScreen : LayoutResource, IScreen, IUpdatable {
    public const string GlobalId = "layouts/connecting";

    protected override IRenderer renderer => Core.engine.renderer;
    protected override IInput input => Core.engine.input;
    protected override IAudio audio => Core.engine.audio;

    private readonly IResources _resources;
    private GameClient? _client;

    private bool _connected;

    private Text? _text;
    private ProgressBar? _progress;
    private string _textFormat = "{0}";

    public ConnectingScreen(IResources resources) => _resources = resources;

    public override void Preload() {
        base.Preload();
        AddDependency<PlayerListTemplate>(PlayerListTemplate.GlobalId);
        AddDependency<ChatMessageListTemplate>(ChatMessageListTemplate.GlobalId);

        AddLayout("connecting");

        AddElement<ProgressBar>("progress");
        AddElement<Text>("text");
    }

    public override void Load(string id) {
        base.Load(id);

        _text = GetElement<Text>("text");
        _progress = GetElement<ProgressBar>("progress");

        _textFormat = _text.text ?? _textFormat;

        _client = new GameClient(renderer, input, audio, _resources);
        _client.onConnect += Connected;
        _client.onDisconnect += Disconnected;
    }

    public override void Unload(string id) {
        base.Unload(id);
        _client?.Finish();
        _client = null;
    }

    public void Connect(string address, string username, string displayName) {
        if(_client is null)
            return;

        string host = "127.0.0.1";
        int port = 12420;
        if(address.Length > 0) {
            string[] parts = address.Split(':');
            host = parts[0];
            if(parts.Length >= 2 && int.TryParse(parts[1], out int parsedPort))
                port = parsedPort;
        }

        if(_text is not null)
            _text.enabled = true;
        if(_progress is not null)
            _progress.enabled = true;

        _client.Connect(host, port, username, displayName);
        if(_text is not null && _progress is not null)
            _client.SetUi(_text, _textFormat, _progress, null, null);
    }

    private void Connected() => _connected = true;

    private static void Disconnected(string reason, bool isError) {
        if(Core.engine.resources.TryGetResource(MainMenuScreen.GlobalId, out MainMenuScreen? screen))
            Core.engine.screens.SwitchScreen(screen, () => {
                if(isError)
                    screen.ShowConnectionError(reason);
                return true;
            });
    }

    public void Open() => _connected = false;
    public void Close() { }

    public void Update(TimeSpan time) {
        _client?.Update(time);
        // ReSharper disable once ForCanBeConvertedToForeach
        for(int i = 0; i < elementList.Count; i++)
            elementList[i].Update(time);
        if(!_connected || _client?.level is null)
            return;
        _connected = false;
        ReceivedLevelData();
    }

    private void ReceivedLevelData() {
        if(_client?.level is null)
            return;
        string id = _client.level.gameMode switch {
            SandboxGameMode => SandboxGameScreen.GlobalId,
            _ => throw new ArgumentOutOfRangeException(nameof(_client.level.gameMode), _client.level.gameMode, "")
        };
        if(!Core.engine.resources.TryGetResource(id, out GameScreen? screen))
            return;
        screen.ContinueConnect(_client);
        Core.engine.screens.SwitchScreen(screen, () => true);
    }
}
