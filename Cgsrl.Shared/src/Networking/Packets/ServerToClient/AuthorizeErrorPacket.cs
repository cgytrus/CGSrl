namespace Cgsrl.Shared.Networking.Packets.ServerToClient;

public record AuthorizeErrorPacket(string error) : Packet {
    public const string GlobalId = "authorizeError";

    public byte[] Serialize() {
        MemoryStream stream = new();
        BinaryWriter writer = new(stream);
        writer.Write(GlobalId);
        writer.Write(error);
        return stream.ToArray();
    }

    public static AuthorizeErrorPacket Deserialize(BinaryReader reader) => new(reader.ReadString());
}
