using PER.Abstractions.Environment;
using PER.Util;

namespace Cgsrl.Shared.Environment;

public abstract class PushableObject : LevelObject {
    public bool TryMove(Vector2Int delta) {
        if(level.HasObjectAt<WallObject>(position + delta) ||
            (level.TryGetObjectAt(position + delta, out PushableObject? next) && !next.TryMove(delta)))
            return false;
        position += delta;
        return true;
    }
}
