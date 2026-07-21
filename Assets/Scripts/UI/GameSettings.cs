// Codex World — Settings store (PlayerPrefs-backed)
// Holds the game's user settings and persists them, the same lightweight way
// ProgressManager stores XP. The SettingsPanel UI reads/writes these values;
// audio and display systems can read them later. Keys:
//   cw_set_master / music / sfx  (float 0..1) volume levels
//   cw_set_resw / resh           (int)  chosen screen resolution
//   cw_set_full                  (0/1)  fullscreen
//   cw_set_lang                  (int)  language option index
//   cw_set_cb                    (0/1)  colour-blind mode

using UnityEngine;

public static class GameSettings
{
    // Defaults match the mock-up (75% / 50% / 90%).
    public const float DefMaster = 0.75f, DefMusic = 0.5f, DefSfx = 0.9f;
    public const bool DefFullscreen = true, DefColorBlind = false;
    public const int DefLanguage = 0;

    const string KMaster = "cw_set_master", KMusic = "cw_set_music", KSfx = "cw_set_sfx",
                 KResW = "cw_set_resw", KResH = "cw_set_resh", KFull = "cw_set_full",
                 KLang = "cw_set_lang", KCb = "cw_set_cb";

    public static float Master, Music, Sfx;
    public static int ResWidth, ResHeight, Language;
    public static bool Fullscreen, ColorBlind;

    // Reads saved values (or defaults) into memory and applies them.
    public static void Load()
    {
        Master = PlayerPrefs.GetFloat(KMaster, DefMaster);
        Music = PlayerPrefs.GetFloat(KMusic, DefMusic);
        Sfx = PlayerPrefs.GetFloat(KSfx, DefSfx);
        ResWidth = PlayerPrefs.GetInt(KResW, Screen.currentResolution.width);
        ResHeight = PlayerPrefs.GetInt(KResH, Screen.currentResolution.height);
        Fullscreen = PlayerPrefs.GetInt(KFull, DefFullscreen ? 1 : 0) == 1;
        Language = PlayerPrefs.GetInt(KLang, DefLanguage);
        ColorBlind = PlayerPrefs.GetInt(KCb, DefColorBlind ? 1 : 0) == 1;
        ApplyAudio();
    }

    // Writes the current in-memory values and applies them.
    public static void Save()
    {
        PlayerPrefs.SetFloat(KMaster, Master);
        PlayerPrefs.SetFloat(KMusic, Music);
        PlayerPrefs.SetFloat(KSfx, Sfx);
        PlayerPrefs.SetInt(KResW, ResWidth);
        PlayerPrefs.SetInt(KResH, ResHeight);
        PlayerPrefs.SetInt(KFull, Fullscreen ? 1 : 0);
        PlayerPrefs.SetInt(KLang, Language);
        PlayerPrefs.SetInt(KCb, ColorBlind ? 1 : 0);
        PlayerPrefs.Save();
        ApplyAudio();
        ApplyDisplay();
    }

    // Resets in-memory values to defaults (not saved until Save()).
    public static void RestoreDefaults()
    {
        Master = DefMaster;
        Music = DefMusic;
        Sfx = DefSfx;
        ResWidth = Screen.currentResolution.width;
        ResHeight = Screen.currentResolution.height;
        Fullscreen = DefFullscreen;
        Language = DefLanguage;
        ColorBlind = DefColorBlind;
    }

    static UnityEngine.Audio.AudioMixer _mixer;
    static UnityEngine.Audio.AudioMixer Mixer =>
        _mixer != null ? _mixer : _mixer = Resources.Load<UnityEngine.Audio.AudioMixer>("GameAudioMixer");

    public static void ApplyAudio()
    {
        AudioListener.volume = Master;
        SetMixerVolume("MusicVol", Music);
        SetMixerVolume("SFXVol", Sfx);
    }

    // Sliders are linear 0..1; mixer attenuation is dB (-80 dB = silence).
    public static void SetMixerVolume(string param, float linear)
    {
        if (Mixer != null)
            Mixer.SetFloat(param, linear > 0.0001f ? Mathf.Log10(linear) * 20f : -80f);
    }

    public static void ApplyDisplay()
    {
        if (ResWidth > 0 && ResHeight > 0)
            Screen.SetResolution(ResWidth, ResHeight, Fullscreen);
        else
            Screen.fullScreen = Fullscreen;
    }
}
