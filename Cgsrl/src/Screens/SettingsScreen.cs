﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Cgsrl.Screens.Templates;

using PER.Abstractions;
using PER.Abstractions.Audio;
using PER.Abstractions.Input;
using PER.Abstractions.Rendering;
using PER.Abstractions.Resources;
using PER.Abstractions.UI;

using PRR.UI;
using PRR.UI.Resources;

namespace Cgsrl.Screens;

public class SettingsScreen : LayoutResource, IScreen {
    public const string GlobalId = "layouts/settings";

    protected override IRenderer renderer => Core.engine.renderer;
    protected override IInput input => Core.engine.input;
    protected override IAudio audio => Core.engine.audio;

    protected override string layoutName => "settings";
    protected override IReadOnlyDictionary<string, Type> elementTypes { get; } = new Dictionary<string, Type> {
        { "frameLeft", typeof(LayoutResourceText) },
        { "frameRight", typeof(LayoutResourceText) },
        { "header.audio", typeof(LayoutResourceText) },
        { "volume.master.value", typeof(LayoutResourceText) },
        { "volume.master", typeof(LayoutResourceSlider) },
        { "volume.master.label", typeof(LayoutResourceText) },
        { "volume.unfocusedMaster.value", typeof(LayoutResourceText) },
        { "volume.unfocusedMaster", typeof(LayoutResourceSlider) },
        { "volume.unfocusedMaster.label", typeof(LayoutResourceText) },
        { "volume.music.value", typeof(LayoutResourceText) },
        { "volume.music", typeof(LayoutResourceSlider) },
        { "volume.music.label", typeof(LayoutResourceText) },
        { "volume.sfx.value", typeof(LayoutResourceText) },
        { "volume.sfx", typeof(LayoutResourceSlider) },
        { "volume.sfx.label", typeof(LayoutResourceText) },
        { "header.video", typeof(LayoutResourceText) },
        { "bloom", typeof(LayoutResourceButton) },
        { "fullscreen", typeof(LayoutResourceButton) },
        { "fpsLimit.value", typeof(LayoutResourceText) },
        { "fpsLimit", typeof(LayoutResourceSlider) },
        { "fpsLimit.label", typeof(LayoutResourceText) },
        { "header.advanced", typeof(LayoutResourceText) },
        { "showFps", typeof(LayoutResourceButton) },
        { "header.packs", typeof(LayoutResourceText) },
        { "pack.description", typeof(LayoutResourceText) },
        { "packs", typeof(LayoutResourceListBox<ResourcePackData>) },
        { "back", typeof(LayoutResourceButton) },
        { "apply", typeof(LayoutResourceButton) },
        { "reload", typeof(LayoutResourceButton) }
    };

    protected override IEnumerable<KeyValuePair<string, Type>> dependencyTypes {
        get {
            foreach(KeyValuePair<string, Type> pair in base.dependencyTypes)
                yield return pair;
            yield return new KeyValuePair<string, Type>(ResourcePackSelectorTemplate.GlobalId,
                typeof(ResourcePackSelectorTemplate));
        }
    }

    protected override IEnumerable<KeyValuePair<string, string>> paths {
        get {
            foreach(KeyValuePair<string, string> pair in base.paths)
                yield return pair;
            yield return new KeyValuePair<string, string>("frameLeft.text", $"{layoutsPath}/{layoutName}Left.txt");
            yield return new KeyValuePair<string, string>("frameRight.text", $"{layoutsPath}/{layoutName}Right.txt");
        }
    }

    private bool reload {
        get => _reload;
        set {
            _reload = value;
            if(TryGetElement("apply", out Button? apply))
                apply.active = value;
        }
    }

    private bool _reload;
    private bool _reloadScheduled;

    private readonly Settings _settings;

    private readonly List<ResourcePackData> _availablePacks = new();
    private readonly HashSet<ResourcePackData> _loadedPacks = new();

    public SettingsScreen(Settings settings, IResources resources) {
        _settings = settings;
        resources.TryAddResource(ResourcePackSelectorTemplate.GlobalId,
            new ResourcePackSelectorTemplate(this, _availablePacks, _loadedPacks));
    }

    public override void Load(string id) {
        base.Load(id);

        LoadAudio();
        LoadVideo();
        LoadAdvanced();

        GetElement<Button>("back").onClick += (_, _) => {
            if(reload)
                _reloadScheduled = true;
            if(Core.engine.resources.TryGetResource(MainMenuScreen.GlobalId, out MainMenuScreen? screen))
                Core.engine.game.SwitchScreen(screen);
        };

        GetElement<Button>("apply").onClick += (_, _) => {
            _reloadScheduled = true;
            if(Core.engine.resources.TryGetResource(GlobalId, out SettingsScreen? screen))
                Core.engine.game.SwitchScreen(screen);
        };

        GetElement<Button>("reload").onClick += (_, _) => {
            Core.engine.Reload();
            if(Core.engine.resources.TryGetResource(GlobalId, out SettingsScreen? screen))
                Core.engine.game.SwitchScreen(screen);
        };
    }

    // ReSharper disable once CognitiveComplexity
    private void LoadAudio() {
        CultureInfo culture = CultureInfo.InvariantCulture;

        Slider volumeMaster = GetElement<Slider>("volume.master");
        volumeMaster.onValueChanged += (_, _) => {
            _settings.masterVolume = volumeMaster.value;
            GetElement<Text>("volume.master.value").text = _settings.masterVolume.ToString(culture);
            _settings.ApplyVolumes();
        };

        Slider volumeUnfocusedMaster = GetElement<Slider>("volume.unfocusedMaster");
        volumeUnfocusedMaster.onValueChanged += (_, _) => {
            _settings.unfocusedMasterVolume = volumeUnfocusedMaster.value;
            GetElement<Text>("volume.unfocusedMaster.value").text =
                _settings.unfocusedMasterVolume.ToString(culture);
            _settings.ApplyVolumes();
        };

        Slider volumeMusic = GetElement<Slider>("volume.music");
        volumeMusic.onValueChanged += (_, _) => {
            _settings.musicVolume = volumeMusic.value;
            GetElement<Text>("volume.music.value").text = _settings.musicVolume.ToString(culture);
            _settings.ApplyVolumes();
        };

        Slider volumeSfx = GetElement<Slider>("volume.sfx");
        volumeSfx.onValueChanged += (_, _) => {
            _settings.sfxVolume = volumeSfx.value;
            GetElement<Text>("volume.sfx.value").text = _settings.sfxVolume.ToString(culture);
            _settings.ApplyVolumes();
        };
    }

    private void LoadVideo() {
        CultureInfo culture = CultureInfo.InvariantCulture;

        Button bloom = GetElement<Button>("bloom");
        bloom.onClick += (_, _) => {
            _settings.bloom = !_settings.bloom;
            bloom.toggled = _settings.bloom;
        };

        Button fullscreen = GetElement<Button>("fullscreen");
        fullscreen.onClick += (_, _) => {
            _settings.fullscreen = !_settings.fullscreen;
            fullscreen.toggled = _settings.fullscreen;
            reload = true;
        };

        Slider fpsLimit = GetElement<Slider>("fpsLimit");
        fpsLimit.onValueChanged += (_, _) => {
            _settings.fpsLimit = (int)fpsLimit.value * 60;
            Core.engine.renderer.framerate = _settings.fpsLimit;
            GetElement<Text>("fpsLimit.value").text = (int)fpsLimit.value switch {
                (int)ReservedFramerates.Vsync => "VSync",
                (int)ReservedFramerates.Unlimited => "Unlimited",
                _ => _settings.fpsLimit.ToString(culture)
            };
        };
    }

    private void LoadAdvanced() {
        Button showFps = GetElement<Button>("showFps");
        showFps.onClick += (_, _) => {
            _settings.showFps = !_settings.showFps;
            showFps.toggled = _settings.showFps;
        };
    }

    public void Open() {
        GetElement<Slider>("volume.master").value = _settings.masterVolume;
        GetElement<Slider>("volume.unfocusedMaster").value = _settings.unfocusedMasterVolume;
        GetElement<Slider>("volume.music").value = _settings.musicVolume;
        GetElement<Slider>("volume.sfx").value = _settings.sfxVolume;
        GetElement<Button>("bloom").toggled = _settings.bloom;
        GetElement<Button>("fullscreen").toggled = _settings.fullscreen;
        GetElement<Slider>("fpsLimit").value = _settings.fpsLimit / 60f;
        GetElement<Button>("showFps").toggled = _settings.showFps;

        GetElement<Text>("pack.description").text = "";
        OpenPacks();

        reload = false;
    }

    private void OpenPacks() {
        _loadedPacks.Clear();
        _availablePacks.Clear();

        foreach(ResourcePackData data in Core.engine.resources.loadedPacks)
            _loadedPacks.Add(data);

        _availablePacks.AddRange(_loadedPacks);
        _availablePacks.AddRange(Core.engine.resources.GetUnloadedAvailablePacks().Reverse());

        GeneratePacksList();
    }

    public void UpdatePacks() {
        _settings.packs = _availablePacks.Where(_loadedPacks.Contains).Select(packData => packData.name).ToArray();
        reload = true;
        GeneratePacksList();
    }

    public void UpdatePackDescription(string text) => GetElement<Text>("pack.description").text = text;

    private void GeneratePacksList() {
        ListBox<ResourcePackData> packs = GetElement<ListBox<ResourcePackData>>("packs");
        packs.Clear();
        foreach(ResourcePackData item in _availablePacks)
            packs.Add(item);
    }

    public void Close() => reload = false;

    public void Update(TimeSpan time) {
        foreach((string _, Element element) in elements)
            element.Update(time);

        if(!_reloadScheduled)
            return;
        _reloadScheduled = false;
        Core.engine.resources.RemoveAllPacks();
        Core.engine.resources.TryAddPacksByNames(_settings.packs);
        Core.engine.SoftReload();
    }

    public void Tick(TimeSpan time) { }
}