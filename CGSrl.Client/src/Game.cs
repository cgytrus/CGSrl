﻿using System;

using CGSrl.Client.Resources;
using CGSrl.Client.Screens;

using PER.Abstractions;
using PER.Abstractions.Rendering;
using PER.Abstractions.Resources;
using PER.Common;
using PER.Common.Effects;
using PER.Common.Resources;
using PER.Util;

using PRR.UI.Resources;

namespace CGSrl.Client;

public class Game : IGame, ISetupable, IUpdatable {
    private const string SettingsPath = "config.json";
    private Settings _settings = new();

    private static IRenderer renderer => Core.engine.renderer;

    private static TimeSpan fpsGood => (Core.engine.updateInterval > TimeSpan.Zero ? Core.engine.updateInterval :
        TimeSpan.FromSeconds(1d / 60d)) + TimeSpan.FromSeconds(0.001d);
    private static TimeSpan fpsOk => (Core.engine.updateInterval > TimeSpan.Zero ? Core.engine.updateInterval * 2 :
        TimeSpan.FromSeconds(1d / 60d) * 2) + TimeSpan.FromSeconds(0.001d);

    private Color _fpsGoodColor;
    private Color _fpsOkColor;
    private Color _fpsBadColor;

    private FrameTimeDisplay? _frameTimeDisplay;

    public void Unload() => _settings.Save(SettingsPath);

    public void Load() {
        IResources resources = Core.engine.resources;

        _settings = Settings.Load(SettingsPath);

        resources.TryAddPacksByNames(_settings.packs);

        resources.TryAddResource(AudioResources.GlobalId, new AudioResources());
        resources.TryAddResource(FontResource.GlobalId, new FontResource());
        resources.TryAddResource(ColorsResource.GlobalId, new ColorsResource());

        renderer.formattingEffects.Clear();
        renderer.formattingEffects.Add("none", null);
        renderer.formattingEffects.Add("glitch", new GlitchEffect(renderer));

        resources.TryAddResource(DialogBoxPaletteResource.GlobalId, new DialogBoxPaletteResource());

        resources.TryAddResource(MainMenuScreen.GlobalId, new MainMenuScreen(_settings));
        resources.TryAddResource(SettingsScreen.GlobalId, new SettingsScreen(_settings, resources));
        resources.TryAddResource(ConnectingScreen.GlobalId, new ConnectingScreen(resources));
        resources.TryAddResource(SandboxGameScreen.GlobalId, new SandboxGameScreen(resources));

        resources.TryAddResource(ConnectionErrorDialogBoxScreen.GlobalId, new ConnectionErrorDialogBoxScreen());
    }

    public void Loaded() {
        if(!Core.engine.resources.TryGetResource(FontResource.GlobalId, out FontResource? font) || font.font is null)
            throw new InvalidOperationException("Missing font.");
        Core.engine.resources.TryGetResource(IconResource.GlobalId, out IconResource? icon);

        if(!Core.engine.resources.TryGetResource(ColorsResource.GlobalId, out ColorsResource? colors) ||
            !colors.colors.TryGetValue("background", out Color backgroundColor))
            throw new InvalidOperationException("Missing colors or background color.");
        renderer.background = backgroundColor;
        if(!colors.colors.TryGetValue("fps_good", out _fpsGoodColor))
            _fpsGoodColor = Color.white;
        if(!colors.colors.TryGetValue("fps_ok", out _fpsOkColor))
            _fpsOkColor = Color.white;
        if(!colors.colors.TryGetValue("fps_bad", out _fpsBadColor))
            _fpsBadColor = Color.white;

        _settings.ApplyVolumes();

        Core.engine.updateInterval =
            _settings.fpsLimit <= 0 ? TimeSpan.Zero : TimeSpan.FromSeconds(1d / _settings.fpsLimit);
        Core.engine.rendererSettings = new RendererSettings {
            fullscreen = _settings.fullscreen,
            font = font.font,
            icon = icon?.icon
        };
        Core.engine.renderer.verticalSync = _settings.fpsLimit < 0;
    }

    public void Setup() {
        _frameTimeDisplay = new FrameTimeDisplay(Core.engine.frameTime, renderer, FrameTimeFormatter);
        _settings.ApplyVolumes(); // apply volumes again because the first time the window isn't created yet
        Core.engine.renderer.focusChanged += (_, _) => _settings.ApplyVolumes();
        if(!Core.engine.resources.TryGetResource(MainMenuScreen.GlobalId, out MainMenuScreen? screen))
            return;
        Core.engine.screens.SwitchScreen(screen);
    }

    public void Update(TimeSpan time) {
        if(_settings.showFps)
            _frameTimeDisplay?.Update(time);
    }

    public void Finish() { }

    private Formatting FrameTimeFormatter(FrameTime frameTime, char flag) => flag switch {
        '1' or 'a' => new Formatting(frameTime.frameTime > fpsOk ? _fpsBadColor :
            frameTime.frameTime > fpsGood ? _fpsOkColor : _fpsGoodColor, Color.transparent),
        '2' or 'b' => new Formatting(frameTime.averageFrameTime > fpsOk ? _fpsBadColor :
            frameTime.averageFrameTime > fpsGood ? _fpsOkColor : _fpsGoodColor, Color.transparent),
        _ => new Formatting(Color.white, Color.transparent)
    };
}
