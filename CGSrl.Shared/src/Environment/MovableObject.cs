using System.Numerics;

using CGSrl.Shared.Networking;

using Lidgren.Network;

using PER.Abstractions;
using PER.Abstractions.Environment;
using PER.Util;

namespace CGSrl.Shared.Environment;

public abstract class MovableObject : SyncedLevelObject, IAddable, ITickable {
    protected abstract bool canPush { get; }
    protected abstract float mass { get; }

    private Vector2 _velocity;
    private Vector2 _subPos;

    public virtual void Added() => level.LoadChunkAt(level.LevelToChunkPosition(position));

    public virtual void Tick(TimeSpan time) {
        bool pushable = true;
        ProcessVelocity(ref pushable);
    }

    public void AddForce(Vector2 velocity) {
        if(!canPush)
            return;
        _velocity += velocity;
    }

    protected void AddMovementForce(Vector2 velocity) {
        velocity *= GetCurrentFriction() * 2f;
        Vector2 newVelocity = new(
            Math.Clamp(_velocity.X + velocity.X, -0.5f, 0.5f),
            Math.Clamp(_velocity.Y + velocity.Y, -0.5f, 0.5f)
        );
        Vector2 needVelocityToReach = new(
            Math.Max(Math.Abs(newVelocity.X - _velocity.X), 0f) * Math.Sign(velocity.X),
            Math.Max(Math.Abs(newVelocity.Y - _velocity.Y), 0f) * Math.Sign(velocity.Y)
        );
        _velocity += needVelocityToReach;
    }

    private void CheckVelocityForNan() {
        if(float.IsNaN(_velocity.X))
            _velocity.X = 0f;
        if(float.IsNaN(_velocity.Y))
            _velocity.Y = 0f;
    }

    private bool ProcessVelocity(ref bool pushable) {
        CheckVelocityForNan();
        Vector2 velocity = new(Math.Clamp(_velocity.X, -1f, 1f), Math.Clamp(_velocity.Y, -1f, 1f));
        _subPos += velocity;
        Vector2Int move = new();
        switch(_subPos.X) {
            case >= 1f:
                _subPos.X = 1f;
                move += new Vector2Int(1, 0);
                break;
            case <= -1f:
                _subPos.X = -1f;
                move -= new Vector2Int(1, 0);
                break;
        }
        switch(_subPos.Y) {
            case >= 1f:
                _subPos.Y = 1f;
                move += new Vector2Int(0, 1);
                break;
            case <= -1f:
                _subPos.Y = -1f;
                move -= new Vector2Int(0, 1);
                break;
        }

        if(move is { x: 0, y: 0 }) {
            ProcessFriction();
            return false;
        }
        bool moved = TryMove(move, ref pushable);
        if(moved)
            _subPos -= new Vector2(move.x, move.y);
        else if(!pushable) {
            if(move.x != 0)
                _velocity.X = 0f;
            if(move.y != 0)
                _velocity.Y = 0f;
        }
        ProcessFriction();
        return moved;
    }

    private void ProcessFriction() {
        CheckVelocityForNan();

        if(_velocity.X == 0f)
            _subPos.X = 0f;
        if(_velocity.Y == 0f)
            _subPos.Y = 0f;

        Vector2 friction = GetCurrentFriction();
        friction.X = Math.Clamp(friction.X, 0f, Math.Abs(_velocity.X)) * Math.Sign(_velocity.X);
        friction.Y = Math.Clamp(friction.Y, 0f, Math.Abs(_velocity.Y)) * Math.Sign(_velocity.Y);
        _velocity -= friction;
    }

    private Vector2 GetCurrentFriction() =>
        level.HasObjectAt<IceObject>(position) ? new Vector2(1f / 10f, 1f / 10f) : new Vector2(1f, 1f);

    private bool TryPush(Vector2 velocity, float otherMass, ref bool pushable) {
        if(!canPush) {
            pushable = false;
            return false;
        }
        _velocity += new Vector2(Math.Clamp(velocity.X * otherMass / mass, -1f, 1f),
            Math.Clamp(velocity.Y * otherMass / mass, -1f, 1f));
        CheckVelocityForNan();
        level.LoadChunkAt(level.LevelToChunkPosition(position));
        level.LoadChunkAt(level.LevelToChunkPosition(new Vector2Int(position.x + Math.Sign(_velocity.X),
            position.y + Math.Sign(_velocity.Y))));
        return ProcessVelocity(ref pushable);
    }

    private bool TryMove(Vector2Int delta, ref bool pushable) {
        if(delta.x != 0 && delta.y != 0)
            return Random.Shared.Next(0, 2) == 0 ?
                TryMoveDiagHorFirst(delta, ref pushable) :
                TryMoveDiagVerFirst(delta, ref pushable);
        if(level.TryGetObjectAt(position + delta, out MovableObject? next) &&
            !next.TryPush(_velocity, mass, ref pushable))
            return false;
        position += delta;
        return true;
    }

    private bool TryMoveDiagHorFirst(Vector2Int delta, ref bool pushable) {
        Vector2Int position = this.position;
        Vector2Int hor = new(delta.x, 0);
        Vector2Int ver = new(0, delta.y);
        Vector2 horV = _velocity with { Y = 0 };
        Vector2 verV = _velocity with { X = 0 };
        bool moved = false;

        if(!level.TryGetObjectAt(position + hor, out MovableObject? nextHor) ||
            nextHor.TryPush(horV, mass, ref pushable)) {
            moved = true;
            this.position += hor;
        }

        bool diag = !moved || !level.TryGetObjectAt(position + delta, out MovableObject? next) ||
            next.TryPush(_velocity, mass, ref pushable);
        if(!diag || level.TryGetObjectAt(position + ver, out MovableObject? nextVer) &&
            !nextVer.TryPush(verV, mass, ref pushable))
            return moved;
        this.position += ver;
        return true;
    }

    private bool TryMoveDiagVerFirst(Vector2Int delta, ref bool pushable) {
        Vector2Int position = this.position;
        Vector2Int hor = new(delta.x, 0);
        Vector2Int ver = new(0, delta.y);
        Vector2 horV = _velocity with { Y = 0 };
        Vector2 verV = _velocity with { X = 0 };
        bool moved = false;

        if(!level.TryGetObjectAt(position + ver, out MovableObject? nextVer) ||
            nextVer.TryPush(verV, mass, ref pushable)) {
            moved = true;
            this.position += ver;
        }

        bool diag = !moved || !level.TryGetObjectAt(position + delta, out MovableObject? next) ||
            next.TryPush(_velocity, mass, ref pushable);
        if(!diag || level.TryGetObjectAt(position + hor, out MovableObject? nextHor) &&
            !nextHor.TryPush(horV, mass, ref pushable))
            return moved;
        this.position += hor;
        return true;
    }

    protected override void WriteStaticDataTo(NetBuffer buffer) {
        base.WriteStaticDataTo(buffer);
        buffer.Write(_velocity);
        buffer.Write(_subPos);
    }

    protected override void ReadStaticDataFrom(NetBuffer buffer) {
        base.ReadStaticDataFrom(buffer);
        _velocity = buffer.ReadVector2();
        _subPos = buffer.ReadVector2();
    }
}
