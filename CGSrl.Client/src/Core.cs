using System;

using PER;
using PER.Audio.Raylib;
using PER.Common.Resources;
using PER.Common.Screens;
using PER.Util;

using PRR.OpenGL;
using PRR.UI;

namespace CGSrl.Client;

public static class Core {
    public static readonly string version = Helper.GetVersion();
    public static readonly string engineVersion = Engine.version;
    public static readonly string abstractionsVersion = Engine.abstractionsVersion;
    public static readonly string utilVersion = Helper.version;
    public static readonly string commonVersion = Helper.GetVersion(typeof(ResourcesManager));
    public static readonly string audioVersion = Helper.GetVersion(typeof(AudioManager));
    public static readonly string rendererVersion = Helper.GetVersion(typeof(Renderer));
    public static readonly string uiVersion = Helper.GetVersion(typeof(Button));

    private static readonly Renderer renderer = new();

    public static Engine engine { get; } =
        new(new ResourcesManager(), new ScreenManager(renderer), new Game(), renderer, new InputManager(renderer),
            new AudioManager()) {
            tickInterval = TimeSpan.FromSeconds(-1d) // don't tick
        };

    private static void Main() => engine.Reload();
}
