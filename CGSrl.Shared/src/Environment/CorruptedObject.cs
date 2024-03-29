﻿using CGSrl.Shared.Networking;

using PER.Abstractions.Rendering;
using PER.Util;

namespace CGSrl.Shared.Environment;

// use only when something really really really really really really bad happens
public class CorruptedObject : SyncedLevelObject {
    public override int layer => int.MaxValue;
    public override RenderCharacter character { get; } = new('\0', Color.white, Color.transparent);
    public override IEffect? effect => renderer?.formattingEffects["glitch"];
    public override bool blocksLight => false;
}
