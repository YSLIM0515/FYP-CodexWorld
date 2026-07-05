// Codex World — Achievements screen builder (Editor tool, display-only draft)
// Menu: Tools > Codex World > Build Achievements Screen
// Generates the achievements template as REAL, editable objects under the
// MainMenu canvas, matching the reference mock-up:
//   - Bronze / Silver / Gold tier badges with horizontal scrollable card rows
//   - ~square achievement cards: icon, name, unlock condition
//   - locked cards in greyscale with a padlock built from simple rects
//   - overall progress bar (% + fraction) at the top
//   - "Recently Unlocked" gold-accent panel (3 entries with timestamps)
//   - bottom tip bar with an ESC/Back button
// All achievements are coding-themed placeholders baked into the scene — pure
// display, not linked to gameplay yet. The menu's CHALLENGES button is retargeted
// to MainMenuController.OnAchievements() to open this screen. Safe to re-run.

using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class AchievementsScreenBuilder
{
    // ---------------- Placeholder data (edit the generated scene objects freely) ----------------

    class Ach { public string Name, Desc, Icon; public bool Unlocked;
        public Ach(string n, string d, string i, bool u) { Name = n; Desc = d; Icon = i; Unlocked = u; } }

    class Tier { public string Name; public Color Color; public Ach[] Items;
        public Tier(string n, Color c, Ach[] i) { Name = n; Color = c; Items = i; } }

    static readonly Color GoldCol   = new Color(0.94f, 0.76f, 0.29f);
    static readonly Color BronzeCol = new Color(0.76f, 0.48f, 0.22f);
    static readonly Color SilverCol = new Color(0.75f, 0.80f, 0.87f);

    static Tier[] Tiers() => new[]
    {
        new Tier("BRONZE", BronzeCol, new[]
        {
            new Ach("Hello World", "Solve your first puzzle.", "</>", true),
            new Ach("Debugger", "Retry after a failed run.", "!", true),
            new Ach("Turn Around", "Use a turn command.", "<~", true),
            new Ach("Valley Clear", "Complete Sequence Valley.", "", false),
            new Ach("Minimalist", "Solve a level with the optimal commands.", "", false),
            new Ach("Quick Thinker", "Solve a level in under 30 seconds.", "", false),
        }),
        new Tier("SILVER", SilverCol, new[]
        {
            new Ach("Loop Learner", "Run your first loop.", "x4", true),
            new Ach("Efficient Coder", "Solve 3 levels optimally.", "++", true),
            new Ach("Refactorer", "Clear the queue and solve a better way.", "{}", true),
            new Ach("Factory Foreman", "Complete Loop Factory.", "", false),
            new Ach("Nested Mind", "Use a loop inside a loop.", "", false),
            new Ach("Bug Squasher", "Recover from 10 failed runs.", "", false),
        }),
        new Tier("GOLD", GoldCol, new[]
        {
            new Ach("Logic Master", "Use an IF conditional.", "IF", true),
            new Ach("Algorithm Architect", "Combine loops and conditionals.", "A*", true),
            new Ach("Virus Purger", "Purge the Null Virus.", "@", true),
            new Ach("Perfectionist", "Solve every level optimally.", "", false),
            new Ach("Speed Demon", "Finish the game in under 20 minutes.", "", false),
            new Ach("True Coder", "Earn every other achievement.", "", false),
        }),
    };

    // Shown in the Recently Unlocked panel (names exist above).
    static readonly (string Name, string Desc, string Icon, string Time)[] Recent =
    {
        ("Logic Master", "Use an IF conditional.", "IF", "2026-07-01 21:47"),
        ("Loop Learner", "Run your first loop.", "x4", "2026-06-28 15:12"),
        ("Hello World", "Solve your first puzzle.", "</>", "2026-06-25 14:08"),
    };

    // ---------------- Palette ----------------

    static readonly Color BgCol       = new Color(0.04f, 0.07f, 0.13f, 1f);
    static readonly Color PanelCol    = new Color(0.08f, 0.13f, 0.22f, 0.97f);
    static readonly Color CardCol     = new Color(0.10f, 0.15f, 0.24f, 1f);
    static readonly Color LockedCard  = new Color(0.10f, 0.11f, 0.14f, 1f);
    static readonly Color GreyIcon    = new Color(0.22f, 0.24f, 0.28f, 1f);
    static readonly Color GreyDetail  = new Color(0.45f, 0.48f, 0.53f, 1f);
    static readonly Color GreyText    = new Color(0.55f, 0.58f, 0.63f, 1f);
    static readonly Color TextLight   = new Color(0.88f, 0.94f, 1f, 1f);
    static readonly Color TrackCol    = new Color(0f, 0f, 0f, 0.55f);

    [MenuItem("Tools/Codex World/Build Achievements Screen")]
    public static void Build()
    {
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

        // Root stays active (Awake must run); Content child is toggled.
        var rootGO = new GameObject("AchievementsScreen");
        rootGO.transform.SetParent(canvas, false);
        Stretch(rootGO.AddComponent<RectTransform>());
        var screen = rootGO.AddComponent<AchievementsScreen>();

        var content = new GameObject("Content");
        content.transform.SetParent(rootGO.transform, false);
        Stretch(content.AddComponent<RectTransform>());
        screen.panelRoot = content;
        var c = content.transform;

        Panel(c, "Background", BgCol, r => Stretch(r));

        BuildHeader(c, screen);

        var tiers = Tiers();
        float y = 140f;
        foreach (var tier in tiers)
        {
            BuildTierRow(c, tier, y);
            y += 275f;
        }

        BuildRecentPanel(c, screen);
        BuildBottomBar(c, screen);

        // The menu's CHALLENGES button is the achievements opener: clear any
        // stale persistent entries on it and retarget OnAchievements.
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
        Debug.Log("Codex World: achievements screen built (display-only draft). Save the scene (Ctrl+S).");
    }

    // ---------------- Header ----------------

    static void BuildHeader(Transform c, AchievementsScreen screen)
    {
        var title = Text(c, "Title", "ACHIEVEMENTS", 40, TextAnchor.MiddleLeft, GoldCol, FontStyle.Bold);
        TopLeft(title.rectTransform, 70, 25, 600, 50);
        var sub = Text(c, "Subtitle", "Complete challenges and earn glory!", 16, TextAnchor.MiddleLeft, TextLight, FontStyle.Normal);
        TopLeft(sub.rectTransform, 74, 78, 600, 24);

        var label = Text(c, "OverallLabel", "OVERALL PROGRESS", 15, TextAnchor.MiddleCenter, TextLight, FontStyle.Bold);
        TopLeft(label.rectTransform, 700, 22, 420, 22);

        var track = Panel(c, "ProgressTrack", TrackCol, r => TopLeft(r, 700, 50, 420, 30));
        Outline(track.gameObject, GoldCol);
        var fill = Panel(track.transform, "ProgressFill", GoldCol, r => Stretch(r));
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 0.5f;
        screen.progressFill = fill;

        screen.progressLabel = Text(track.transform, "Percent", "50%", 16, TextAnchor.MiddleCenter, Color.black, FontStyle.Bold);
        Stretch(screen.progressLabel.rectTransform);

        screen.fractionLabel = Text(c, "Fraction", "9 / 18", 20, TextAnchor.MiddleLeft, TextLight, FontStyle.Bold);
        TopLeft(screen.fractionLabel.rectTransform, 1145, 50, 120, 30);
    }

    // ---------------- Tier rows ----------------

    static void BuildTierRow(Transform c, Tier tier, float y)
    {
        int unlocked = 0;
        foreach (var a in tier.Items) if (a.Unlocked) unlocked++;

        // Badge
        var badge = Panel(c, "Badge_" + tier.Name, PanelCol, r => TopLeft(r, 60, y, 140, 250));
        Outline(badge.gameObject, tier.Color);
        var trophy = Panel(badge.transform, "Trophy", tier.Color, r => TopLeft(r, 40, 30, 60, 60));
        Panel(trophy.transform, "TrophyBase", new Color(0f, 0f, 0f, 0.35f), r => TopLeft(r, 15, 44, 30, 12));
        var name = Text(badge.transform, "TierName", tier.Name, 18, TextAnchor.MiddleCenter, tier.Color, FontStyle.Bold);
        TopLeft(name.rectTransform, 0, 115, 140, 26);
        var count = Text(badge.transform, "Count", $"{unlocked} / {tier.Items.Length}", 15, TextAnchor.MiddleCenter, TextLight, FontStyle.Normal);
        TopLeft(count.rectTransform, 0, 148, 140, 22);

        // Horizontal scrollable card row
        var cards = ScrollRow(c, "Row_" + tier.Name, 220, y, 1210, 250, tier.Items.Length * 200f - 10f);
        for (int i = 0; i < tier.Items.Length; i++)
            BuildCard(cards, tier.Items[i], tier.Color, i * 200f);
    }

    static void BuildCard(Transform content, Ach a, Color tierColor, float x)
    {
        var card = Panel(content, "Card_" + a.Name.Replace(" ", ""), a.Unlocked ? CardCol : LockedCard,
            r => TopLeft(r, x, 10, 180, 230));
        Outline(card.gameObject, a.Unlocked ? tierColor : GreyDetail);

        // Icon area (70x70 centered)
        var icon = Panel(card.transform, "Icon", a.Unlocked ? tierColor : GreyIcon, r => TopLeft(r, 55, 25, 70, 70));
        if (a.Unlocked)
        {
            var glyph = Text(icon.transform, "Glyph", a.Icon, 24, TextAnchor.MiddleCenter, BgCol, FontStyle.Bold);
            Stretch(glyph.rectTransform);
        }
        else
        {
            // Padlock from simple rects: shackle (hollow) + body.
            Panel(icon.transform, "LockShackle", GreyDetail, r => TopLeft(r, 23, 10, 24, 20));
            Panel(icon.transform, "LockShackleHole", GreyIcon, r => TopLeft(r, 29, 15, 12, 15));
            Panel(icon.transform, "LockBody", GreyText, r => TopLeft(r, 17, 30, 36, 28));
        }

        var name = Text(card.transform, "Name", a.Name, 15, TextAnchor.MiddleCenter,
            a.Unlocked ? tierColor : GreyText, FontStyle.Bold);
        TopLeft(name.rectTransform, 0, 110, 180, 24);

        var desc = Text(card.transform, "Desc", a.Desc, 12, TextAnchor.UpperCenter,
            a.Unlocked ? TextLight : GreyText, FontStyle.Normal);
        TopLeft(desc.rectTransform, 15, 140, 150, 80);
        desc.horizontalOverflow = HorizontalWrapMode.Wrap;
    }

    // ---------------- Recently Unlocked panel ----------------

    static void BuildRecentPanel(Transform c, AchievementsScreen screen)
    {
        var panel = Panel(c, "RecentlyUnlocked", PanelCol, r => TopLeft(r, 1460, 25, 420, 940));
        Outline(panel.gameObject, GoldCol);

        var title = Text(panel.transform, "Title", "*  RECENTLY UNLOCKED  *", 18, TextAnchor.MiddleCenter, GoldCol, FontStyle.Bold);
        TopLeft(title.rectTransform, 0, 22, 420, 28);

        float y = 75f;
        foreach (var r in Recent)
        {
            var card = Panel(panel.transform, "Recent_" + r.Name.Replace(" ", ""), CardCol,
                rt => TopLeft(rt, 20, y, 380, 150));
            Outline(card.gameObject, GoldCol);

            var icon = Panel(card.transform, "Icon", GoldCol, rt => TopLeft(rt, 18, 43, 64, 64));
            var glyph = Text(icon.transform, "Glyph", r.Icon, 22, TextAnchor.MiddleCenter, BgCol, FontStyle.Bold);
            Stretch(glyph.rectTransform);

            var name = Text(card.transform, "Name", r.Name, 17, TextAnchor.MiddleLeft, GoldCol, FontStyle.Bold);
            TopLeft(name.rectTransform, 100, 25, 260, 24);
            var desc = Text(card.transform, "Desc", r.Desc, 13, TextAnchor.UpperLeft, TextLight, FontStyle.Normal);
            TopLeft(desc.rectTransform, 100, 55, 260, 44);
            desc.horizontalOverflow = HorizontalWrapMode.Wrap;
            var time = Text(card.transform, "Time", "Unlocked: " + r.Time, 12, TextAnchor.MiddleLeft, GreyText, FontStyle.Normal);
            TopLeft(time.rectTransform, 100, 112, 260, 20);

            y += 170f;
        }

        // Display-only for now — no OnClick target until a full list screen exists.
        var viewAll = ButtonRect(panel.transform, "Btn_ViewAll", "VIEW ALL ACHIEVEMENTS  >", CardCol, GoldCol);
        TopLeft((RectTransform)viewAll.transform, 40, 860, 340, 50);
        screen.viewAllButton = viewAll;
    }

    // ---------------- Bottom bar ----------------

    static void BuildBottomBar(Transform c, AchievementsScreen screen)
    {
        var bar = Panel(c, "TipBar", PanelCol, r =>
        {
            r.anchorMin = new Vector2(0, 0);
            r.anchorMax = new Vector2(1, 0);
            r.pivot = new Vector2(0.5f, 0);
            r.offsetMin = new Vector2(0, 0);
            r.offsetMax = new Vector2(0, 0);
            r.sizeDelta = new Vector2(0, 50);
        });

        var tip = Text(bar.transform, "Tip", "Tip: Complete achievements to earn rewards and show off your accomplishments!",
            14, TextAnchor.MiddleLeft, TextLight, FontStyle.Normal);
        Stretch(tip.rectTransform);
        tip.rectTransform.offsetMin = new Vector2(70, 0);
        tip.rectTransform.offsetMax = new Vector2(-200, 0);

        var back = ButtonRect(bar.transform, "Btn_Back", "ESC   Back", CardCol, TextLight);
        var brt = (RectTransform)back.transform;
        brt.anchorMin = new Vector2(1, 0.5f);
        brt.anchorMax = new Vector2(1, 0.5f);
        brt.pivot = new Vector2(1, 0.5f);
        brt.sizeDelta = new Vector2(150, 38);
        brt.anchoredPosition = new Vector2(-30, 0);
        screen.backButton = back;
        UnityEventTools.AddPersistentListener(back.onClick, screen.Close);
    }

    // ---------------- Building blocks ----------------

    static Transform ScrollRow(Transform parent, string name, float x, float y, float w, float h, float contentW)
    {
        var view = new GameObject(name);
        view.transform.SetParent(parent, false);
        var vrt = view.AddComponent<RectTransform>();
        TopLeft(vrt, x, y, w, h);
        var img = view.AddComponent<Image>();          // invisible drag/raycast surface
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
