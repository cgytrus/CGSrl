using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using CGSrl.Client.Screens.Templates;

using PER.Abstractions;
using PER.Abstractions.Audio;
using PER.Abstractions.Input;
using PER.Abstractions.Rendering;
using PER.Abstractions.Resources;
using PER.Abstractions.Screens;

using PRR.UI;
using PRR.UI.Resources;

namespace CGSrl.Client.Screens;

public class SettingsScreen : LayoutResource, IScreen, IUpdatable {
    public const string GlobalId = "layouts/settings";

    protected override IRenderer renderer => Core.engine.renderer;
    protected override IInput input => Core.engine.input;
    protected override IAudio audio => Core.engine.audio;

    protected override string layoutName => "settings";

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

    private readonly int[] _fpsValues =
        { -1, 0, 30, 60, 72, 120, 144, 180, 240, 288, 360, 420, 432, 480, 540, 576, 600, 660, 720 };

    private readonly List<ResourcePackData> _availablePacks = new();
    private readonly HashSet<ResourcePackData> _loadedPacks = new();

    public SettingsScreen(Settings settings, IResources resources) {
        _settings = settings;
        resources.TryAddResource(ResourcePackSelectorTemplate.GlobalId,
            new ResourcePackSelectorTemplate(this, _availablePacks, _loadedPacks));
    }

    public override void Preload() {
        base.Preload();
        AddDependency<ResourcePackSelectorTemplate>(ResourcePackSelectorTemplate.GlobalId);

        AddPath("frameLeft.text", $"{layoutsPath}/{layoutName}Left.txt");
        AddPath("frameRight.text", $"{layoutsPath}/{layoutName}Right.txt");

        AddElement<Text>("frameLeft");
        AddElement<Text>("frameRight");
        AddElement<Text>("header.audio");
        AddElement<Text>("volume.master.value");
        AddElement<Slider>("volume.master");
        AddElement<Text>("volume.master.label");
        AddElement<Text>("volume.unfocusedMaster.value");
        AddElement<Slider>("volume.unfocusedMaster");
        AddElement<Text>("volume.unfocusedMaster.label");
        AddElement<Text>("volume.music.value");
        AddElement<Slider>("volume.music");
        AddElement<Text>("volume.music.label");
        AddElement<Text>("volume.sfx.value");
        AddElement<Slider>("volume.sfx");
        AddElement<Text>("volume.sfx.label");
        AddElement<Text>("header.video");
        AddElement<Button>("bloom");
        AddElement<Button>("fullscreen");
        AddElement<Text>("fpsLimit.value");
        AddElement<Slider>("fpsLimit");
        AddElement<Text>("fpsLimit.label");
        AddElement<Text>("header.advanced");
        AddElement<Button>("showFps");
        AddElement<Text>("header.packs");
        AddElement<Text>("pack.description");
        AddElement<ListBox<ResourcePackData>>("packs");
        AddElement<Button>("back");
        AddElement<Button>("apply");
        AddElement<Button>("reload");
    }

    public override void Load(string id) {
        base.Load(id);

        LoadAudio();
        LoadVideo();
        LoadAdvanced();

        GetElement<Button>("back").hotkey = KeyCode.Escape;
        GetElement<Button>("back").onClick += (_, _) => {
            if(reload)
                _reloadScheduled = true;
            if(Core.engine.resources.TryGetResource(MainMenuScreen.GlobalId, out MainMenuScreen? screen))
                Core.engine.screens.SwitchScreen(screen);
        };

        GetElement<Button>("apply").onClick += (_, _) => {
            _reloadScheduled = true;
            if(Core.engine.resources.TryGetResource(GlobalId, out SettingsScreen? screen))
                Core.engine.screens.SwitchScreen(screen);
        };

        GetElement<Button>("reload").onClick += (_, _) => {
            Core.engine.Reload();
            if(Core.engine.resources.TryGetResource(GlobalId, out SettingsScreen? screen))
                Core.engine.screens.SwitchScreen(screen);
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
            int fpsLimitValue = (int)fpsLimit.value;
            if(fpsLimitValue >= 0 && fpsLimitValue < _fpsValues.Length) {
                _settings.fpsLimit = _fpsValues[fpsLimitValue];
                Core.engine.renderer.verticalSync = _settings.fpsLimit < 0;
                Core.engine.updateInterval =
                    _settings.fpsLimit <= 0 ? TimeSpan.Zero : TimeSpan.FromSeconds(1d / _settings.fpsLimit);
            }
            GetElement<Text>("fpsLimit.value").text = fpsLimitValue switch {
                0 => "VSync",
                1 => "Unlimited",
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
        GetElement<Slider>("fpsLimit").value = Array.IndexOf(_fpsValues, _settings.fpsLimit);
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
        // ReSharper disable once ForCanBeConvertedToForeach
        for(int i = 0; i < elementList.Count; i++)
            elementList[i].Update(time);

        if(!_reloadScheduled)
            return;
        _reloadScheduled = false;
        Core.engine.resources.RemoveAllPacks();
        Core.engine.resources.TryAddPacksByNames(_settings.packs);
        Core.engine.SoftReload();
    }
}
