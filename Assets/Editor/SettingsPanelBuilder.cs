// Codex World — Settings panel builder (Editor tool)
// Menu: Tools > Codex World > Build Settings Panel
// Generates a reusable SETTINGS overlay as REAL, editable objects under the open
// scene's Canvas: AUDIO volume sliders, DISPLAY (resolution + fullscreen),
// LANGUAGE, ACCESSIBILITY, and SAVE / CANCEL / RESTORE DEFAULTS buttons.
// Run it in the MainMenu scene AND in the CodexWorld scene:
//   - In MainMenu it wires the panel to the SETTINGS button.
//   - In gameplay it adds a small gear button that opens the panel.
// The panel starts hidden and is shown via SettingsPanel.Open(). Safe to re-run.

using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class SettingsPanelBuilder
{
    const float WW = 720f, WH = 620f;      // window size
    const float PadX = 40f;

    static readonly Color Dim        = new Color(0f, 0f, 0f, 0.6f);
    static readonly Color WindowCol  = new Color(0.07f, 0.12f, 0.20f, 0.98f);
    static readonly Color SectionCol = new Color(0.10f, 0.16f, 0.26f, 0.95f);
    static readonly Color ControlCol = new Color(0.05f, 0.09f, 0.15f, 1f);
    static readonly Color Cyan       = new Color(0.45f, 0.85f, 1f, 1f);
    static readonly Color TextLight  = new Color(0.88f, 0.94f, 1f, 1f);
    static readonly Color SaveCol    = new Color(0.20f, 0.55f, 0.45f, 1f);
    static readonly Color NeutralBtn = new Color(0.12f, 0.20f, 0.30f, 1f);

    static DefaultControls.Resources Res => new DefaultControls.Resources();

    [MenuItem("Tools/Codex World/Build Settings Panel")]
    public static void Build()
    {
        var canvas = FindOrCreateCanvas();
        var existing = canvas.transform.Find("SettingsPanel");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);
        var oldGear = canvas.transform.Find("Btn_OpenSettings");
        if (oldGear != null) Object.DestroyImmediate(oldGear.gameObject);

        // Root overlay (full-screen), holds the SettingsPanel component. This
        // GameObject stays ACTIVE so its Awake() wires the buttons; only the
        // Content child below is shown/hidden.
        var rootGO = new GameObject("SettingsPanel");
        rootGO.transform.SetParent(canvas.transform, false);
        Stretch(rootGO.AddComponent<RectTransform>());
        var panel = rootGO.AddComponent<SettingsPanel>();

        // Content container that is toggled on/off when opening/closing.
        var content = new GameObject("Content");
        content.transform.SetParent(rootGO.transform, false);
        Stretch(content.AddComponent<RectTransform>());
        panel.panelRoot = content;

        // Click-blocking dimmer.
        var dim = Panel(content.transform, "Dimmer", Dim);
        Stretch(dim.rectTransform);

        // Centered window.
        var window = Panel(content.transform, "Window", WindowCol);
        Center(window.rectTransform, WW, WH);
        Outline(window.gameObject, Cyan);
        var w = window.transform;

        // Title
        var title = Text(w, "Title", "SETTINGS", 34, TextAnchor.MiddleCenter, Cyan, FontStyle.Bold);
        TopLeft(title.rectTransform, 0, 18, WW, 44);

        float y = 78f;

        // ---- AUDIO ----
        SectionHeader(w, ref y, "♪  AUDIO");
        panel.masterSlider = SliderRow(w, ref y, "Master Volume", out panel.masterValue);
        panel.musicSlider  = SliderRow(w, ref y, "Music Volume",  out panel.musicValue);
        panel.sfxSlider    = SliderRow(w, ref y, "Sound Effects Volume", out panel.sfxValue);
        y += 10;

        // ---- DISPLAY ----
        SectionHeader(w, ref y, "▢  DISPLAY");
        panel.resolutionDropdown = DropdownRow(w, ref y, "Resolution", new[] { "1920 x 1080 (16:9)" });
        panel.fullscreenToggle   = ToggleRow(w, ref y, "Fullscreen", true);
        y += 10;

        // ---- LANGUAGE ----
        SectionHeader(w, ref y, "⌾  LANGUAGE");
        panel.languageDropdown = DropdownRow(w, ref y, "Language", new[] { "English" });
        y += 10;

        // ---- ACCESSIBILITY ----
        SectionHeader(w, ref y, "◉  ACCESSIBILITY");
        panel.colorBlindToggle = ToggleRow(w, ref y, "Colour Blind Mode", false);

        // ---- Buttons: wired through their OnClick() lists (Inspector-visible) ----
        panel.saveButton    = BottomButton(w, -230, "SAVE", SaveCol);
        panel.cancelButton  = BottomButton(w, 0, "CANCEL", NeutralBtn);
        panel.restoreButton = BottomButton(w, 230, "RESTORE DEFAULTS", NeutralBtn);
        UnityEventTools.AddPersistentListener(panel.saveButton.onClick, panel.Save);
        UnityEventTools.AddPersistentListener(panel.cancelButton.onClick, panel.Close);
        UnityEventTools.AddPersistentListener(panel.restoreButton.onClick, panel.RestoreDefaults);

        // ---- Hook up an opener for the current scene ----
        var menu = Object.FindAnyObjectByType<MainMenuController>();
        if (menu != null)
        {
            // The menu's SETTINGS button already targets OnSettings(); it just
            // needs this panel reference to open it.
            menu.settingsPanel = panel;
            EditorUtility.SetDirty(menu);
        }
        else
        {
            panel.openButton = GearButton(canvas.transform);
            UnityEventTools.AddPersistentListener(panel.openButton.onClick, panel.Open);
        }

        EnsureEventSystem();
        // Content stays visible in the editor for easy tweaking; SettingsPanel.Awake()
        // hides it at Play start until Open() is called.

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = rootGO;
        Debug.Log("Codex World: settings panel built under '" + canvas.name + "'. Save the scene (Ctrl+S). " +
                  (menu != null ? "Wired to the main-menu SETTINGS button." : "Added a gear button that opens it."));
    }

    // ---------------- Sections & rows ----------------

    static void SectionHeader(Transform window, ref float y, string label)
    {
        var bar = Panel(window, "Section_" + Sanitize(label), SectionCol);
        TopLeft(bar.rectTransform, PadX, y, WW - PadX * 2, 34);
        var text = Text(bar.transform, "Label", label, 18, TextAnchor.MiddleLeft, Cyan, FontStyle.Bold);
        var rt = text.rectTransform;
        Stretch(rt);
        rt.offsetMin = new Vector2(16, 0);
        rt.offsetMax = new Vector2(-16, 0);
        y += 42;
    }

    static Slider SliderRow(Transform window, ref float y, string label, out Text valueLabel)
    {
        var row = Row(window, y, 38);
        Label(row, label, 250);

        var sliderGO = DefaultControls.CreateSlider(Res);
        sliderGO.name = "Slider";
        sliderGO.transform.SetParent(row, false);
        var rt = (RectTransform)sliderGO.transform;
        rt.anchorMin = new Vector2(0, 0.5f);
        rt.anchorMax = new Vector2(0, 0.5f);
        rt.pivot = new Vector2(0, 0.5f);
        rt.sizeDelta = new Vector2(300, 16);
        rt.anchoredPosition = new Vector2(280, 0);

        var slider = sliderGO.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        Recolor(slider.fillRect, Cyan);
        Recolor(slider.handleRect, Cyan);
        var bg = sliderGO.transform.Find("Background");
        if (bg) Recolor((RectTransform)bg, ControlCol);

        valueLabel = Text(row, "Value", "0%", 16, TextAnchor.MiddleRight, TextLight, FontStyle.Normal);
        var vrt = valueLabel.rectTransform;
        vrt.anchorMin = new Vector2(1, 0.5f);
        vrt.anchorMax = new Vector2(1, 0.5f);
        vrt.pivot = new Vector2(1, 0.5f);
        vrt.sizeDelta = new Vector2(70, 28);
        vrt.anchoredPosition = new Vector2(-16, 0);

        y += 40;
        return slider;
    }

    static Dropdown DropdownRow(Transform window, ref float y, string label, string[] options)
    {
        var row = Row(window, y, 44);
        Label(row, label, 200);

        var ddGO = DefaultControls.CreateDropdown(Res);
        ddGO.name = "Dropdown";
        ddGO.transform.SetParent(row, false);
        var rt = (RectTransform)ddGO.transform;
        rt.anchorMin = new Vector2(1, 0.5f);
        rt.anchorMax = new Vector2(1, 0.5f);
        rt.pivot = new Vector2(1, 0.5f);
        rt.sizeDelta = new Vector2(320, 34);
        rt.anchoredPosition = new Vector2(-16, 0);
        ddGO.GetComponent<Image>().color = ControlCol;

        var dd = ddGO.GetComponent<Dropdown>();
        dd.ClearOptions();
        dd.AddOptions(new System.Collections.Generic.List<string>(options));
        foreach (var t in ddGO.GetComponentsInChildren<Text>(true)) t.color = TextLight;

        y += 46;
        return dd;
    }

    static Toggle ToggleRow(Transform window, ref float y, string label, bool on)
    {
        var row = Row(window, y, 40);
        Label(row, label, 300);

        var tgGO = DefaultControls.CreateToggle(Res);
        tgGO.name = "Toggle";
        tgGO.transform.SetParent(row, false);
        var rt = (RectTransform)tgGO.transform;
        rt.anchorMin = new Vector2(1, 0.5f);
        rt.anchorMax = new Vector2(1, 0.5f);
        rt.pivot = new Vector2(1, 0.5f);
        rt.sizeDelta = new Vector2(40, 28);
        rt.anchoredPosition = new Vector2(-16, 0);

        var toggle = tgGO.GetComponent<Toggle>();
        toggle.isOn = on;
        if (toggle.graphic is Image chk) chk.color = Cyan;
        var bg = tgGO.transform.Find("Background");
        if (bg) bg.GetComponent<Image>().color = ControlCol;
        var lbl = tgGO.transform.Find("Label");
        if (lbl) lbl.gameObject.SetActive(false); // our row already has a label

        y += 42;
        return toggle;
    }

    static Button BottomButton(Transform window, float x, string label, Color color)
    {
        var btnGO = DefaultControls.CreateButton(Res);
        btnGO.name = "Btn_" + Sanitize(label);
        btnGO.transform.SetParent(window, false);
        var rt = (RectTransform)btnGO.transform;
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(200, 48);
        rt.anchoredPosition = new Vector2(x, 24);
        btnGO.GetComponent<Image>().color = color;
        Outline(btnGO, new Color(Cyan.r, Cyan.g, Cyan.b, 0.5f));

        var text = btnGO.GetComponentInChildren<Text>();
        text.text = label;
        text.color = TextLight;
        text.fontStyle = FontStyle.Bold;
        text.fontSize = 16;
        return btnGO.GetComponent<Button>();
    }

    static Button GearButton(Transform canvas)
    {
        var btnGO = DefaultControls.CreateButton(Res);
        btnGO.name = "Btn_OpenSettings";
        btnGO.transform.SetParent(canvas, false);
        var rt = (RectTransform)btnGO.transform;
        rt.anchorMin = new Vector2(1, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(1, 1);
        rt.sizeDelta = new Vector2(48, 48);
        rt.anchoredPosition = new Vector2(-16, -110);
        btnGO.GetComponent<Image>().color = NeutralBtn;
        var text = btnGO.GetComponentInChildren<Text>();
        text.text = "⚙";
        text.color = TextLight;
        text.fontSize = 26;
        return btnGO.GetComponent<Button>();
    }

    // ---------------- Small helpers ----------------

    static Transform Row(Transform window, float y, float h)
    {
        var go = new GameObject("Row");
        go.transform.SetParent(window, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.offsetMin = new Vector2(PadX, 0);
        rt.offsetMax = new Vector2(-PadX, 0);
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, h);
        rt.anchoredPosition = new Vector2(0, -y);
        return go.transform;
    }

    static void Label(Transform row, string label, float width)
    {
        var text = Text(row, "RowLabel", label, 16, TextAnchor.MiddleLeft, TextLight, FontStyle.Normal);
        var rt = text.rectTransform;
        rt.anchorMin = new Vector2(0, 0.5f);
        rt.anchorMax = new Vector2(0, 0.5f);
        rt.pivot = new Vector2(0, 0.5f);
        rt.sizeDelta = new Vector2(width, 28);
        rt.anchoredPosition = new Vector2(16, 0);
    }

    static Canvas FindOrCreateCanvas()
    {
        // Prefer the game's known canvases; otherwise any overlay canvas.
        foreach (var name in new[] { "MenuCanvas", "GameCanvas" })
        {
            var go = GameObject.Find(name);
            if (go && go.TryGetComponent<Canvas>(out var c)) return c;
        }
        var any = Object.FindAnyObjectByType<Canvas>();
        if (any != null) return any;

        var canvasGO = new GameObject("SettingsCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();
    }

    static Image Panel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    static Text Text(Transform parent, string name, string content, int size, TextAnchor align, Color color, FontStyle style)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var text = go.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = content;
        text.fontSize = size;
        text.alignment = align;
        text.color = color;
        text.fontStyle = style;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    static void Recolor(RectTransform rt, Color color)
    {
        if (rt && rt.TryGetComponent<Image>(out var img)) img.color = color;
    }

    static void Outline(GameObject go, Color color)
    {
        var o = go.AddComponent<Outline>();
        o.effectColor = color;
        o.effectDistance = new Vector2(2, -2);
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void Center(RectTransform rt, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = Vector2.zero;
    }

    static void TopLeft(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(x, -y);
    }

    static string Sanitize(string s) => s.Replace(" ", "").Replace("♪", "").Replace("⌾", "").Replace("◉", "").Replace("▢", "").Trim();
}
