using System.Numerics;

using CGSrl.Shared.Networking;

using Lidgren.Network;

using PER.Abstractions;
using PER.Abstractions.Environment;
using PER.Abstractions.Input;
using PER.Abstractions.Rendering;
using PER.Util;

namespace CGSrl.Shared.Environment;

public class PlayerObject : MovableObject, IUpdatable, IMovable, ILight {
    public override int layer => 0;
    public override RenderCharacter character => new('@',
        highlighted ? new Color(1f, 1f, 0f, 0.2f) : Color.transparent, new Color(0, 255, 255, 255));
    public override bool blocksLight => false;

    protected override bool canPush => true;
    protected override float mass => 2f;
    protected override float strength => float.PositiveInfinity;

    // you fucking retarded bitch
#pragma warning disable CA1822
    public float brightness => connection is null ? 0f : 1f;
    public byte emission => connection is null ? (byte)0 : (byte)16;
    public byte visibility => connection is null ? (byte)0 : (byte)16;
#pragma warning restore CA1822

    public NetConnection? connection { get; set; }

    public bool highlighted { get; set; }

    public string username {
        get => _username;
        init => _username = value;
    }

    public string displayName {
        get => _displayName;
        init => _displayName = value;
    }

    public float ping {
        get => _ping;
        set {
            if(_ping != value)
                dirty = true;
            _ping = value;
        }
    }

    public bool pingDirty { get; set; }

    private string _username = "";
    private string _displayName = "";
    private float _ping;

    private const float MaxInteractionDistance = 3f;
    private bool _prevLeftPressed;
    public static InteractableObject? currentInteractable { get; private set; }

    private Vector2Int _prevMove;
    public Vector2Int move { get; set; }

    // client-side only, have to do this cuz it's in the shared project and i don't wanna depend on PRR.UI here xd
    public object? text { get; set; }

    public override void Added() => LoadChunks();

    public void Update(TimeSpan time) {
        if(connection is null)
            return;
        UpdateInteraction();
        UpdateMovement();
    }

    private void UpdateInteraction() {
        Vector2Int mouse = level.ScreenToLevelPosition(input.mousePosition);
        Vector2Int relativeMouse = mouse - position;
        float mouseDistSqr = new Vector2(relativeMouse.x, relativeMouse.y).LengthSquared();
        if(mouseDistSqr > MaxInteractionDistance * MaxInteractionDistance ||
            !level.TryGetObjectAt(mouse, out InteractableObject? obj)) {
            currentInteractable = null;
            return;
        }
        currentInteractable = obj;

        bool leftPressed = input.MouseButtonPressed(MouseButton.Left);
        if(!_prevLeftPressed && leftPressed)
            obj.Interact(this);
        _prevLeftPressed = leftPressed;
    }

    private void UpdateMovement() {
        if(connection is null)
            return;
        int moveX = 0;
        int moveY = 0;
        if(input.KeyPressed(KeyCode.D))
            moveX++;
        if(input.KeyPressed(KeyCode.A))
            moveX--;
        if(input.KeyPressed(KeyCode.S))
            moveY++;
        if(input.KeyPressed(KeyCode.W))
            moveY--;
        move = new Vector2Int(moveX, moveY);
        if(move == _prevMove)
            return;
        NetOutgoingMessage msg = connection.Peer.CreateMessage(1 + sizeof(int) * 2);
        msg.Write((byte)CtsDataType.PlayerMove);
        msg.Write(move);
        connection.SendMessage(msg, NetDeliveryMethod.ReliableOrdered, 0);
        _prevMove = move;
    }

    public override void Tick(TimeSpan time) {
        AddMovementForce(new Vector2(move.x, move.y));
        base.Tick(time);
        LoadChunks();
    }

    private void LoadChunks() {
        Vector2Int currentChunk = level.LevelToChunkPosition(position);
        for(int x = currentChunk.x - 5; x <= currentChunk.x + 5; x++)
            for(int y = currentChunk.y - 3; y <= currentChunk.y + 3; y++)
                level.LoadChunkAt(new Vector2Int(x, y));
    }

    public void Moved(Vector2Int from) {
        if(!level.isClient)
            return;
        Vector2Int delta = position - from;
        Vector2Int cameraPosition = level.LevelToCameraPosition(position);
        if(Math.Abs(cameraPosition.x) > 5)
            level.cameraPosition += new Vector2Int(delta.x, 0);
        if(Math.Abs(cameraPosition.y) > 5)
            level.cameraPosition += new Vector2Int(0, delta.y);
    }

    protected override void WriteStaticDataTo(NetBuffer buffer) {
        base.WriteStaticDataTo(buffer);
        buffer.Write(username);
        buffer.Write(displayName);
    }
    public override void WriteDynamicDataTo(NetBuffer buffer) {
        base.WriteDynamicDataTo(buffer);
        buffer.Write(ping);
    }

    protected override void ReadStaticDataFrom(NetBuffer buffer) {
        base.ReadStaticDataFrom(buffer);
        _username = buffer.ReadString();
        _displayName = buffer.ReadString();
    }
    public override void ReadDynamicDataFrom(NetBuffer buffer) {
        base.ReadDynamicDataFrom(buffer);
        _ping = buffer.ReadFloat();
        pingDirty = true;
    }
}
