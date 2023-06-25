using PER;
using PER.Common.Resources;

namespace Cgsrl.Server;

public static class Core {
    public static Engine engine { get; } = new(new ResourcesManager(), new Game()) {
        tickInterval = TimeSpan.FromSeconds(0.08d)
    };

    private static void Main() => engine.Reload();
}
