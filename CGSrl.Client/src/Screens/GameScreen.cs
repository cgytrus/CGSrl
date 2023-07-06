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
using PER.Util;

using PRR.UI;
using PRR.UI.Resources;

namespace CGSrl.Client.Screens;

public abstract class GameScreen : LayoutResource, IScreen, IUpdatable {
    private const int MaxMessageHistory = 100;

    protected override IRenderer renderer => Core.engine.renderer;
    protected override IInput input => Core.engine.input;
    protected override IAudio audio => Core.engine.audio;

    protected GameClient client => _client!;
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

    private bool _prevEscapePressed;
    private bool _prevTPressed;
    private bool _prevUpPressed;
    private bool _prevDownPressed;

    protected GameScreen(IResources resources) {
        resources.TryAddResource(PlayerListTemplate.GlobalId, new PlayerListTemplate());
        resources.TryAddResource(ChatMessageListTemplate.GlobalId, new ChatMessageListTemplate());
    }

    public override void Preload() {
        base.Preload();
        AddDependency<PlayerListTemplate>(PlayerListTemplate.GlobalId);
        AddDependency<ChatMessageListTemplate>(ChatMessageListTemplate.GlobalId);
        AddLayout("game");
        AddElement<ProgressBar>("loading.progress");
        AddElement<Text>("loading.text");
        AddElement<ListBox<PlayerObject>>("players");
        AddElement<ListBox<ChatMessage>>("chat.messages");
        AddElement<InputField>("chat.input");
        AddElement<Text>("interactablePrompt");
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
    }

    public void ContinueConnect(GameClient client) {
        _client = client;
        client.onJoin += Joined;
        client.onDisconnect += Disconnected;
        if(_loadingText is not null && _loadingProgress is not null)
            client.SetUi(_loadingText, _loadingTextFormat, _loadingProgress, _players, _messages);
    }

    protected virtual void Joined() { }
    protected virtual void Disconnected(string reason, bool isError) { }

    public void Open() { }
    public void Close() {
        if(_client is null)
            return;
        _client.onJoin -= Joined;
        _client.onDisconnect -= Disconnected;
    }

    public void Update(TimeSpan time) {
        bool prevBlock = input.block;
        bool block = (_chatInput?.typing ?? false) || _client is not null && _client.joining;

        if(_client is not null) {
            input.block = block;
            _client.Update(time);
            UpdateInteractablePrompt();
            UpdateInterface();
            _client.level?.Update(time);
            input.block = prevBlock;
        }

        // ReSharper disable once ForCanBeConvertedToForeach
        for(int i = 0; i < elementList.Count; i++)
            elementList[i].Update(time);

        UpdateInput(block || _chatInput?.currentState == ClickableElement.State.Clicked);
    }

    protected virtual void UpdateInterface() { }

    protected virtual void UpdateInput(bool block) {
        if(_chatInput?.typing ?? false)
            UpdateChatHistoryInput();

        if(block) {
            _prevEscapePressed = input.KeyPressed(KeyCode.Escape);
            _prevTPressed = input.KeyPressed(KeyCode.T);
            return;
        }

        UpdateExitHotkey();

        if(_client is not null && _client.joining)
            return;

        UpdateChatHotkey();
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
}
