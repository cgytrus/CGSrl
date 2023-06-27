using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Cgsrl.Networking;
using Cgsrl.Screens.Templates;
using Cgsrl.Shared.Environment;
using Cgsrl.Shared.Networking;

using Lidgren.Network;

using NLog;

using PER.Abstractions;
using PER.Abstractions.Audio;
using PER.Abstractions.Environment;
using PER.Abstractions.Input;
using PER.Abstractions.Rendering;
using PER.Abstractions.Resources;
using PER.Abstractions.UI;
using PER.Common.Effects;
using PER.Util;

using PRR.UI;
using PRR.UI.Resources;

namespace Cgsrl.Screens;

public class GameScreen : LayoutResource, IScreen {
    public const string GlobalId = "layouts/game";

    private const float MessageFadeInTime = 0.5f;
    private const double MessageStayTime = 60d;
    private const float MessageFadeOutTime = 5f;

    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    protected override IRenderer renderer => Core.engine.renderer;
    protected override IInput input => Core.engine.input;
    protected override IAudio audio => Core.engine.audio;

    protected override string layoutName => "game";
    protected override IReadOnlyDictionary<string, Type> elementTypes { get; } = new Dictionary<string, Type> {
        { "players", typeof(LayoutResourceListBox<PlayerObject>) },
        { "chat.messages", typeof(LayoutResourceListBox<ChatMessage>) },
        { "chat.input", typeof(LayoutResourceInputField) },
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
            yield return new KeyValuePair<string, Type>(ChatMessageListTemplate.GlobalId, typeof(ChatMessageListTemplate));
        }
    }

    private bool _connecting;
    private string? _lastError;

    private readonly IResources _resources;
    private GameClient? _client;
    private Level<SyncedLevelObject>? _level;

    private ListBox<PlayerObject>? _players;
    private ListBox<ChatMessage>? _messages;
    private InputField? _chatInput;

    private SyncedLevelObject _spawnerCurrent;
    private readonly FloorObject _spawnerFloor = new() { layer = -1 };
    private readonly WallObject _spawnerWall = new() { layer = 1 };
    private readonly BoxObject _spawnerBox = new() { layer = 1 };
    private readonly EffectObject _spawnerEffect = new() { layer = 2 };

    private bool _prevEscapePressed;
    private bool _prevLeftPressed;
    private bool _prevRightPressed;

    public GameScreen(IResources resources) {
        _resources = resources;
        resources.TryAddResource(PlayerListTemplate.GlobalId, new PlayerListTemplate());
        resources.TryAddResource(ChatMessageListTemplate.GlobalId, new ChatMessageListTemplate());
        _spawnerCurrent = _spawnerWall;
    }

    public override void Load(string id) {
        base.Load(id);
        _players = GetElement<ListBox<PlayerObject>>("players");
        _messages = GetElement<ListBox<ChatMessage>>("chat.messages");

        _chatInput = GetElement<InputField>("chat.input");
        _chatInput.onSubmit += (_, _) => {
            SendChatMessage();
            _chatInput.value = null;
        };
        _chatInput.onCancel += (_, _) => {
            _chatInput.value = null;
        };

        GetElement<Button>("spawner.objects.floor").onClick += (_, _) => _spawnerCurrent = _spawnerFloor;
        GetElement<Button>("spawner.objects.wall").onClick += (_, _) => _spawnerCurrent = _spawnerWall;
        GetElement<Button>("spawner.objects.box").onClick += (_, _) => _spawnerCurrent = _spawnerBox;
        GetElement<Button>("spawner.objects.effect").onClick += (_, _) => _spawnerCurrent = _spawnerEffect;

        _level = new Level<SyncedLevelObject>(renderer, input, audio, _resources);
        _level.objectAdded += obj => {
            if(obj is PlayerObject player)
                _players?.Add(player);
        };
        _level.objectRemoved += obj => {
            if(obj is PlayerObject player)
                _players?.Remove(player);
        };

        _client = new GameClient(_level, _messages);
        _client.onConnect += Connected;
        _client.onDisconnect += Disconnected;
    }

    public override void Unload(string id) {
        base.Unload(id);
        _client?.Finish();
        _client = null;
        _level = null;
    }

    public bool TryConnect(string address, string username, string displayName,
        [NotNullWhen(false)] out string? error) {
        if(_client is null) {
            error = "client doesn't exist?????";
            return false;
        }

        string host = "127.0.0.1";
        int port = 12420;
        if(address.Length > 0) {
            string[] parts = address.Split(':');
            host = parts[0];
            if(parts.Length >= 2 && int.TryParse(parts[1], out int parsedPort))
                port = parsedPort;
        }

        _client.Connect(host, port, username, displayName);
        _connecting = true;
        // do minimal processing until we connect or disconnect
        while(_connecting)
            _client.ProcessMessages();
        error = _lastError;
        return _lastError is null;
    }

    private void Connected() {
        _connecting = false;
        _lastError = null;
    }

    private void Disconnected(string reason, bool isError) {
        _level?.Reset();
        _players?.Clear();
        _messages?.Clear();
        if(_connecting) {
            _connecting = false;
            if(isError)
                _lastError = reason;
        }
        else if(isError)
            SwitchToMainMenuWithError(reason);
        else if(Core.engine.resources.TryGetResource(MainMenuScreen.GlobalId, out MainMenuScreen? screen))
            Core.engine.game.SwitchScreen(screen);
    }

    public void Open() { }
    public void Close() { }

    public void Update(TimeSpan time) {
        bool prevBlock = input.block;
        bool block = _chatInput?.typing ?? false;

        input.block = block;
        if(_client is not null && _level is not null) {
            _client.ProcessMessages();
            UpdatePlayerList();
            UpdateChatMessageList();
            _level.Update(time);
        }
        input.block = prevBlock;

        foreach((string _, Element element) in elements)
            element.Update(time);

        if(block) {
            _prevEscapePressed = input.KeyPressed(KeyCode.Escape);
            _prevLeftPressed = input.MouseButtonPressed(MouseButton.Left);
            _prevRightPressed = input.MouseButtonPressed(MouseButton.Right);
            return;
        }

        bool escapePressed = input.KeyPressed(KeyCode.Escape);
        if(!_prevEscapePressed && escapePressed)
            _client?.Disconnect();
        _prevEscapePressed = escapePressed;

        if(_chatInput?.currentState == ClickableElement.State.Clicked) {
            _prevLeftPressed = input.MouseButtonPressed(MouseButton.Left);
            _prevRightPressed = input.MouseButtonPressed(MouseButton.Right);
            return;
        }

        UpdateSpawner();
    }

    private void UpdateSpawner() {
        if(_level is null || _client is null)
            return;

        if(input.mousePosition is { x: >= 100, y: <= 10 })
            return;

        bool leftPressed = input.MouseButtonPressed(MouseButton.Left);
        bool rightPressed = input.MouseButtonPressed(MouseButton.Right);

        if(!_prevLeftPressed && leftPressed &&
            !_level.HasObjectAt(_level.ScreenToLevelPosition(input.mousePosition), _spawnerCurrent.GetType()))
            CreateCurrentSpawnerObject();
        if(!_prevRightPressed && rightPressed)
            RemoveCurrentObject();

        _prevLeftPressed = leftPressed;
        _prevRightPressed = rightPressed;
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

        NetOutgoingMessage msg = _client.peer.CreateMessage(SyncedLevelObject.PreallocSize);
        msg.Write((byte)CtsDataType.AddObject);
        _spawnerCurrent.WriteTo(msg);
        _client.peer.SendMessage(msg, NetDeliveryMethod.ReliableOrdered, 0);
    }

    private void RemoveCurrentObject() {
        if(_client is null || _level is null ||
            !_level.TryGetObjectAt(_level.ScreenToLevelPosition(input.mousePosition), out SyncedLevelObject? obj) &&
            obj is not PlayerObject)
            return;

        NetOutgoingMessage msg = _client.peer.CreateMessage(17);
        msg.Write((byte)CtsDataType.RemoveObject);
        msg.Write(obj.id);
        _client.peer.SendMessage(msg, NetDeliveryMethod.ReliableOrdered, 0);
    }

    private void UpdatePlayerList() {
        if(_players is null)
            return;
        foreach(PlayerObject player in _players.items) {
            if(player.text is not Text text) {
                logger.Warn($"{player.username} doesnt have text????");
                continue;
            }
            player.highlighted = input.mousePosition.InBounds(text.bounds);
        }
    }

    private void UpdateChatMessageList() {
        if(_messages is null)
            return;
        foreach(ChatMessage message in _messages.items)
            UpdateChatMessage(message);
    }
    private void UpdateChatMessage(ChatMessage message) {
        if(_messages is null)
            return;
        if(message.element is null) {
            logger.Warn("message doesnt have element????");
            return;
        }
        if(message.player is not null)
            message.player.highlighted = message.player.highlighted ||
                input.mousePosition.InBounds(_messages.bounds) &&
                input.mousePosition.InBounds(message.element.bounds);
        if(message.element.formatting['\0'].effect is not FadeEffect { fading: false } fade)
            return;
        if(NetTime.Now - message.time >= MessageStayTime)
            fade.Start(MessageFadeOutTime, float.PositiveInfinity, () => _messages.Remove(message));
        else if(message.isNew)
            fade.Start(0f, MessageFadeInTime, () => message.isNew = false);
    }

    private void SendChatMessage() {
        if(_client is null || _chatInput is null || string.IsNullOrEmpty(_chatInput.value))
            return;
        NetOutgoingMessage msg = _client.peer.CreateMessage();
        msg.Write((byte)CtsDataType.ChatMessage);
        msg.WriteTime(false);
        msg.Write(_chatInput.value);
        _client.peer.SendMessage(msg, NetDeliveryMethod.ReliableOrdered, 0);
    }

    private static void SwitchToMainMenuWithError(string error) {
        if(Core.engine.resources.TryGetResource(MainMenuScreen.GlobalId, out MainMenuScreen? screen))
            Core.engine.game.SwitchScreen(screen, () => {
                screen.ShowConnectionError(error);
                return true;
            });
    }

    public void Tick(TimeSpan time) { }
}
