// Codex World — Profile panel builder (Editor tool)
// Menu: Tools > Codex World > Build Profile Panel
// Run in the MainMenu scene (after Build Main Menu). Generates the profile
// editor as REAL, editable objects under MenuCanvas:
//   - dark overlay behind the window (draft stand-in for a background blur)
//   - PROFILE NAME input field
//   - PROFILE ICON row (5 avatar portraits sliced from playerAvatar.png)
//   - BADGES toggles (first 3 ticked show on the profile card)
//   - SAVE (persist + refresh card) and BACK (cancel to main menu) buttons
// Also makes the profile-card avatar clickable (opens the panel) and adds a
// mini badge strip under the card. All buttons wired via OnClick() lists.
// Safe to re-run.

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
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

    // settingPanel.png UI kit, loaded by Build().
    static Dictionary<string, Sprite> S;

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

        var avatars = LoadAvatarSprites();
        if (avatars == null) return;
        var badges = LoadBadgeSprites();
        if (badges == null) return;
        if (!LoadKit()) return;

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

        var window = Panel(c, "Window", Color.white, r => Center(r, WW, WH));
        window.sprite = S["SP_Frame_Window"];
        window.type = Image.Type.Sliced;
        var w = window.transform;

        var title = Text(w, "Title", "EDIT PROFILE", 30, TextAnchor.MiddleCenter, Cyan, FontStyle.Bold);
        TopLeft(title.rectTransform, 0, 18, WW, 40);

        // ---- Profile name ----
        SectionHeader(w, 70, "PROFILE NAME");
        panel.nameInput = BuildNameInput(w);

        // ---- Profile icon ----
        SectionHeader(w, 176, "PROFILE ICON");
        panel.avatarSprites = avatars;
        panel.iconOptions = new Button[avatars.Length];
        for (int i = 0; i < avatars.Length; i++)
        {
            panel.iconOptions[i] = BuildIconOption(w, i, avatars[i]);
            UnityEventTools.AddIntPersistentListener(panel.iconOptions[i].onClick, panel.SelectIcon, i);
        }

        // ---- Badges ----
        SectionHeader(w, 320, "BADGES   (unlock achievements to earn - first 3 ticked are shown)");
        panel.badgeSprites = badges;
        panel.badgeToggles = new Toggle[GameProfile.BadgeNames.Length];
        panel.badgeLabels = new Text[GameProfile.BadgeNames.Length];
        for (int i = 0; i < GameProfile.BadgeNames.Length; i++)
            panel.badgeToggles[i] = BuildBadgeToggle(w, i, badges[i], out panel.badgeLabels[i]);

        // ---- Save / Back (kit buttons; the art has the labels baked in) ----
        panel.saveButton = BottomButton(w, -115, "SAVE", S["SP_Btn_Save"], S["SP_Btn_Save_Hi"]);
        panel.backButton = BottomButton(w, 115, "BACK", S["SP_Btn_Cancel"], S["SP_Btn_Cancel_Hi"]);
        UnityEventTools.AddPersistentListener(panel.saveButton.onClick, panel.Save);
        UnityEventTools.AddPersistentListener(panel.backButton.onClick, panel.Close);

        // ---- Make the profile-card avatar open the panel ----
        WireAvatarAndCard(menu, panel, avatars);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = rootGO;
        Debug.Log("Codex World: profile panel built. Click the avatar to open it. Save the scene (Ctrl+S).");
    }

    // ---------------- Profile card hookup ----------------

    static void WireAvatarAndCard(MainMenuController menu, ProfilePanel panel, Sprite[] avatars)
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

            // Real portrait on the card now; drop the old hint/glyph placeholder.
            menu.avatar.sprite = avatars[0];
            menu.avatar.color = Color.white;
            menu.avatar.preserveAspect = true;
            foreach (var childName in new[] { "Hint", "Glyph" })
            {
                var t = avatarGO.transform.Find(childName);
                if (t != null) Object.DestroyImmediate(t.gameObject);
            }
            panel.cardAvatarGlyph = null;

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

    // ---------------- Avatar sprite sheet ----------------

    const string AvatarSheet = "Assets/Design/Image/playerAvatar.png";
    // Portrait plate rects measured from the 1536x1024 sheet (bottom-based y).
    static readonly int[] AvatarX = { 38, 334, 630, 928, 1226 };
    static readonly int[] AvatarW = { 270, 272, 274, 274, 270 };
    const int AvatarY = 372, AvatarH = 292;

    // Slices playerAvatar.png into AV_0..AV_4 on first run, then loads them.
    static Sprite[] LoadAvatarSprites()
    {
        var imp = AssetImporter.GetAtPath(AvatarSheet) as TextureImporter;
        if (imp == null)
        {
            Debug.LogError("Codex World: " + AvatarSheet + " is missing.");
            return null;
        }

        var sprites = CollectAvatars();
        if (sprites != null) return sprites;

        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Multiple;
        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var dp = factory.GetSpriteEditorDataProviderFromObject(imp);
        dp.InitSpriteEditorDataProvider();
        var rects = new SpriteRect[AvatarX.Length];
        for (int i = 0; i < rects.Length; i++)
            rects[i] = new SpriteRect
            {
                name = "AV_" + i,
                rect = new Rect(AvatarX[i], AvatarY, AvatarW[i], AvatarH),
                alignment = SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f),
            };
        dp.SetSpriteRects(rects);
        dp.Apply();
        imp.SaveAndReimport();

        sprites = CollectAvatars();
        if (sprites == null) Debug.LogError("Codex World: slicing " + AvatarSheet + " failed.");
        return sprites;
    }

    static Sprite[] CollectAvatars() => CollectByPrefix(AvatarSheet, "AV_", AvatarX.Length);

    static Sprite[] CollectByPrefix(string path, string prefix, int expected)
    {
        var found = new Sprite[expected];
        int count = 0;
        foreach (var a in AssetDatabase.LoadAllAssetsAtPath(path))
            if (a is Sprite s && s.name.StartsWith(prefix)
                && int.TryParse(s.name.Substring(prefix.Length), out int i)
                && i >= 0 && i < found.Length && found[i] == null)
            {
                found[i] = s;
                count++;
            }
        return count == found.Length ? found : null;
    }

    // ---------------- Badge icons (AchievementUI.png) ----------------

    const string AchSheet = "Assets/Design/Image/AchievementUI.png";
    // Badge plate rects measured from the 1358x339 sheet (bottom-based y).
    static readonly int[] BadgeX = { 649, 712, 773, 834, 896, 951 };
    static readonly int[] BadgeW = { 55, 54, 54, 54, 54, 55 };
    const int BadgeY = 52, BadgeH = 61;

    // Appends ACH_Badge_0..5 to the sheet's existing slices on first run.
    static Sprite[] LoadBadgeSprites()
    {
        var sprites = CollectByPrefix(AchSheet, "ACH_Badge_", BadgeX.Length);
        if (sprites != null) return sprites;

        var imp = AssetImporter.GetAtPath(AchSheet) as TextureImporter;
        if (imp == null)
        {
            Debug.LogError("Codex World: " + AchSheet + " is missing.");
            return null;
        }
        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var dp = factory.GetSpriteEditorDataProviderFromObject(imp);
        dp.InitSpriteEditorDataProvider();
        var rects = new List<SpriteRect>(dp.GetSpriteRects());
        for (int i = 0; i < BadgeX.Length; i++)
            rects.Add(new SpriteRect
            {
                name = "ACH_Badge_" + i,
                rect = new Rect(BadgeX[i], BadgeY, BadgeW[i], BadgeH),
                alignment = SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f),
            });
        dp.SetSpriteRects(rects.ToArray());
        dp.Apply();
        imp.SaveAndReimport();

        sprites = CollectByPrefix(AchSheet, "ACH_Badge_", BadgeX.Length);
        if (sprites == null) Debug.LogError("Codex World: slicing badge icons failed.");
        return sprites;
    }

    // ---------------- settingPanel.png kit ----------------

    const string KitSheet = "Assets/Design/Image/settingPanel.png";

    static bool LoadKit()
    {
        S = new Dictionary<string, Sprite>();
        foreach (var a in AssetDatabase.LoadAllAssetsAtPath(KitSheet))
            if (a is Sprite s) S[s.name] = s;
        if (!S.ContainsKey("SP_Frame_Window") || !S.ContainsKey("SP_Toggle_Off"))
        {
            Debug.LogError("Codex World: " + KitSheet + " is not sliced (need SP_Frame_Window, SP_Plate, buttons, toggles).");
            return false;
        }
        return true;
    }

    // ---------------- Controls ----------------

    static InputField BuildNameInput(Transform w)
    {
        var go = DefaultControls.CreateInputField(Res);
        go.name = "NameInput";
        go.transform.SetParent(w, false);
        TopLeft((RectTransform)go.transform, PadX, 112, WW - PadX * 2, 44);
        var bgImage = go.GetComponent<Image>();
        bgImage.sprite = S["SP_Plate"];
        bgImage.type = Image.Type.Sliced;
        bgImage.color = new Color(0.55f, 0.65f, 0.78f, 1f); // darkened plate so typed text pops

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

    static Button BuildIconOption(Transform w, int index, Sprite sprite)
    {
        var img = Panel(w, $"Icon_{index}", Color.white,
            r => TopLeft(r, PadX + index * 135, 218, 80, 80));
        img.sprite = sprite;
        img.preserveAspect = true;

        var o = img.gameObject.AddComponent<Outline>();
        o.effectColor = Color.white;
        o.effectDistance = new Vector2(3, -3);
        o.enabled = index == 0; // runtime Open()/SelectIcon() drives selection

        return img.gameObject.AddComponent<Button>();
    }

    static Toggle BuildBadgeToggle(Transform w, int index, Sprite icon, out Text label)
    {
        var go = DefaultControls.CreateToggle(Res);
        go.name = $"Badge_{GameProfile.BadgeNames[index].Replace(" ", "")}";
        go.transform.SetParent(w, false);
        int col = index % 2, row = index / 2;
        TopLeft((RectTransform)go.transform, 60 + col * 310, 362 + row * 44, 300, 36);

        // Kit checkbox: off-plate as background, on-plate as the checkmark.
        var bg = go.transform.Find("Background")?.GetComponent<Image>();
        if (bg != null)
        {
            var r = bg.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0f, 0.5f);
            r.pivot = new Vector2(0f, 0.5f);
            r.anchoredPosition = Vector2.zero;
            r.sizeDelta = new Vector2(28, 28);
            bg.sprite = S["SP_Toggle_Off"];
            bg.color = Color.white;
            bg.preserveAspect = true;
        }
        var chk = go.transform.Find("Background/Checkmark")?.GetComponent<Image>();
        if (chk != null)
        {
            Stretch(chk.rectTransform);
            chk.sprite = S["SP_Toggle_On"];
            chk.color = Color.white;
            chk.preserveAspect = true;
        }

        // The achievement's badge art next to the checkbox.
        var iconImg = Panel(go.transform, "BadgeIcon", Color.white, r =>
        {
            r.anchorMin = r.anchorMax = new Vector2(0f, 0.5f);
            r.pivot = new Vector2(0f, 0.5f);
            r.anchoredPosition = new Vector2(38, 0);
            r.sizeDelta = new Vector2(30, 30);
        });
        iconImg.sprite = icon;
        iconImg.preserveAspect = true;

        label = go.transform.Find("Label")?.GetComponent<Text>();
        if (label != null)
        {
            var r = label.rectTransform;
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = new Vector2(78, 0);
            r.offsetMax = Vector2.zero;
            label.text = GameProfile.BadgeNames[index];
            label.color = GameProfile.BadgeColors[index];
            label.fontSize = 15;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleLeft;
        }
        return go.GetComponent<Toggle>();
    }

    static Button BottomButton(Transform w, float x, string name, Sprite normal, Sprite hover)
    {
        var go = new GameObject("Btn_" + name.Replace(" ", ""));
        go.transform.SetParent(w, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(210, 62);
        rt.anchoredPosition = new Vector2(x, 22);
        var img = go.AddComponent<Image>();
        img.sprite = normal;
        img.preserveAspect = true;
        var btn = go.AddComponent<Button>();
        btn.transition = Selectable.Transition.SpriteSwap;
        var ss = btn.spriteState;
        ss.highlightedSprite = hover;
        ss.pressedSprite = hover;
        btn.spriteState = ss;
        return btn;
    }

    static void SectionHeader(Transform w, float y, string label)
    {
        var bar = Panel(w, "Section_" + label.Split(' ')[0], Color.white, r => TopLeft(r, PadX, y, WW - PadX * 2, 32));
        bar.sprite = S["SP_Plate"];
        bar.type = Image.Type.Sliced;
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
