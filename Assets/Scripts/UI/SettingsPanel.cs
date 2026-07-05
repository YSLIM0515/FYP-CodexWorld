// Codex World — Settings panel (scene-authored)
// A reusable settings overlay used by both the main menu and the gameplay scene.
// References the UI controls (built by SettingsPanelBuilder) and drives them:
//   - volume sliders preview live; other controls edit a working copy
//   - SAVE   writes the values via GameSettings and closes
//   - CANCEL discards edits (reloads saved values) and closes
//   - RESTORE DEFAULTS resets the controls to defaults (persisted on next SAVE)
// Open it via Open() (wired from a Settings/gear button); it starts hidden.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingsPanel : MonoBehaviour
{
    [Header("Root (the object shown/hidden)")]
    [Tooltip("Object toggled on/off when opening/closing. Defaults to this GameObject.")]
    public GameObject panelRoot;

    [Header("Open trigger (optional)")]
    [Tooltip("Button that opens this panel (e.g. an in-game gear button).")]
    public Button openButton;

    [Header("Audio")]
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;
    public Text masterValue;
    public Text musicValue;
    public Text sfxValue;

    [Header("Display")]
    public Dropdown resolutionDropdown;
    public Toggle fullscreenToggle;

    [Header("Language")]
    public Dropdown languageDropdown;

    [Header("Accessibility")]
    public Toggle colorBlindToggle;

    [Header("Buttons")]
    public Button saveButton;
    public Button cancelButton;
    public Button restoreButton;

    readonly List<Vector2Int> _resolutions = new List<Vector2Int>();

    void Awake()
    {
        GameSettings.Load();
        BuildResolutionOptions();

        // Buttons are wired through their OnClick() lists in the Inspector
        // (SettingsPanelBuilder sets them up) — no script wiring here.
        // Sliders stay code-wired: their % labels / live preview need code.
        if (masterSlider) masterSlider.onValueChanged.AddListener(v => { SetLabel(masterValue, v); AudioListener.volume = v; });
        if (musicSlider) musicSlider.onValueChanged.AddListener(v => SetLabel(musicValue, v));
        if (sfxSlider) sfxSlider.onValueChanged.AddListener(v => SetLabel(sfxValue, v));

        Hide();
    }

    // ---------------- Open / close ----------------

    public void Open()
    {
        GameSettings.Load();     // start from the saved values
        PullFromSettings();
        Root().SetActive(true);
        transform.SetAsLastSibling();
    }

    // Cancel: throw away edits and restore the saved audio preview.
    public void Close()
    {
        GameSettings.Load();
        Hide();
    }

    // SAVE button (OnClick): persist the current controls and close.
    public void Save()
    {
        PushToSettings();
        GameSettings.Save();
        Hide();
    }

    // RESTORE DEFAULTS button (OnClick): reset controls; saved on next Save().
    public void RestoreDefaults()
    {
        GameSettings.RestoreDefaults();
        PullFromSettings();      // slider callbacks preview the default volumes
    }

    void Hide() => Root().SetActive(false);

    GameObject Root() => panelRoot != null ? panelRoot : gameObject;

    // ---------------- Sync helpers ----------------

    void PullFromSettings()
    {
        if (masterSlider) masterSlider.value = GameSettings.Master;
        if (musicSlider) musicSlider.value = GameSettings.Music;
        if (sfxSlider) sfxSlider.value = GameSettings.Sfx;
        if (fullscreenToggle) fullscreenToggle.isOn = GameSettings.Fullscreen;
        if (colorBlindToggle) colorBlindToggle.isOn = GameSettings.ColorBlind;
        if (languageDropdown && languageDropdown.options.Count > 0)
            languageDropdown.value = Mathf.Clamp(GameSettings.Language, 0, languageDropdown.options.Count - 1);
        if (resolutionDropdown && _resolutions.Count > 0)
            resolutionDropdown.value = CurrentResolutionIndex();

        SetLabel(masterValue, GameSettings.Master);
        SetLabel(musicValue, GameSettings.Music);
        SetLabel(sfxValue, GameSettings.Sfx);
    }

    void PushToSettings()
    {
        if (masterSlider) GameSettings.Master = masterSlider.value;
        if (musicSlider) GameSettings.Music = musicSlider.value;
        if (sfxSlider) GameSettings.Sfx = sfxSlider.value;
        if (fullscreenToggle) GameSettings.Fullscreen = fullscreenToggle.isOn;
        if (colorBlindToggle) GameSettings.ColorBlind = colorBlindToggle.isOn;
        if (languageDropdown) GameSettings.Language = languageDropdown.value;
        if (resolutionDropdown && _resolutions.Count > 0)
        {
            var r = _resolutions[Mathf.Clamp(resolutionDropdown.value, 0, _resolutions.Count - 1)];
            GameSettings.ResWidth = r.x;
            GameSettings.ResHeight = r.y;
        }
    }

    void BuildResolutionOptions()
    {
        if (resolutionDropdown == null) return;
        _resolutions.Clear();
        var seen = new HashSet<string>();
        foreach (var r in Screen.resolutions)
        {
            string key = $"{r.width}x{r.height}";
            if (seen.Add(key)) _resolutions.Add(new Vector2Int(r.width, r.height));
        }
        if (_resolutions.Count == 0) _resolutions.Add(new Vector2Int(1920, 1080));

        var options = new List<string>();
        foreach (var r in _resolutions) options.Add($"{r.x} x {r.y}");
        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(options);
    }

    int CurrentResolutionIndex()
    {
        for (int i = 0; i < _resolutions.Count; i++)
            if (_resolutions[i].x == GameSettings.ResWidth && _resolutions[i].y == GameSettings.ResHeight)
                return i;
        return _resolutions.Count - 1;
    }

    static void SetLabel(Text label, float value01)
    {
        if (label) label.text = Mathf.RoundToInt(value01 * 100f) + "%";
    }
}
