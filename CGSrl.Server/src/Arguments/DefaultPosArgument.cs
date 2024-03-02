using CGSrl.Shared.Environment;

using PER.Util;

namespace CGSrl.Server.Arguments;

public class DefaultPosArgument(CoordinateArgument x, CoordinateArgument y) : IPosArgument {
    public bool xRelative => x.relative;
    public bool yRelative => y.relative;

    public Vector2Int ToAbsolutePos(PlayerObject? player) {
        Vector2Int position = player?.position ?? new Vector2Int();
        return new Vector2Int(x.ToAbsoluteCoordinate(position.x), y.ToAbsoluteCoordinate(position.y));
    }
}
