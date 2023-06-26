using Cgsrl.Shared.Networking.Packets.ClientToServer;

using LiteNetwork;

using PER.Abstractions.Environment;
using PER.Abstractions.Input;
using PER.Abstractions.Rendering;
using PER.Util;

namespace Cgsrl.Shared.Environment;

public class PlayerObject : LevelObject {
    protected override RenderCharacter character => new('@',
        highlighted ? new Color(1f, 1f, 0f, 0.2f) : Color.transparent, new Color(0, 255, 255, 255));

    public LiteConnection? connection { get; set; }

    public bool highlighted { get; set; }

    public string username {
        get => _username;
        init => _username = value;
    }

    public string displayName {
        get => _displayName;
        init => _displayName = value;
    }

    private string _username = "";
    private string _displayName = "";

    public Vector2Int move { get; set; }

    private Vector2Int _prevClientMove;
    private Vector2Int _clientMove;

    private Vector2Int _prevPosition;

    public override void Update(TimeSpan time) {
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
        _clientMove = new Vector2Int(moveX, moveY);
        if(_clientMove != _prevClientMove) {
            connection.Send(new PlayerMovePacket(_clientMove).Serialize());
            _prevClientMove = _clientMove;
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
    public override void Tick(TimeSpan time) {
        int moveX = this.move.x;
        int moveY = this.move.y;

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

        Vector2Int move = new(moveX, moveY);
        if(level.TryGetObjectAt(position + move, out PushableObject? pushable) && !pushable.TryMove(move))
            return;
        position += move;
    }

    public override void CustomSerialize(BinaryWriter writer) {
        writer.Write(username);
        writer.Write(displayName);
    }

    public override void CustomDeserialize(BinaryReader reader) {
        _username = reader.ReadString();
        _displayName = reader.ReadString();
    }
}
