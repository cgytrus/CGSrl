using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

using Cgsrl.Networking;
using Cgsrl.Screens.Templates;

using LiteNetwork.Client;

using NLog;

using PER.Abstractions;
using PER.Abstractions.Audio;
using PER.Abstractions.Environment;
using PER.Abstractions.Input;
using PER.Abstractions.Rendering;
using PER.Abstractions.Resources;
using PER.Abstractions.UI;

using PRR.UI.Resources;

namespace Cgsrl.Screens;

public class GameScreen : LayoutResource, IScreen, IDisposable {
    public const string GlobalId = "layouts/game";

    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    protected override IRenderer renderer => Core.engine.renderer;
    protected override IInput input => Core.engine.input;
    protected override IAudio audio => Core.engine.audio;

    protected override string layoutName => "game";
    protected override IReadOnlyDictionary<string, Type> elementTypes { get; } = new Dictionary<string, Type>();

    protected override IEnumerable<KeyValuePair<string, Type>> dependencyTypes {
        get {
            foreach(KeyValuePair<string, Type> pair in base.dependencyTypes)
                yield return pair;
            yield return new KeyValuePair<string, Type>(ResourcePackSelectorTemplate.GlobalId,
                typeof(ResourcePackSelectorTemplate));
        }
    }

    private readonly IResources _resources;
    private TcpClient? _client;
    private Level? _level;

    private bool _connected;

    public GameScreen(IResources resources) => _resources = resources;

    public bool TryConnect(string address, [NotNullWhen(false)] out string? error) {
        string host = "127.0.0.1";
        int port = 12420;
        if(address.Length > 0) {
            string[] parts = address.Split(':');
            host = parts[0];
            if(parts.Length >= 2 && int.TryParse(parts[1], out int parsedPort))
                port = parsedPort;
        }

        _client = new TcpClient(new LiteClientOptions { Host = host, Port = port });
        _client.Error += (_, ex) => {
            logger.Error(ex);
        };
        _client.Connected += (_, _) => {
            _connected = true;
            logger.Info("connected");
        };
        _client.Disconnected += (_, _) => {
            _connected = false;
            logger.Info("disconnected");
        };
        Task connectTask = _client.ConnectAsync();

        if(!connectTask.Wait(10000)) {
            error = $"Failed to connect to {host}:{port} (connection timed out)";
            logger.Error(error);
            Close();
            return false;
        }

        if(!_connected) {
            error = $"Failed to connect to {host}:{port} (connection refused)";
            logger.Error(error);
            Close();
            return false;
        }

        error = null;
        return true;
    }

    public void Open() {
        _level = new Level(renderer, input, audio, _resources);
    }

    public void Close() {
        _level = null;
        if(_client is null)
            return;
        if(_connected) {
            Task disconnectTask = _client.DisconnectAsync();
            disconnectTask.Wait();
            _connected = false;
        }
        _client.Dispose();
        _client = null;
    }

    public void Update(TimeSpan time) {
        if(_level is not null) {
            _client?.ProcessPackets(_level);
            _level.Update(time);
        }
        foreach((string _, Element element) in elements)
            element.Update(time);
        if(input.KeyPressed(KeyCode.Escape) &&
            Core.engine.resources.TryGetResource(MainMenuScreen.GlobalId, out MainMenuScreen? screen))
            Core.engine.game.SwitchScreen(screen);
    }

    public void Tick(TimeSpan time) { }

    public void Dispose() {
        _client?.Dispose();
        _client = null;
        GC.SuppressFinalize(this);
    }
}
