using System.Diagnostics.CodeAnalysis;

using Cgsrl.Shared.Environment;
using Cgsrl.Shared.Networking.Packets.ServerToClient;

using LiteNetwork;

using PER.Abstractions.Environment;

namespace Cgsrl.Shared.Networking.Packets.ClientToServer;

public record AuthorizePacket(string username, string displayName) : Packet {
    public const string GlobalId = "authorize";

    public enum AuthError { None, EmptyUsername, EmptyDisplayName, InvalidUsername, DuplicateUsername }

    public AuthError Process(LiteConnection connection, Level level, out PlayerObject player) {
        player = new PlayerObject { connection = connection, username = username, displayName = displayName };
        if(string.IsNullOrEmpty(username)) {
            connection.Send(new AuthorizeErrorPacket("empty username").Serialize());
            return AuthError.EmptyUsername;
        }
        if(string.IsNullOrEmpty(displayName)) {
            connection.Send(new AuthorizeErrorPacket("empty display name").Serialize());
            return AuthError.EmptyDisplayName;
        }
        if(username.Any(c => !char.IsAsciiLetterLower(c) && !char.IsAsciiDigit(c) && c != '_' && c != '-')) {
            connection.Send(
                new AuthorizeErrorPacket("invalid username: only lowercase letters, digits, _ and - are allowed")
                    .Serialize());
            return AuthError.InvalidUsername;
        }
        if(level.objects.Values.OfType<PlayerObject>().Any(x => x.username == username)) {
            connection.Send(new AuthorizeErrorPacket("player with this username is already connected").Serialize());
            return AuthError.DuplicateUsername;
        }
        level.Add(player);
        connection.Send(new JoinedPacket(player.id, level.objects.Count, level.objects.Values).Serialize());
        return AuthError.None;
    }

    public byte[] Serialize() {
        MemoryStream stream = new();
        BinaryWriter writer = new(stream);
        writer.Write(GlobalId);
        writer.Write(username);
        writer.Write(displayName);
        return stream.ToArray();
    }

    public static AuthorizePacket Deserialize(BinaryReader reader) => new(reader.ReadString(), reader.ReadString());
}
