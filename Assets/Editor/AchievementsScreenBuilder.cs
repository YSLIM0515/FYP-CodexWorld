// Codex World — Achievements screen builder (Editor tool, sprite-skinned)
// Menu: Tools > Codex World > Build Achievements Screen
// Rebuilds the achievements screen under MenuCanvas using the sliced sprites
// from Assets/Design/Image/AchievementUI.png (icons, tier shields, border
// frames, lock, progress bar). Cards carry an AchievementCard component and
// are built LOCKED by default — AchievementsScreen.Refresh() restyles them
// from AchievementManager's saved state at runtime. Names/descriptions come
// from AchievementManager.Defs so UI and unlock logic always match.
// Requires the sheet to be sliced first. Safe to re-run.

using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class AchievementsScreenBuilder
{
    const string SheetPath = "Assets/Design/Image/AchievementUI.png";

    static readonly Color GoldCol   = new Color(0.94f, 0.76f, 0.29f);
    static readonly Color BronzeCol = new Color(0.76f, 0.48f, 0.22f);
    static readonly Color SilverCol = new Color(0.75f, 0.80f, 0.87f);
    static readonly Color BgCol     = new Color(0.04f, 0.07f, 0.13f, 1f);
    static readonly Color PanelCol  = new Color(0.08f, 0.13f, 0.22f, 0.97f);
    static readonly Color GreyText  = new Color(0.55f, 0.58f, 0.63f, 1f);
    static readonly Color TextLight = new Color(0.88f, 0.94f, 1f, 1f);

    [MenuItem("Tools/Codex World/Build Achievements Screen")]
    public static void Build()
    {
        var sp = new System.Collections.Generic.Dictionary<string, Sprite>();
        foreach (var a in AssetDatabase.LoadAllAssetsAtPath(SheetPath))
            if (a is Sprite s) sp[s.name] = s;
        if (!sp.ContainsKey("ACH_Icon_0") || !sp.ContainsKey("ACH_Frame_Grey"))
        {
            Debug.LogError("Codex World: AchievementUI.png is not sliced yet (need ACH_Icon_0..17, frames, shields, lock, bar).");
            return;
        }

        var canvasGO = GameObject.Find("MenuCanvas");
        if (canvasGO == null)
        {
            Debug.LogError("Codex World: open the MainMenu scene and run Build Main Menu first.");
            return;
        }
        var canvas = canvasGO.transform;
        var old = canvas.Find("AchievementsScreen");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var oldBtn = canvas.Find("Btn_Achievements");
        if (oldBtn != null) Object.DestroyImmediate(oldBtn.gameObject);

        var rootGO = new GameObject("AchievementsScreen");
        rootGO.transform.SetParent(canvas, false);
        Stretch(rootGO.AddComponent<RectTransform>());
        var screen = rootGO.AddComponent<AchievementsScreen>();

        var content = new GameObject("Content");
        content.transform.SetParent(rootGO.transform, false);
        Stretch(content.AddComponent<RectTransform>());
        screen.panelRoot = content;
        var c = content.transform;

        Panel(c, "Background", BgCol, Stretch);

        // ---- Header ----
        var title = Text(c, "Title", "ACHIEVEMENTS", 40, TextAnchor.MiddleLeft, GoldCol, FontStyle.Bold);
        TopLeft(title.rectTransform, 70, 25, 600, 50);
        var sub = Text(c, "Subtitle", "Complete challenges and earn glory!", 16, TextAnchor.MiddleLeft, TextLight, FontStyle.Normal);
        TopLeft(sub.rectTransform, 74, 78, 600, 24);

        var label = Text(c, "OverallLabel", "OVERALL PROGRESS", 15, TextAnchor.MiddleCenter, TextLight, FontStyle.Bold);
        TopLeft(label.rectTransform, 700, 22, 420, 22);
        var track = Panel(c, "ProgressTrack", Color.white, r => TopLeft(r, 700, 50, 420, 32));
        track.sprite = sp["ACH_Bar_Track"];
        track.type = Image.Type.Sliced;
        var fill = Panel(track.transform, "ProgressFill", Color.white, r => { Stretch(r); r.offsetMin = new Vector2(4, 5); r.offsetMax = new Vector2(-4, -5); });
        fill.sprite = sp["ACH_Bar_Fill"];
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 0f;
        screen.progressFill = fill;
        screen.progressLabel = Text(track.transform, "Percent", "0%", 15, TextAnchor.MiddleCenter, TextLight, FontStyle.Bold);
        Stretch(screen.progressLabel.rectTransform);
        screen.fractionLabel = Text(c, "Fraction", "0 / 18", 20, TextAnchor.MiddleLeft, TextLight, FontStyle.Bold);
        TopLeft(screen.fractionLabel.rectTransform, 1145, 50, 120, 30);

        // ---- Tier rows ----
        var defs = AchievementManager.Defs;
        screen.cards = new AchievementCard[defs.Length];
        screen.icons = new Sprite[defs.Length];
        screen.tierCounts = new Text[3];
        for (int i = 0; i < defs.Length; i++) screen.icons[i] = sp["ACH_Icon_" + i];

        var tierData = new (string name, Color color, string shield, string frame)[]
        {
            // Every tier shares the same gold frame when unlocked. On the sheet
            // the sprite NAMED ACH_Frame_Orange is the yellow/gold-outline one.
            ("BRONZE", BronzeCol, "ACH_Shield_Bronze", "ACH_Frame_Orange"),
            ("SILVER", SilverCol, "ACH_Shield_Silver", "ACH_Frame_Orange"),
            ("GOLD",   GoldCol,   "ACH_Shield_Gold",   "ACH_Frame_Orange"),
        };

        float y = 140f;
        for (int t = 0; t < 3; t++)
        {
            var shield = Panel(c, "Shield_" + tierData[t].name, Color.white, r => TopLeft(r, 68, y + 15, 124, 160));
            shield.sprite = sp[tierData[t].shield];
            shield.preserveAspect = true;
            screen.tierCounts[t] = Text(c, "Count_" + tierData[t].name, "0 / 6", 16, TextAnchor.MiddleCenter, TextLight, FontStyle.Bold);
            TopLeft(screen.tierCounts[t].rectTransform, 60, y + 182, 140, 24);

            var cards = ScrollRow(c, "Row_" + tierData[t].name, 220, y, 1210, 250, 6 * 200f - 10f);
            for (int k = 0; k < 6; k++)
            {
                int idx = t * 6 + k;
                screen.cards[idx] = BuildCard(cards, idx, defs[idx].Name, defs[idx].Desc,
                    sp["ACH_Icon_" + idx], sp["ACH_Frame_Grey"], sp[tierData[t].frame], sp["ACH_Lock"],
                    tierData[t].color, k * 200f);
            }
            y += 275f;
        }

        // ---- Recently Unlocked panel ----
        var panel = Panel(c, "RecentlyUnlocked", PanelCol, r => TopLeft(r, 1460, 25, 420, 940));
        Outline(panel.gameObject, GoldCol);
        var rTitle = Text(panel.transform, "Title", "*  RECENTLY UNLOCKED  *", 18, TextAnchor.MiddleCenter, GoldCol, FontStyle.Bold);
        TopLeft(rTitle.rectTransform, 0, 22, 420, 28);

        screen.recentRoots = new GameObject[3];
        screen.recentIcons = new Image[3];
        screen.recentNames = new Text[3];
        screen.recentDescs = new Text[3];
        screen.recentTimes = new Text[3];
        for (int s = 0; s < 3; s++)
        {
            float ry = 70f + s * 150f;
            var card = Panel(panel.transform, "Recent" + (s + 1), Color.white, rt => TopLeft(rt, 20, ry, 380, 132));
            card.sprite = sp["ACH_Frame_Gold"];
            card.type = Image.Type.Sliced;
            screen.recentRoots[s] = card.gameObject;

            screen.recentIcons[s] = Panel(card.transform, "Icon", Color.white, rt => TopLeft(rt, 18, 34, 64, 64));
            screen.recentIcons[s].preserveAspect = true;
            screen.recentNames[s] = Text(card.transform, "Name", "", 16, TextAnchor.MiddleLeft, GoldCol, FontStyle.Bold);
            TopLeft(screen.recentNames[s].rectTransform, 100, 20, 260, 24);
            screen.recentDescs[s] = Text(card.transform, "Desc", "", 12, TextAnchor.UpperLeft, TextLight, FontStyle.Normal);
            TopLeft(screen.recentDescs[s].rectTransform, 100, 48, 260, 40);
            screen.recentDescs[s].horizontalOverflow = HorizontalWrapMode.Wrap;
            screen.recentTimes[s] = Text(card.transform, "Time", "", 11, TextAnchor.MiddleLeft, GreyText, FontStyle.Normal);
            TopLeft(screen.recentTimes[s].rectTransform, 100, 98, 260, 18);
            card.gameObject.SetActive(false); // shown by Refresh() once something unlocks
        }

        var hint = Text(panel.transform, "EmptyHint", "Nothing unlocked yet —\nsolve puzzles to earn glory!", 14, TextAnchor.MiddleCenter, GreyText, FontStyle.Italic);
        TopLeft(hint.rectTransform, 20, 90, 380, 60);
        screen.recentEmptyHint = hint.gameObject; // Refresh() hides it once anything unlocks

        // ---- Bottom bar ----
        var bar = Panel(c, "TipBar", PanelCol, r =>
        {
            r.anchorMin = new Vector2(0, 0); r.anchorMax = new Vector2(1, 0);
            r.pivot = new Vector2(0.5f, 0);
            r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
            r.sizeDelta = new Vector2(0, 50);
        });
        var tip = Text(bar.transform, "Tip", "Tip: Complete achievements to earn rewards and show off your accomplishments!",
            14, TextAnchor.MiddleLeft, TextLight, FontStyle.Normal);
        Stretch(tip.rectTransform);
        tip.rectTransform.offsetMin = new Vector2(70, 0);
        tip.rectTransform.offsetMax = new Vector2(-200, 0);
        var back = ButtonRect(bar.transform, "Btn_Back", "ESC   Back", new Color(0.10f, 0.15f, 0.24f, 1f), TextLight);
        var brt = (RectTransform)back.transform;
        brt.anchorMin = brt.anchorMax = new Vector2(1, 0.5f);
        brt.pivot = new Vector2(1, 0.5f);
        brt.sizeDelta = new Vector2(150, 38);
        brt.anchoredPosition = new Vector2(-30, 0);
        screen.backButton = back;
        UnityEventTools.AddPersistentListener(back.onClick, screen.Close);

        // ---- CHALLENGES button opens this screen ----
        var menu = Object.FindAnyObjectByType<MainMenuController>();
        if (menu != null)
        {
            menu.achievementsScreen = screen;
            if (menu.challengesButton != null)
            {
                while (menu.challengesButton.onClick.GetPersistentEventCount() > 0)
                    UnityEventTools.RemovePersistentListener(menu.challengesButton.onClick, 0);
                UnityEventTools.AddPersistentListener(menu.challengesButton.onClick, menu.OnAchievements);
                EditorUtility.SetDirty(menu.challengesButton);
            }
            EditorUtility.SetDirty(menu);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = rootGO;
        Debug.Log("Codex World: achievements screen rebuilt with sprites — all locked by default. Save the scene (Ctrl+S).");
    }

    static AchievementCard BuildCard(Transform content, int index, string name, string desc,
        Sprite icon, Sprite lockedFrame, Sprite unlockedFrame, Sprite padlock, Color tierColor, float x)
    {
        var frame = Panel(content, "Card_" + name.Replace(" ", ""), Color.white, r => TopLeft(r, x, 10, 180, 230));
        frame.sprite = lockedFrame;
        frame.type = Image.Type.Sliced;

        var card = frame.gameObject.AddComponent<AchievementCard>();
        card.index = index;
        card.frame = frame;
        card.lockedFrameSprite = lockedFrame;
        card.unlockedFrameSprite = unlockedFrame;
        card.unlockedNameColor = tierColor;

        var iconImg = Panel(frame.transform, "Icon", Color.white, r => TopLeft(r, 55, 25, 70, 70));
        iconImg.sprite = icon;
        iconImg.preserveAspect = true;
        card.icon = iconImg;

        var padlockImg = Panel(frame.transform, "Padlock", Color.white, r => TopLeft(r, 66, 32, 48, 56));
        padlockImg.sprite = padlock;
        padlockImg.preserveAspect = true;
        card.padlock = padlockImg.gameObject;

        card.nameText = Text(frame.transform, "Name", name, 15, TextAnchor.MiddleCenter, GreyText, FontStyle.Bold);
        TopLeft(card.nameText.rectTransform, 0, 110, 180, 24);
        card.descText = Text(frame.transform, "Desc", desc, 12, TextAnchor.UpperCenter, GreyText, FontStyle.Normal);
        TopLeft(card.descText.rectTransform, 15, 140, 150, 80);
        card.descText.horizontalOverflow = HorizontalWrapMode.Wrap;

        card.Apply(false); // locked by default
        return card;
    }

    // ---------------- Building blocks ----------------

    static Transform ScrollRow(Transform parent, string name, float x, float y, float w, float h, float contentW)
    {
        var view = new GameObject(name);
        view.transform.SetParent(parent, false);
        var vrt = view.AddComponent<RectTransform>();
        TopLeft(vrt, x, y, w, h);
        var img = view.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.001f);
        view.AddComponent<RectMask2D>();
        var sr = view.AddComponent<ScrollRect>();

        var content = new GameObject("Cards");
        content.transform.SetParent(view.transform, false);
        var crt = content.AddComponent<RectTransform>();
        crt.anchorMin = crt.anchorMax = new Vector2(0, 1);
        crt.pivot = new Vector2(0, 1);
        crt.sizeDelta = new Vector2(Mathf.Max(contentW, w), h);
        crt.anchoredPosition = Vector2.zero;

        sr.content = crt;
        sr.viewport = vrt;
        sr.horizontal = true;
        sr.vertical = false;
        sr.movementType = ScrollRect.MovementType.Elastic;
        sr.scrollSensitivity = 20f;
        return content.transform;
    }

    static Button ButtonRect(Transform parent, string name, string label, Color bg, Color textColor)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var img = go.AddComponent<Image>();
        img.color = bg;
        var btn = go.AddComponent<Button>();
        Outline(go, new Color(GoldCol.r, GoldCol.g, GoldCol.b, 0.55f));
        var text = Text(go.transform, "Label", label, 15, TextAnchor.MiddleCenter, textColor, FontStyle.Bold);
        Stretch(text.rectTransform);
        return btn;
    }

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

    static void TopLeft(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(x, -y);
    }
}
