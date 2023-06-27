using Cgsrl.Shared.Networking;

using PER.Util;

namespace Cgsrl.Shared.Environment;

public abstract class PushableObject : SyncedLevelObject {
    public bool TryMove(Vector2Int delta) {
        if(level.HasObjectAt<WallObject>(position + delta) ||
            level.HasObjectAt<PlayerObject>(position + delta) ||
            (level.TryGetObjectAt(position + delta, out PushableObject? next) && !next.TryMove(delta)))
            return false;
        position += delta;
        return true;
    }
}
