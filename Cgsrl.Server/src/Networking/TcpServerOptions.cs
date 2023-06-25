using LiteNetwork.Server;

using PER.Abstractions.Environment;

namespace Cgsrl.Server.Networking;

public class TcpServerOptions : LiteServerOptions {
    public Level level { get; }
    public TcpServerOptions(Level level) => this.level = level;
}
