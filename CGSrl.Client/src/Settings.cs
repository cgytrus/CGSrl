﻿using System.IO;
using System.Text.Json;

using PER.Abstractions.Audio;

namespace CGSrl.Client;

public class Settings {
    public string address { get; set; } = "";
    public string username { get; set; } = "";
    public string displayName { get; set; } = "";
    public string[] packs { get; set; } = { "Default" };
    public float masterVolume { get; set; } = 0.2f;
    public float unfocusedMasterVolume { get; set; } = 0.2f;
    public float musicVolume { get; set; } = 1f;
    public float sfxVolume { get; set; } = 1f;
    public bool bloom { get; set; }
    public bool fullscreen { get; set; }
    public int fpsLimit { get; set; } = -1; // default to VSync
    public bool showFps { get; set; } = true;

    public static Settings Load(string path) {
        if(!File.Exists(path))
            return new Settings();
        FileStream file = File.OpenRead(path);
        Settings settings = JsonSerializer.Deserialize<Settings>(file) ?? new Settings();
        file.Close();
        return settings;
    }

    public void Save(string path) {
        FileStream file = File.Open(path, FileMode.Create);
        JsonSerializer.Serialize(file, this);
        file.Close();
    }

    public void ApplyVolumes() {
        if(Core.engine.audio.TryGetMixer("master", out IAudioMixer? mixer)) {
            mixer.volume = masterVolume;
            if(!Core.engine.renderer.focused)
                mixer.volume *= unfocusedMasterVolume;
        }

        if(Core.engine.audio.TryGetMixer("music", out mixer))
            mixer.volume = musicVolume;

        if(Core.engine.audio.TryGetMixer("sfx", out mixer))
            mixer.volume = sfxVolume;

        Core.engine.audio.UpdateVolumes();
    }
}
