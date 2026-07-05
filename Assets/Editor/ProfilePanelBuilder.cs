// Codex World — Profile panel builder (Editor tool)
// Menu: Tools > Codex World > Build Profile Panel
// Run in the MainMenu scene (after Build Main Menu). Generates the profile
// editor as REAL, editable objects under MenuCanvas:
//   - dark overlay behind the window (draft stand-in for a background blur)
//   - PROFILE NAME input field
//   - PROFILE ICON row (6 selectable placeholder icons)
//   - BADGES toggles (first 3 ticked show on the profile card)
//   - SAVE (persist + refresh card) and BACK (cancel to main menu) buttons
// Also makes the profile-card avatar clickable (opens the panel) and adds a
// mini badge strip under the card. All buttons wired via OnClick() lists.
// Safe to re-run.

using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class ProfilePanelBuilder
{
    const float WW = 700f, WH = 640f;
    const float PadX = 40f;

    static readonly Color DimCol     = new Color(0.01f, 0.03f, 0.06f, 0.88f);
    static readonly Color WindowCol  = new Color(0.07f, 0.12f, 0.20f, 0.98f);
    static readonly Color SectionCol = new Color(0.10f, 0.16f, 0.26f, 0.95f);
    static readonly Color ControlCol = new Color(0.05f, 0.09f, 0.15f, 1f);
    static readonly Color Cyan       = new Color(0.45f, 0.85f, 1f, 1f);
    static readonly Color TextLight  = new Color(0.88f, 0.94f, 1f, 1f);
    static readonly Color SaveCol    = new Color(0.20f, 0.55f, 0.45f, 1f);
    static readonly Color NeutralBtn = new Color(0.12f, 0.20f, 0.30f, 1f);

    static DefaultControls.Resources Res => new DefaultControls.Resources();

    [MenuItem("Tools/Codex World/Build Profile Panel")]
    public static void Build()
    {
        var canvasGO = GameObject.Find("MenuCanvas");
        var menu = Object.FindAnyObjectByType<MainMenuController>();
        if (canvasGO == null || menu == null)
        {
            Debug.LogError("Codex World: open the MainMenu scene and run Build Main Menu first.");
            return;
        }
        var canvas = canvasGO.transform;

        var old = canvas.Find("ProfilePanel");
        if (old != null) Object.DestroyImmediate(old.gameObject);

        // Root stays active (Awake wires state); Content child is toggled.
        var rootGO = new GameObject("ProfilePanel");
        rootGO.transform.SetParent(canvas, false);
        Stretch(rootGO.AddComponent<RectTransform>());
        var panel = rootGO.AddComponent<ProfilePanel>();

        var content = new GameObject("Content");
        content.transform.SetParent(rootGO.transform, false);
        Stretch(content.AddComponent<RectTransform>());
        panel.panelRoot = content;
        var c = content.transform;

        // Dark overlay = draft "blur": blocks clicks and dims the menu behind.
        var dim = Panel(c, "BlurOverlay", DimCol, Stretch);
        dim.raycastTarget = true;

        var window = Panel(c, "Window", WindowCol, r => Center(r, WW, WH));
        Outline(window.gameObject, Cyan);
        var w = window.transform;

        var title = Text(w, "Title", "EDIT PROFILE", 30, TextAnchor.MiddleCenter, Cyan, FontStyle.Bold);
        TopLeft(title.rectTransform, 0, 18, WW, 40);

        // ---- Profile name ----
        SectionHeader(w, 70, "PROFILE NAME");
        panel.nameInput = BuildNameInput(w);

        // ---- Profile icon ----
        SectionHeader(w, 176, "PROFILE ICON");
        panel.iconOptions = new Button[GameProfile.IconGlyphs.Length];
        for (int i = 0; i < GameProfile.IconGlyphs.Length; i++)
        {
            panel.iconOptions[i] = BuildIconOption(w, i);
            UnityEventTools.AddIntPersistentListener(panel.iconOptions[i].onClick, panel.SelectIcon, i);
        }

        // ---- Badges ----
        SectionHeader(w, 320, "BADGES   (first 3 ticked are shown)");
        panel.badgeToggles = new Toggle[GameProfile.BadgeNames.Length];
        for (int i = 0; i < GameProfile.BadgeNames.Length; i++)
            panel.badgeToggles[i] = BuildBadgeToggle(w, i);

        // ---- Save / Back ----
        panel.saveButton = BottomButton(w, -115, "SAVE", SaveCol);
        panel.backButton = BottomButton(w, 115, "BACK", NeutralBtn);
        UnityEventTools.AddPersistentListener(panel.saveButton.onClick, panel.Save);
        UnityEventTools.AddPersistentListener(panel.backButton.onClick, panel.Close);

        // ---- Make the profile-card avatar open the panel ----
        WireAvatarAndCard(menu, panel);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = rootGO;
        Debug.Log("Codex World: profile panel built. Click the avatar to open it. Save the scene (Ctrl+S).");
    }

    // ---------------- Profile card hookup ----------------

    static void WireAvatarAndCard(MainMenuController menu, ProfilePanel panel)
    {
        panel.cardNameText = menu.playerNameText;
        panel.cardAvatar = menu.avatar;

        if (menu.avatar != null)
        {
            var avatarGO = menu.avatar.gameObject;
            var btn = avatarGO.GetComponent<Button>();
            if (btn == null) btn = avatarGO.AddComponent<Button>();
            while (btn.onClick.GetPersistentEventCount() > 0)
                UnityEventTools.RemovePersistentListener(btn.onClick, 0);
            UnityEventTools.AddPersistentListener(btn.onClick, panel.Open);
            EditorUtility.SetDirty(btn);

            // Re-purpose the "AVATAR" hint text as the icon glyph.
            var hint = avatarGO.transform.Find("Hint");
            Text glyph = hint != null ? hint.GetComponent<Text>() : null;
            if (glyph == null)
                glyph = Text(avatarGO.transform, "Glyph", "</>", 26, TextAnchor.MiddleCenter, Color.black, FontStyle.Bold);
            glyph.fontSize = 26;
            glyph.fontStyle = FontStyle.Bold;
            glyph.color = new Color(0f, 0f, 0f, 0.8f);
            Stretch(glyph.rectTransform);
            panel.cardAvatarGlyph = glyph;

            // Mini badge strip hanging under the card.
            var card = menu.avatar.transform.parent;
            var oldStrip = card.Find("BadgeStrip");
            if (oldStrip != null) Object.DestroyImmediate(oldStrip.gameObject);
            var strip = new GameObject("BadgeStrip");
            strip.transform.SetParent(card, false);
            var srt = strip.AddComponent<RectTransform>();
            srt.anchorMin = srt.anchorMax = new Vector2(0f, 0f);
            srt.pivot = new Vector2(0f, 1f);
            srt.anchoredPosition = new Vector2(21, -4);
            srt.sizeDelta = new Vector2(100, 26);

            panel.cardBadgeSlots = new Image[GameProfile.MaxBadges];
            panel.cardBadgeGlyphs = new Text[GameProfile.MaxBadges];
            for (int i = 0; i < GameProfile.MaxBadges; i++)
            {
                var slot = Panel(strip.transform, $"Badge{i + 1}", Color.white, r =>
                {
                    r.anchorMin = r.anchorMax = new Vector2(0f, 1f);
                    r.pivot = new Vector2(0f, 1f);
                    r.sizeDelta = new Vector2(26, 26);
                });
                ((RectTransform)slot.transform).anchoredPosition = new Vector2(i * 32, 0);
                Outline(slot.gameObject, new Color(0f, 0f, 0f, 0.6f));
                var g = Text(slot.transform, "Glyph", "?", 12, TextAnchor.MiddleCenter, new Color(0f, 0f, 0f, 0.85f), FontStyle.Bold);
                Stretch(g.rectTransform);
                panel.cardBadgeSlots[i] = slot;
                panel.cardBadgeGlyphs[i] = g;
            }
        }
        EditorUtility.SetDirty(menu);
    }

    // ---------------- Controls ----------------

    static InputField BuildNameInput(Transform w)
    {
        var go = DefaultControls.CreateInputField(Res);
        go.name = "NameInput";
        go.transform.SetParent(w, false);
        TopLeft((RectTransform)go.transform, PadX, 112, WW - PadX * 2, 44);
        go.GetComponent<Image>().color = ControlCol;

        var input = go.GetComponent<InputField>();
        input.characterLimit = 16;
        foreach (var t in go.GetComponentsInChildren<Text>(true))
        {
            t.color = TextLight;
            t.fontSize = 18;
        }
        var placeholder = input.placeholder as Text;
        if (placeholder != null)
        {
            placeholder.text = "Enter profile name...";
            placeholder.color = new Color(0.5f, 0.55f, 0.62f);
        }
        return input;
    }

    static Button BuildIconOption(Transform w, int index)
    {
        var img = Panel(w, $"Icon_{index}", GameProfile.IconColors[index],
            r => TopLeft(r, PadX + index * 105, 218, 80, 80));
        var glyph = Text(img.transform, "Glyph", GameProfile.IconGlyphs[index], 24,
            TextAnchor.MiddleCenter, new Color(0f, 0f, 0f, 0.8f), FontStyle.Bold);
        Stretch(glyph.rectTransform);

        var o = img.gameObject.AddComponent<Outline>();
        o.effectColor = Color.white;
        o.effectDistance = new Vector2(3, -3);
        o.enabled = index == 0; // runtime Open()/SelectIcon() drives selection

        return img.gameObject.AddComponent<Button>();
    }

    static Toggle BuildBadgeToggle(Transform w, int index)
    {
        var go = DefaultControls.CreateToggle(Res);
        go.name = $"Badge_{GameProfile.BadgeNames[index].Replace(" ", "")}";
        go.transform.SetParent(w, false);
        int col = index % 2, row = index / 2;
        TopLeft((RectTransform)go.transform, 60 + col * 310, 362 + row * 44, 300, 36);

        var label = go.transform.Find("Label")?.GetComponent<Text>();
        if (label != null)
        {
            label.text = $"{GameProfile.BadgeGlyphs[index]}   {GameProfile.BadgeNames[index]}";
            label.color = GameProfile.BadgeColors[index];
            label.fontSize = 15;
            label.fontStyle = FontStyle.Bold;
        }
        var bg = go.transform.Find("Background")?.GetComponent<Image>();
        if (bg != null) bg.color = ControlCol;
        var toggle = go.GetComponent<Toggle>();
        if (toggle.graphic is Image chk) chk.color = Cyan;
        return toggle;
    }

    static Button BottomButton(Transform w, float x, string label, Color color)
    {
        var go = new GameObject("Btn_" + label.Replace(" ", ""));
        go.transform.SetParent(w, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(200, 48);
        rt.anchoredPosition = new Vector2(x, 26);
        go.AddComponent<Image>().color = color;
        Outline(go, new Color(Cyan.r, Cyan.g, Cyan.b, 0.5f));
        var btn = go.AddComponent<Button>();
        var text = Text(go.transform, "Label", label, 16, TextAnchor.MiddleCenter, TextLight, FontStyle.Bold);
        Stretch(text.rectTransform);
        return btn;
    }

    static void SectionHeader(Transform w, float y, string label)
    {
        var bar = Panel(w, "Section_" + label.Split(' ')[0], SectionCol, r => TopLeft(r, PadX, y, WW - PadX * 2, 32));
        var text = Text(bar.transform, "Label", label, 16, TextAnchor.MiddleLeft, Cyan, FontStyle.Bold);
        Stretch(text.rectTransform);
        text.rectTransform.offsetMin = new Vector2(14, 0);
        text.rectTransform.offsetMax = new Vector2(-14, 0);
    }

    // ---------------- Building blocks ----------------

    static Image Panel(Transform parent, string name, Color color, System.Action<RectTransform> layout)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        layout(rt);
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
}
