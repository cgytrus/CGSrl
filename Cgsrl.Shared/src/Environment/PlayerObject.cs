using System.Numerics;

using Cgsrl.Shared.Networking;

using Lidgren.Network;

using PER.Abstractions;
using PER.Abstractions.Environment;
using PER.Abstractions.Input;
using PER.Abstractions.Rendering;
using PER.Util;

namespace Cgsrl.Shared.Environment;

public class PlayerObject : SyncedLevelObject, IAddable, IUpdatable, ITickable, IMovable {
    protected override RenderCharacter character => new('@',
        highlighted ? new Color(1f, 1f, 0f, 0.2f) : Color.transparent, new Color(0, 255, 255, 255));

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

    private int _iceMoveTime;
    private int _iceMoveSpeed;
    private Vector2Int _lastNonZeroMove;

    // client-side only, have to do this cuz it's in the shared project and i don't wanna depend on PRR.UI here xd
    public object? text { get; set; }

    private Vector2Int _prevPosition;

    public void Added() => Moved();

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
        if(move != _prevMove) {
            NetOutgoingMessage msg = connection.Peer.CreateMessage(1 + sizeof(int) * 2);
            msg.Write((byte)CtsDataType.PlayerMove);
            msg.Write(move);
            connection.SendMessage(msg, NetDeliveryMethod.ReliableOrdered, 0);
            _prevMove = move;
        }

        if(position == _prevPosition)
            return;
        Vector2Int delta = position - _prevPosition;
        Vector2Int cameraPosition = level.LevelToCameraPosition(position);
        if(Math.Abs(cameraPosition.x) > 5)
            level.cameraPosition += new Vector2Int(delta.x, 0);
        if(Math.Abs(cameraPosition.y) > 5)
            level.cameraPosition += new Vector2Int(0, delta.y);
        _prevPosition = position;
    }

    // shtu up
    // ReSharper disable once CognitiveComplexity
    public void Tick(TimeSpan time) {
        int moveX = this.move.x;
        int moveY = this.move.y;
        bool iceSlowingDown = false;
        if(moveX == 0 && moveY == 0) {
            if(level.HasObjectAt<IceObject>(position)) {
                moveX = _lastNonZeroMove.x;
                moveY = _lastNonZeroMove.y;
                iceSlowingDown = _iceMoveSpeed < 4;
                if(!iceSlowingDown)
                    return;
            }
            else {
                _iceMoveTime = 0;
                _iceMoveSpeed = 0;
                return;
            }
        }
        if(!iceSlowingDown)
            _lastNonZeroMove = new Vector2Int(moveX, moveY);

        bool isDiagonal = moveX != 0 && moveY != 0;
        bool collidesHorizontal = moveX != 0 && level.HasObjectAt<WallObject>(position + new Vector2Int(moveX, 0));
        bool collidesVertical = moveY != 0 && level.HasObjectAt<WallObject>(position + new Vector2Int(0, moveY));
        bool collidesDiagonal = isDiagonal && level.HasObjectAt<WallObject>(position + new Vector2Int(moveX, moveY));

        if(isDiagonal &&
            (collidesHorizontal && collidesVertical || !collidesHorizontal && !collidesVertical && collidesDiagonal)) {
            moveX = 0;
            moveY = 0;
        }
        else if(!isDiagonal || collidesDiagonal) {
            if(collidesHorizontal)
                moveX = 0;
            if(collidesVertical)
                moveY = 0;
        }

        PushableObject? pushable;
        Vector2Int move = new(moveX, moveY);
        if(!level.HasObjectAt<IceObject>(position)) {
            _iceMoveTime = 0;
            _iceMoveSpeed = 0;
            if(level.TryGetObjectAt(position + move, out pushable) && !pushable.TryMove(move))
                return;
            position += move;
            _lastNonZeroMove = move;
            return;
        }
        if(_iceMoveTime < _iceMoveSpeed) {
            _iceMoveTime++;
            return;
        }
        if(iceSlowingDown) {
            if(_iceMoveSpeed < 4) {
                _iceMoveSpeed++;
                _iceMoveTime = 0;
            }
        }
        else {
            if(_iceMoveSpeed > 0) {
                _iceMoveSpeed--;
                _iceMoveTime = 0;
            }
        }
        if(level.TryGetObjectAt(position + move, out pushable) && !pushable.TryMove(move))
            return;
        position += move;
    }

    public void Moved() {
        if(level.isClient)
            return;
        Vector2Int currentChunk = level.LevelToChunkPosition(position);
        for(int x = currentChunk.x - 5; x <= currentChunk.x + 5; x++)
            for(int y = currentChunk.y - 3; y <= currentChunk.y + 3; y++)
                level.CreateChunkAt(new Vector2Int(x, y));
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
