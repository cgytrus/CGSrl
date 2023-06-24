﻿using System;
using System.Collections.Generic;

using PER.Abstractions;
using PER.Abstractions.Audio;
using PER.Abstractions.Input;
using PER.Abstractions.Rendering;
using PER.Abstractions.UI;
using PER.Util;

using PRR.UI;
using PRR.UI.Resources;

namespace Cgsrl.Screens;

public class MainMenuScreen : LayoutResource, IScreen {
    public const string GlobalId = "layouts/mainMenu";

    protected override IRenderer renderer => Core.engine.renderer;
    protected override IInput input => Core.engine.input;
    protected override IAudio audio => Core.engine.audio;

    protected override string layoutName => "mainMenu";
    protected override IReadOnlyDictionary<string, Type> elementTypes { get; } = new Dictionary<string, Type> {
        { "frameLeft", typeof(LayoutResourceText) },
        { "frameRight", typeof(LayoutResourceText) },
        { "title", typeof(LayoutResourceText) },
        { "play", typeof(LayoutResourceButton) },
        { "settings", typeof(LayoutResourceButton) },
        { "exit", typeof(LayoutResourceButton) },
        { "sfml", typeof(LayoutResourceButton) },
        { "github", typeof(LayoutResourceButton) },
        { "discord", typeof(LayoutResourceButton) },
        { "versions", typeof(LayoutResourceText) }
    };

    protected override IEnumerable<KeyValuePair<string, string>> paths {
        get {
            foreach(KeyValuePair<string, string> pair in base.paths)
                yield return pair;
            yield return new KeyValuePair<string, string>("frameLeft.text", $"{layoutsPath}/{layoutName}Left.txt");
            yield return new KeyValuePair<string, string>("frameRight.text", $"{layoutsPath}/{layoutName}Right.txt");
        }
    }

    public override void Load(string id) {
        base.Load(id);

        GetElement<Button>("play").onClick += (_, _) => {
            if(Core.engine.resources.TryGetResource(GameScreen.GlobalId, out GameScreen? screen))
                Core.engine.game.SwitchScreen(screen);
        };

        GetElement<Button>("settings").onClick += (_, _) => {
            if(Core.engine.resources.TryGetResource(SettingsScreen.GlobalId, out SettingsScreen? screen))
                Core.engine.game.SwitchScreen(screen);
        };

        GetElement<Button>("exit").onClick += (_, _) => {
            Core.engine.game.SwitchScreen(null);
        };

        GetElement<Button>("sfml").onClick += (_, _) => {
            Helper.OpenUrl("https://sfml-dev.org");
        };

        GetElement<Button>("github").onClick += (_, _) => {
            Helper.OpenUrl("https://github.com/cgytrus/Cgsrl");
        };

        GetElement<Button>("discord").onClick += (_, _) => {
            Helper.OpenUrl("https://discord.gg/AuYUVs5");
        };

        Text versions = GetElement<Text>("versions");
        versions.text =
            string.Format(versions.text ?? string.Empty, Core.version, Core.engineVersion, Core.abstractionsVersion,
                Core.utilVersion, Core.commonVersion, Core.audioVersion, Core.rendererVersion, Core.uiVersion);
    }

    public void Open() { }
    public void Close() { }

    public void Update(TimeSpan time) {
        foreach((string _, Element element) in elements)
            element.Update(time);
    }

    public void Tick(TimeSpan time) { }
}