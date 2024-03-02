using PER;
using PER.Common.Resources;
using PER.Util;

namespace CGSrl.Server;

public static class Core {
    public static readonly string version = Helper.GetVersion();

    public static HeadlessEngine engine { get; } = new(new Resources(), new Game()) {
        tickInterval = TimeSpan.FromSeconds(0.05d)
    };

    private static void Main() => engine.Run();
}
