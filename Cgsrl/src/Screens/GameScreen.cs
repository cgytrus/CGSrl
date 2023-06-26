using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

using Cgsrl.Networking;
using Cgsrl.Screens.Templates;
using Cgsrl.Shared.Environment;
using Cgsrl.Shared.Networking.Packets.ClientToServer;

using LiteNetwork.Client;

using NLog;

using PER.Abstractions;
using PER.Abstractions.Audio;
using PER.Abstractions.Environment;
using PER.Abstractions.Input;
using PER.Abstractions.Rendering;
using PER.Abstractions.Resources;
using PER.Abstractions.UI;
using PER.Util;

using PRR.UI;
using PRR.UI.Resources;

namespace Cgsrl.Screens;

public class GameScreen : LayoutResource, IScreen, IDisposable {
    public const string GlobalId = "layouts/game";

    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    protected override IRenderer renderer => Core.engine.renderer;
    protected override IInput input => Core.engine.input;
    protected override IAudio audio => Core.engine.audio;

    protected override string layoutName => "game";
    protected override IReadOnlyDictionary<string, Type> elementTypes { get; } = new Dictionary<string, Type> {
        { "players", typeof(LayoutResourceListBox<PlayerObject>) },
        { "spawner.objects.floor", typeof(LayoutResourceButton) },
        { "spawner.objects.wall", typeof(LayoutResourceButton) },
        { "spawner.objects.box", typeof(LayoutResourceButton) },
        { "spawner.objects.effect", typeof(LayoutResourceButton) },
        { "spawner.width", typeof(LayoutResourceInputField) },
        { "spawner.height", typeof(LayoutResourceInputField) },
        { "spawner.effect", typeof(LayoutResourceInputField) }
    };

    protected override IEnumerable<KeyValuePair<string, Type>> dependencyTypes {
        get {
            foreach(KeyValuePair<string, Type> pair in base.dependencyTypes)
                yield return pair;
            yield return new KeyValuePair<string, Type>(PlayerListTemplate.GlobalId, typeof(PlayerListTemplate));
        }
    }

    private string _address = "";
    private bool _connected;

    private readonly IResources _resources;
    private TcpClient? _client;
    private Level? _level;

    private ListBox<PlayerObject>? _players;

    private LevelObject _spawnerCurrent;
    private readonly FloorObject _spawnerFloor = new() { layer = -1 };
    private readonly WallObject _spawnerWall = new() { layer = 1 };
    private readonly BoxObject _spawnerBox = new() { layer = 1 };
    private readonly EffectObject _spawnerEffect = new() { layer = 2 };

    public GameScreen(IResources resources) {
        _resources = resources;
        resources.TryAddResource(PlayerListTemplate.GlobalId, new PlayerListTemplate());
        _spawnerCurrent = _spawnerWall;
    }

    public override void Load(string id) {
        base.Load(id);
        _players = GetElement<ListBox<PlayerObject>>("players");

        GetElement<Button>("spawner.objects.floor").onClick += (_, _) => _spawnerCurrent = _spawnerFloor;
        GetElement<Button>("spawner.objects.wall").onClick += (_, _) => _spawnerCurrent = _spawnerWall;
        GetElement<Button>("spawner.objects.box").onClick += (_, _) => _spawnerCurrent = _spawnerBox;
        GetElement<Button>("spawner.objects.effect").onClick += (_, _) => _spawnerCurrent = _spawnerEffect;
    }

    public bool TryConnect(string address, string username, string displayName,
        [NotNullWhen(false)] out string? error) {
        string host = "127.0.0.1";
        int port = 12420;
        if(address.Length > 0) {
            string[] parts = address.Split(':');
            host = parts[0];
            if(parts.Length >= 2 && int.TryParse(parts[1], out int parsedPort))
                port = parsedPort;
        }

        _address = $"{host}:{port}";

        _client = new TcpClient(new LiteClientOptions { Host = host, Port = port });
        _client.Error += (_, ex) => {
            logger.Error(ex);
        };
        _client.Connected += (_, _) => {
            _connected = true;
            logger.Info($"Connected to {_address}");
            _client.Send(new AuthorizePacket(username, displayName).Serialize());
        };
        _client.Disconnected += (_, _) => {
            _connected = false;
            logger.Info($"Disconnected from {_address}");
        };
        Task connectTask = _client.ConnectAsync();

        if(!connectTask.Wait(10000)) {
            error = $"Failed to connect to {_address} (connection timed out)";
            logger.Error(error);
            Close();
            return false;
        }

        if(!_connected) {
            error = $"Failed to connect to {_address} (connection refused)";
            logger.Error(error);
            Close();
            return false;
        }

        error = null;
        return true;
    }

    public void Open() {
        _players?.Clear();
        _level = new Level(renderer, input, audio, _resources);
        _level.objectAdded += obj => {
            if(obj is PlayerObject player)
                _players?.Add(player);
        };
        _level.objectRemoved += obj => {
            if(obj is PlayerObject player)
                _players?.Remove(player);
        };
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
        if(_client is not null && _level is not null) {
            if(!_client.ProcessPackets(_level, out string? error))
                SwitchToMainMenuWithError(error);
            UpdatePlayerList();
            _level.Update(time);
        }

        foreach((string _, Element element) in elements)
            element.Update(time);

        if(input.KeyPressed(KeyCode.Escape) &&
            Core.engine.resources.TryGetResource(MainMenuScreen.GlobalId, out MainMenuScreen? screen))
            Core.engine.game.SwitchScreen(screen);

        UpdateSpawner();
    }

    private void UpdateSpawner() {
        if(_level is null || _client is null)
            return;

        if(input.mousePosition is { x: >= 100, y: <= 10 })
            return;

        if(input.MouseButtonPressed(MouseButton.Left) &&
            !_level.HasObjectAt(_level.ScreenToLevelPosition(input.mousePosition), _spawnerCurrent.GetType()))
            CreateCurrentSpawnerObject();
        if(input.MouseButtonPressed(MouseButton.Right) &&
            _level.TryGetObjectAt(_level.ScreenToLevelPosition(input.mousePosition), out LevelObject? obj) &&
            obj is not PlayerObject)
            _client.Send(new RemoveObjectPacket(obj.id).Serialize());
    }

    private void CreateCurrentSpawnerObject() {
        if(_level is null || _client is null)
            return;

        _spawnerCurrent.id = Guid.NewGuid();
        _spawnerCurrent.position = _level.ScreenToLevelPosition(input.mousePosition);

        if(_spawnerCurrent is EffectObject effectObject) {
            if(int.TryParse(GetElement<InputField>("spawner.width").value, out int width) &&
                int.TryParse(GetElement<InputField>("spawner.height").value, out int height))
                effectObject.size = new Vector2Int(width, height);
            effectObject.effect = GetElement<InputField>("spawner.effect").value ?? "none";
        }

        _client.Send(new CreateObjectPacket(_spawnerCurrent).Serialize());
    }

    private void UpdatePlayerList() {
        if(_players is null)
            return;
        for(int i = 0; i < _players.items.Count; i++)
            _players[i].highlighted = input.mousePosition.InBounds(_players.elements[i].bounds);
    }

    private void SwitchToMainMenuWithError(string error) {
        if(Core.engine.resources.TryGetResource(MainMenuScreen.GlobalId, out MainMenuScreen? screen))
            Core.engine.game.SwitchScreen(screen, () => {
                screen.ShowConnectionError($"Failed to connect to {_address} ({error})");
                return true;
            });
    }

    public void Tick(TimeSpan time) { }

    public void Dispose() {
        _client?.Dispose();
        _client = null;
        GC.SuppressFinalize(this);
    }
}
