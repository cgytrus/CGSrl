﻿using CGSrl.Shared.Environment;

using PER.Util;

namespace CGSrl.Server.Arguments;

public interface IPosArgument {
    public bool xRelative { get; }
    public bool yRelative { get; }
    public Vector2Int ToAbsolutePos(PlayerObject? player);
}
