using System.Numerics;

using Lidgren.Network;

using PER.Util;

namespace Cgsrl.Shared.Networking;

public static class NetBufferExtensions {
    public static void Write(this NetBuffer buffer, Guid id) {
        Span<byte> bytes = stackalloc byte[16];
        id.TryWriteBytes(bytes);
        buffer.Write(bytes);
    }
    public static Guid ReadGuid(this NetBuffer buffer) {
        Span<byte> bytes = stackalloc byte[16];
        buffer.ReadBytes(bytes);
        return new Guid(bytes);
    }

    public static void Write(this NetBuffer buffer, Vector2Int vec) {
        buffer.Write(vec.x);
        buffer.Write(vec.y);
    }
    public static Vector2Int ReadVector2Int(this NetBuffer buffer) => new(buffer.ReadInt32(), buffer.ReadInt32());

    public static void Write(this NetBuffer buffer, Vector2 vec) {
        buffer.Write(vec.X);
        buffer.Write(vec.Y);
    }
    public static Vector2 ReadVector2(this NetBuffer buffer) => new(buffer.ReadFloat(), buffer.ReadFloat());
}
