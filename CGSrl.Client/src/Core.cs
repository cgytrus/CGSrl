using System;

using PER;
using PER.Util;

using PRR.OpenGL;
using PRR.UI;

namespace CGSrl.Client;

public static class Core {
    public static readonly string version = Helper.GetVersion();
    public static readonly string engineVersion = Engine.version;
    public static readonly string abstractionsVersion = Engine.abstractionsVersion;
    public static readonly string utilVersion = Helper.version;
    public static readonly string commonVersion = Helper.GetVersion(typeof(PER.Common.Resources.Resources));
    public static readonly string audioVersion = Helper.GetVersion(typeof(PER.Audio.Raylib.Audio));
    public static readonly string rendererVersion = Helper.GetVersion(typeof(Renderer));
    public static readonly string uiVersion = Helper.GetVersion(typeof(Button));

    private static readonly Renderer renderer = new("CGSrl", new Vector2Int(128, 72));

    public static Engine engine { get; } =
        new(new PER.Common.Resources.Resources(), new PER.Common.Screens.Screens(renderer), new Game(), renderer,
            new Input(renderer), new PER.Audio.Raylib.Audio()) {
            tickInterval = TimeSpan.FromSeconds(-1d) // don't tick
        };

    private static void Main() => engine.Run();
}
