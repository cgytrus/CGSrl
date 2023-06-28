﻿using System;
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
        { "loading.progress.center", typeof(LayoutResourceProgressBar) },
        { "loading.text.center", typeof(LayoutResourceText) },
        { "loading.progress.bottom", typeof(LayoutResourceProgressBar) },
        { "loading.text.bottom", typeof(LayoutResourceText) },
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
    private bool _joining;

    private readonly IResources _resources;
    private GameClient? _client;
    private Level<SyncedLevelObject>? _level;

    private Text? _loadingTextCenter;
    private Text? _loadingTextBottom;
    private ProgressBar? _loadingProgressCenter;
    private ProgressBar? _loadingProgressBottom;
    private string _loadingTextCenterFormat = "{0}";
    private string _loadingTextBottomFormat = "{0}";

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

        _loadingTextCenter = GetElement<Text>("loading.text.center");
        _loadingTextBottom = GetElement<Text>("loading.text.bottom");
        _loadingProgressCenter = GetElement<ProgressBar>("loading.progress.center");
        _loadingProgressBottom = GetElement<ProgressBar>("loading.progress.bottom");

        _loadingTextCenter.enabled = false;
        _loadingProgressCenter.enabled = false;
        _loadingTextBottom.enabled = false;
        _loadingProgressBottom.enabled = false;

        _loadingTextCenterFormat = _loadingTextCenter.text ?? _loadingTextCenterFormat;
        _loadingTextBottomFormat = _loadingTextBottom.text ?? _loadingTextBottomFormat;

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
        _client.onJoin += Joined;
        _client.onDisconnect += Disconnected;
    }

    public override void Unload(string id) {
        base.Unload(id);
        _client?.Finish();
        _client = null;
        _level = null;
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

        if(_loadingTextCenter is not null)
            _loadingTextCenter.enabled = true;
        if(_loadingProgressCenter is not null)
            _loadingProgressCenter.enabled = true;

        _client.Connect(host, port, username, displayName);
        _connecting = true;
    }

    private void Connected() {
        _connecting = false;
        _joining = true;
        if(_loadingTextCenter is null || _loadingProgressCenter is null ||
            _loadingTextBottom is null || _loadingProgressBottom is null)
            return;
        _loadingTextCenter.enabled = false;
        _loadingProgressCenter.enabled = false;
        _loadingTextBottom.enabled = true;
        _loadingProgressBottom.enabled = true;
    }

    private void Joined() {
        _joining = false;
        if(_loadingTextCenter is null || _loadingProgressCenter is null ||
            _loadingTextBottom is null || _loadingProgressBottom is null)
            return;
        _loadingTextCenter.enabled = false;
        _loadingProgressCenter.enabled = false;
        _loadingTextBottom.enabled = false;
        _loadingProgressBottom.enabled = false;
    }

    private void Disconnected(string reason, bool isError) {
        _level?.Reset();
        _players?.Clear();
        _messages?.Clear();
        _connecting = false;
        _joining = false;
        if(isError)
            SwitchToMainMenuWithError(reason);
        else
            SwitchToMainMenu();
    }

    public void Open() {
        if(_loadingProgressCenter is null || _loadingProgressBottom is null)
            return;
        _loadingProgressCenter.value = 0f;
        _loadingProgressBottom.value = 0f;
    }

    public void Close() {
        if(_loadingTextCenter is null || _loadingProgressCenter is null ||
            _loadingTextBottom is null || _loadingProgressBottom is null)
            return;
        _loadingTextCenter.enabled = false;
        _loadingProgressCenter.enabled = false;
        _loadingTextBottom.enabled = false;
        _loadingProgressBottom.enabled = false;
    }

    public void Update(TimeSpan time) {
        bool prevBlock = input.block;
        bool block = (_chatInput?.typing ?? false) || _connecting || _joining;

        if(_client is not null && _level is not null) {
            input.block = block;
            _client.ProcessMessages(renderer.framerate > 0 ? TimeSpan.FromSeconds(1d / renderer.framerate) :
                TimeSpan.Zero);
            UpdatePlayerList();
            UpdateChatMessageList();
            _level.Update(time);
            input.block = prevBlock;
        }

        if(_connecting)
            UpdateConnectingProgress();
        else if(_joining)
            UpdateJoiningProgress();

        foreach((string _, Element element) in elements)
            element.Update(time);

        UpdateInput(block);
    }

    private void UpdateConnectingProgress() {
        if(_loadingTextCenter is null || _loadingProgressCenter is null ||
            _loadingTextBottom is null || _loadingProgressBottom is null ||
            _client is null)
            return;
        string text = _client.peer.ConnectionStatus switch {
            NetConnectionStatus.None or NetConnectionStatus.Disconnected => "Connecting...",
            NetConnectionStatus.InitiatedConnect => "Waiting for response...",
            NetConnectionStatus.ReceivedInitiation => "huh ????",
            NetConnectionStatus.RespondedAwaitingApproval => "Waiting for approval...",
            NetConnectionStatus.RespondedConnect => "huh 2 ???",
            NetConnectionStatus.Connected => "Connected",
            NetConnectionStatus.Disconnecting => "Disconnecting...",
            _ => "Unknow..."
        };
        float progress = _client.peer.ConnectionStatus switch {
            NetConnectionStatus.None or NetConnectionStatus.Disconnected => 0f / 3f,
            NetConnectionStatus.InitiatedConnect => 1f / 3f,
            NetConnectionStatus.RespondedAwaitingApproval => 2f / 3f,
            NetConnectionStatus.Connected => 3f / 3f,
            _ => 0f
        };
        _loadingTextCenter.text = string.Format(_loadingTextCenterFormat, text);
        _loadingTextBottom.text = string.Format(_loadingTextBottomFormat, text);
        _loadingProgressCenter.value = progress;
        _loadingProgressBottom.value = progress;
    }

    private void UpdateJoiningProgress() {
        if(_loadingTextCenter is null || _loadingProgressCenter is null ||
            _loadingTextBottom is null || _loadingProgressBottom is null ||
            _client is null)
            return;
        int received = _client.totalJoinedObjectCount - _client.leftJoinedObjectCount;
        int total = _client.totalJoinedObjectCount;
        string text = $"Receiving objects... ({received}/{total})";
        _loadingTextCenter.text = string.Format(_loadingTextCenterFormat, text);
        _loadingTextBottom.text = string.Format(_loadingTextBottomFormat, text);
        float progress = Math.Clamp(received / (float)total, 0f, 1f);
        if(!float.IsNormal(progress))
            progress = 0f;
        _loadingProgressCenter.value = progress;
        _loadingProgressBottom.value = progress;
    }

    private void UpdateInput(bool block) {
        if(block) {
            _prevEscapePressed = input.KeyPressed(KeyCode.Escape);
            _prevLeftPressed = input.MouseButtonPressed(MouseButton.Left);
            _prevRightPressed = input.MouseButtonPressed(MouseButton.Right);
            return;
        }

        bool escapePressed = input.KeyPressed(KeyCode.Escape);
        if(!_prevEscapePressed && escapePressed) {
            if(_client is null)
                SwitchToMainMenu();
            else
                _client.Disconnect();
        }
        _prevEscapePressed = escapePressed;

        if(_connecting || _joining)
            return;

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
        if(message.element.effect is not FadeEffect { fading: false } fade)
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

    private static void SwitchToMainMenu() {
        if(Core.engine.resources.TryGetResource(MainMenuScreen.GlobalId, out MainMenuScreen? screen))
            Core.engine.game.SwitchScreen(screen);
    }

    public void Tick(TimeSpan time) { }
}
