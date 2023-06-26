using Cgsrl.Shared.Environment;
using Cgsrl.Shared.Networking.Packets.ServerToClient;

using LiteNetwork;
using LiteNetwork.Server;

using PER.Abstractions.Environment;

namespace Cgsrl.Server.Networking;

public class TcpServer : LiteServer<TcpUser> {
    private readonly Level _level;
    private readonly List<LiteConnection> _usersToDisconnect = new();

    public TcpServer(TcpServerOptions options) : base(options) {
        _level = options.level;
        _level.objectAdded += obj => {
                   // prevent from sending own player twice
            SendTo(Users.Where(user => obj is not PlayerObject player || player.connection != user),
                new ObjectAddedPacket(obj).Serialize());
        };
        _level.objectRemoved += obj => {
            SendToAll(new ObjectRemovedPacket(obj.id).Serialize());
        };

        MemoryStream stream = new();
        BinaryWriter writer = new(stream);
        _level.objectChanged += obj => {
            stream.SetLength(0);
            obj.CustomSerialize(writer);
            SendToAll(new ObjectChangedPacket(obj.id, obj.layer, obj.position, stream.ToArray()).Serialize());
        };
    }

    public void ProcessPackets(Level level) {
        foreach(LiteConnection connection in Users) {
            if(connection is not TcpUser user)
                continue;
            if(!user.ProcessPackets(level))
                _usersToDisconnect.Add(user);
        }
        foreach(LiteConnection user in _usersToDisconnect)
            DisconnectUser(user);
        _usersToDisconnect.Clear();
    }
}
