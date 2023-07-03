using PER;
using PER.Common.Resources;
using PER.Util;

namespace CGSrl.Server;

public static class Core {
    public static readonly string version = Helper.GetVersion();

    public static Engine engine { get; } = new(new ResourcesManager(), new Game()) {
        updateInterval = TimeSpan.FromSeconds(0.08d),
        tickInterval = TimeSpan.FromSeconds(0.08d)
    };

    private static void Main() => engine.Reload();
}
