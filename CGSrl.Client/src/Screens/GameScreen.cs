using System;
using System.Collections.Generic;

using CGSrl.Client.Networking;
using CGSrl.Client.Screens.Templates;
using CGSrl.Shared.Environment;
using CGSrl.Shared.Networking;

using Lidgren.Network;

using PER.Abstractions;
using PER.Abstractions.Audio;
using PER.Abstractions.Input;
using PER.Abstractions.Rendering;
using PER.Abstractions.Resources;
using PER.Abstractions.Screens;
using PER.Abstractions.UI;
using PER.Util;

using PRR.UI;
using PRR.UI.Resources;

namespace CGSrl.Client.Screens;

public class GameScreen : LayoutResource, IScreen, IUpdatable {
    public const string GlobalId = "layouts/game";

    private const int MaxMessageHistory = 100;

    protected override IRenderer renderer => Core.engine.renderer;
    protected override IInput input => Core.engine.input;
    protected override IAudio audio => Core.engine.audio;

    protected override string layoutName => "game";

    private GameClient? _client;

    private Text? _loadingText;
    private ProgressBar? _loadingProgress;
    private string _loadingTextFormat = "{0}";

    private ListBox<PlayerObject>? _players;
    private ListBox<ChatMessage>? _messages;
    private InputField? _chatInput;
    private readonly List<string> _messageHistory = new();

    private Text? _interactableText;
    private string _interactableFormat = "{0}";

    private Text? _infoText;
    private string _infoFormat = "{0} {1} {2}";

    private Type _spawnerCurrent;

    private bool _prevEscapePressed;
    private bool _prevTPressed;
    private bool _prevUpPressed;
    private bool _prevDownPressed;

    private bool _prevLeftPressed;
    private bool _prevRightPressed;

    public GameScreen(IResources resources) {
        resources.TryAddResource(PlayerListTemplate.GlobalId, new PlayerListTemplate());
        resources.TryAddResource(ChatMessageListTemplate.GlobalId, new ChatMessageListTemplate());
        _spawnerCurrent = typeof(WallObject);
    }

    public override void Preload() {
        base.Preload();
        AddDependency<PlayerListTemplate>(PlayerListTemplate.GlobalId);
        AddDependency<ChatMessageListTemplate>(ChatMessageListTemplate.GlobalId);

        AddElement<ProgressBar>("loading.progress");
        AddElement<Text>("loading.text");
        AddElement<ListBox<PlayerObject>>("players");
        AddElement<ListBox<ChatMessage>>("chat.messages");
        AddElement<InputField>("chat.input");
        AddElement<Text>("interactablePrompt");
        AddElement<Text>("info");
        AddElement<Button>("spawner.objects.floor");
        AddElement<Button>("spawner.objects.wall");
        AddElement<Button>("spawner.objects.box");
        AddElement<Button>("spawner.objects.ice");
        AddElement<Button>("spawner.objects.message");
        AddElement<Button>("spawner.objects.grass");
        AddElement<Button>("spawner.objects.bomb");
        AddElement<Button>("spawner.objects.light");
        AddElement<Button>("spawner.objects.redLight");
        AddElement<Button>("spawner.objects.greenLight");
        AddElement<Button>("spawner.objects.blueLight");
    }

    public override void Load(string id) {
        base.Load(id);

        _loadingText = GetElement<Text>("loading.text");
        _loadingProgress = GetElement<ProgressBar>("loading.progress");

        _loadingTextFormat = _loadingText.text ?? _loadingTextFormat;

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

        _interactableText = GetElement<Text>("interactablePrompt");
        _interactableFormat = _interactableText.text ?? _interactableFormat;

        _infoText = GetElement<Text>("info");
        _infoFormat = _infoText.text ?? _infoFormat;

        GetElement<Button>("spawner.objects.floor").onClick += (_, _) => _spawnerCurrent = typeof(FloorObject);
        GetElement<Button>("spawner.objects.wall").onClick += (_, _) => _spawnerCurrent = typeof(WallObject);
        GetElement<Button>("spawner.objects.box").onClick += (_, _) => _spawnerCurrent = typeof(BoxObject);
        GetElement<Button>("spawner.objects.ice").onClick += (_, _) => _spawnerCurrent = typeof(IceObject);
        GetElement<Button>("spawner.objects.message").onClick += (_, _) => _spawnerCurrent = typeof(MessageObject);
        GetElement<Button>("spawner.objects.grass").onClick += (_, _) => _spawnerCurrent = typeof(GrassObject);
        GetElement<Button>("spawner.objects.bomb").onClick += (_, _) => _spawnerCurrent = typeof(BombObject);
        GetElement<Button>("spawner.objects.light").onClick += (_, _) => _spawnerCurrent = typeof(LightObject);
        GetElement<Button>("spawner.objects.redLight").onClick += (_, _) => _spawnerCurrent = typeof(RedLightObject);
        GetElement<Button>("spawner.objects.greenLight").onClick += (_, _) => _spawnerCurrent = typeof(GreenLightObject);
        GetElement<Button>("spawner.objects.blueLight").onClick += (_, _) => _spawnerCurrent = typeof(BlueLightObject);
        ToggleSpawner(false);
    }

    public void ContinueConnect(GameClient client) {
        _client = client;
        client.onJoin += Joined;
        client.onDisconnect += Disconnected;
        if(_loadingText is not null && _loadingProgress is not null)
            client.SetUi(_loadingText, _loadingTextFormat, _loadingProgress, _players, _messages);
    }

    private void Joined() => ToggleSpawner(_client?.level?.gameMode.allowAddingObjects ?? false);

    private void Disconnected(string reason, bool isError) => ToggleSpawner(false);

    public void Open() { }
    public void Close() {
        if(_client is null)
            return;
        _client.onJoin -= Joined;
        _client.onDisconnect -= Disconnected;
    }

    private void ToggleSpawner(bool enabled) {
        foreach((string id, Element element) in elements)
            if(id.StartsWith("spawner.", StringComparison.Ordinal))
                element.enabled = enabled;
    }

    public void Update(TimeSpan time) {
        bool prevBlock = input.block;
        bool block = (_chatInput?.typing ?? false) || _client is not null && _client.joining;

        if(_client is not null) {
            input.block = block;
            _client.Update(time);
            UpdateInteractablePrompt();
            UpdateInfoText();
            _client.level?.Update(time);
            input.block = prevBlock;
        }

        // ReSharper disable once ForCanBeConvertedToForeach
        for(int i = 0; i < elementList.Count; i++)
            elementList[i].Update(time);

        UpdateInput(block);
    }

    private void UpdateInput(bool block) {
        if(_chatInput?.typing ?? false)
            UpdateChatHistoryInput();

        if(block) {
            _prevEscapePressed = input.KeyPressed(KeyCode.Escape);
            _prevTPressed = input.KeyPressed(KeyCode.T);
            _prevLeftPressed = input.MouseButtonPressed(MouseButton.Left);
            _prevRightPressed = input.MouseButtonPressed(MouseButton.Right);
            return;
        }

        UpdateExitHotkey();

        if(_client is not null && _client.joining)
            return;

        UpdateChatHotkey();

        if(_chatInput?.currentState == ClickableElement.State.Clicked) {
            _prevLeftPressed = input.MouseButtonPressed(MouseButton.Left);
            _prevRightPressed = input.MouseButtonPressed(MouseButton.Right);
            return;
        }

        UpdateSpawner();
    }

    private void UpdateChatHistoryInput() {
        if(_chatInput is null)
            return;

        bool upPressed = input.KeyPressed(KeyCode.Up);
        bool up = !_prevUpPressed && upPressed;
        _prevUpPressed = upPressed;

        bool downPressed = input.KeyPressed(KeyCode.Down);
        bool down = !_prevDownPressed && downPressed;
        _prevDownPressed = downPressed;

        if(!up && !down)
            return;

        int index = _messageHistory.IndexOf(_chatInput.value ?? "") + (up ? -1 : 1);

        while(index >= _messageHistory.Count) index -= _messageHistory.Count + 1;
        while(index < -1) index += _messageHistory.Count + 1;

        _chatInput.value = index >= 0 && index < _messageHistory.Count ? _messageHistory[index] : null;
        _chatInput.cursor = _chatInput.value?.Length ?? 0;
    }

    private void RemoveOldHistory() {
        for(int i = 0; i < _messageHistory.Count - MaxMessageHistory; i++)
            _messageHistory.RemoveAt(i);
    }

    private void UpdateExitHotkey() {
        bool escapePressed = input.KeyPressed(KeyCode.Escape);
        if(!_prevEscapePressed && escapePressed)
            _client?.Disconnect();
        _prevEscapePressed = escapePressed;
    }

    private void UpdateChatHotkey() {
        bool tPressed = input.KeyPressed(KeyCode.T);
        if(!_prevTPressed && tPressed && _chatInput is not null)
            _chatInput.StartTyping();
        _prevTPressed = tPressed;
    }

    private void UpdateSpawner() {
        if(_client?.level is null)
            return;

        if(input.mousePosition is { x: >= 100, y: <= 10 })
            return;

        bool leftPressed = input.MouseButtonPressed(MouseButton.Left);
        bool rightPressed = input.MouseButtonPressed(MouseButton.Right);

        if(_client.level.gameMode.allowAddingObjects && !_prevLeftPressed && leftPressed &&
            !_client.level.HasObjectAt(_client.level.ScreenToLevelPosition(input.mousePosition), _spawnerCurrent))
            CreateCurrentSpawnerObject();
        if(_client.level.gameMode.allowRemovingObjects && !_prevRightPressed && rightPressed)
            RemoveCurrentObject();

        _prevLeftPressed = leftPressed;
        _prevRightPressed = rightPressed;
    }

    private void CreateCurrentSpawnerObject() {
        if(_client?.level is null)
            return;

        if(Activator.CreateInstance(_spawnerCurrent) is not SyncedLevelObject newObj)
            return;

        newObj.position = _client.level.ScreenToLevelPosition(input.mousePosition);

        NetOutgoingMessage msg = _client.peer.CreateMessage(SyncedLevelObject.PreallocSize);
        msg.Write((byte)CtsDataType.AddObject);
        newObj.WriteTo(msg);
        _client.peer.SendMessage(msg, NetDeliveryMethod.ReliableOrdered, 0);
    }

    private void RemoveCurrentObject() {
        if(_client?.level is null ||
            !_client.level.TryGetObjectAt(_client.level.ScreenToLevelPosition(input.mousePosition),
                out SyncedLevelObject? obj) &&
            obj is not PlayerObject)
            return;

        NetOutgoingMessage msg = _client.peer.CreateMessage(17);
        msg.Write((byte)CtsDataType.RemoveObject);
        msg.Write(obj.id);
        _client.peer.SendMessage(msg, NetDeliveryMethod.ReliableOrdered, 0);
    }

    private void SendChatMessage() {
        if(_client is null || _chatInput is null || string.IsNullOrEmpty(_chatInput.value))
            return;

        NetOutgoingMessage msg = _client.peer.CreateMessage();
        msg.Write((byte)CtsDataType.ChatMessage);
        msg.WriteTime(false);
        msg.Write(_chatInput.value);
        _client.peer.SendMessage(msg, NetDeliveryMethod.ReliableOrdered, 0);

        // remove the current message so that if it's already in the history
        // it's moved to the end of the history
        _messageHistory.Remove(_chatInput.value);
        _messageHistory.Add(_chatInput.value);
        RemoveOldHistory();
    }

    private void UpdateInteractablePrompt() {
        if(_interactableText is null || _client?.level is null)
            return;
        switch(PlayerObject.currentInteractable) {
            case null:
                _interactableText.text = string.Empty;
                _interactableText.position = new Vector2Int(-1, -1);
                break;
            case SyncedLevelObject obj:
                _interactableText.text = string.Format(_interactableFormat, PlayerObject.currentInteractable.prompt);
                _interactableText.position = _client.level.LevelToScreenPosition(obj.position + new Vector2Int(1, -1));
                break;
        }
    }

    private void UpdateInfoText() {
        if(_infoText is null || _client?.level is null)
            return;
        _infoText.text = string.Format(_infoFormat,
            input.mousePosition,
            _client.level.ScreenToCameraPosition(input.mousePosition),
            _client.level.ScreenToLevelPosition(input.mousePosition),
            _client.level.ScreenToChunkPosition(input.mousePosition));
    }
}
