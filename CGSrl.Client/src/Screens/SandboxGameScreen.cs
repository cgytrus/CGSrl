using System;

using CGSrl.Shared.Environment;
using CGSrl.Shared.Networking;

using Lidgren.Network;

using PER.Abstractions.Audio;
using PER.Abstractions.Input;
using PER.Abstractions.Rendering;
using PER.Abstractions.Resources;

using PRR.UI;

namespace CGSrl.Client.Screens;

public class SandboxGameScreen(IResources resources) : GameScreen(resources) {
    public const string GlobalId = "layouts/game/sandbox";

    protected override IRenderer renderer => Core.engine.renderer;
    protected override IInput input => Core.engine.input;
    protected override IAudio audio => Core.engine.audio;

    private Text? _infoText;
    private string _infoFormat = "{0} {1} {2}";

    private Type _spawnerCurrent = typeof(WallObject);

    private bool _prevLeftPressed;
    private bool _prevRightPressed;

    public override void Preload() {
        base.Preload();
        AddLayout("game/sandbox");
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

    protected override void Joined() => ToggleSpawner(client.level?.gameMode.allowAddingObjects ?? false);
    protected override void Disconnected(string reason, bool isError) => ToggleSpawner(false);

    private void ToggleSpawner(bool enabled) {
        foreach((string id, Element element) in elements)
            if(id.StartsWith("spawner.", StringComparison.Ordinal))
                element.enabled = enabled;
    }

    protected override void UpdateInterface() => UpdateInfoText();

    protected override void UpdateInput(bool block) {
        base.UpdateInput(block);

        if(block) {
            _prevLeftPressed = input.MouseButtonPressed(MouseButton.Left);
            _prevRightPressed = input.MouseButtonPressed(MouseButton.Right);
            return;
        }

        UpdateSpawner();
    }

    private void UpdateSpawner() {
        if(client.level is null)
            return;

        if(input.mousePosition is { x: >= 100, y: <= 10 })
            return;

        bool leftPressed = input.MouseButtonPressed(MouseButton.Left);
        bool rightPressed = input.MouseButtonPressed(MouseButton.Right);

        if(client.level.gameMode.allowAddingObjects && !_prevLeftPressed && leftPressed &&
            !client.level.HasObjectAt(client.level.ScreenToLevelPosition(input.mousePosition), _spawnerCurrent))
            CreateCurrentSpawnerObject();
        if(client.level.gameMode.allowRemovingObjects && !_prevRightPressed && rightPressed)
            RemoveCurrentObject();

        _prevLeftPressed = leftPressed;
        _prevRightPressed = rightPressed;
    }

    private void CreateCurrentSpawnerObject() {
        if(client.level is null)
            return;

        if(Activator.CreateInstance(_spawnerCurrent) is not SyncedLevelObject newObj)
            return;

        newObj.position = client.level.ScreenToLevelPosition(input.mousePosition);

        NetOutgoingMessage msg = client.peer.CreateMessage(SyncedLevelObject.PreallocSize);
        msg.Write((byte)CtsDataType.AddObject);
        newObj.WriteTo(msg);
        client.peer.SendMessage(msg, NetDeliveryMethod.ReliableOrdered, 0);
    }

    private void RemoveCurrentObject() {
        if(client.level is null ||
            !client.level.TryGetObjectAt(client.level.ScreenToLevelPosition(input.mousePosition),
                out SyncedLevelObject? obj) &&
            obj is not PlayerObject)
            return;

        NetOutgoingMessage msg = client.peer.CreateMessage(17);
        msg.Write((byte)CtsDataType.RemoveObject);
        msg.Write(obj.id);
        client.peer.SendMessage(msg, NetDeliveryMethod.ReliableOrdered, 0);
    }

    private void UpdateInfoText() {
        if(_infoText is null || client.level is null)
            return;
        _infoText.text = string.Format(_infoFormat,
            input.mousePosition,
            client.level.ScreenToCameraPosition(input.mousePosition),
            client.level.ScreenToLevelPosition(input.mousePosition),
            client.level.ScreenToChunkPosition(input.mousePosition));
    }
}
