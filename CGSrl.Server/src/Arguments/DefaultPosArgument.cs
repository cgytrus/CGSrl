using CGSrl.Shared.Environment;

using PER.Util;

namespace CGSrl.Server.Arguments;

public class DefaultPosArgument : IPosArgument {
    public bool xRelative => _x.relative;
    public bool yRelative => _y.relative;

    private readonly CoordinateArgument _x;
    private readonly CoordinateArgument _y;

    public DefaultPosArgument(CoordinateArgument x, CoordinateArgument y) {
        _x = x;
        _y = y;
    }

    public Vector2Int ToAbsolutePos(PlayerObject? player) {
        Vector2Int position = player?.position ?? new Vector2Int();
        return new Vector2Int(_x.ToAbsoluteCoordinate(position.x), _y.ToAbsoluteCoordinate(position.y));
    }
}
