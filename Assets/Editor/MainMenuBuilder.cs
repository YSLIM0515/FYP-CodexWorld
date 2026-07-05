// Codex World — Main menu builder (Editor tool, draft template)
// Menu: Tools > Codex World > Build Main Menu
// Generates a DRAFT main-menu layout as REAL, editable objects in the open
// MainMenu scene: background, title, the PLAY/CHALLENGES/LEADERBOARD/CODEX/
// SETTINGS/EXIT button column, and a top-right profile card (avatar, name, XP,
// level badge, XP bar). Matches the reference mock-up so it can be shown to a
// supervisor; every piece is a plain UGUI object you can restyle or replace with
// real art afterwards. Safe to re-run (it replaces the generated roots).

using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class MainMenuBuilder
{
    // Palette pulled from the reference image (dark navy circuit-board theme).
    static readonly Color BgColor      = new Color(0.05f, 0.09f, 0.16f, 1f);
    static readonly Color PanelColor   = new Color(0.09f, 0.15f, 0.25f, 0.95f);
    static readonly Color ButtonColor  = new Color(0.10f, 0.20f, 0.33f, 0.95f);
    static readonly Color PlayColor    = new Color(0.85f, 0.68f, 0.20f, 0.98f); // highlighted PLAY
    static readonly Color AccentCyan   = new Color(0.45f, 0.85f, 1f, 1f);
    static readonly Color TextLight    = new Color(0.88f, 0.94f, 1f, 1f);

    [MenuItem("Tools/Codex World/Build Main Menu")]
    public static void Build()
    {
        RemoveRoot("MenuCanvas");
        RemoveRoot("EventSystem");

        var canvasGO = new GameObject("MenuCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();
        var controller = canvasGO.AddComponent<MainMenuController>();
        var root = canvasGO.transform;

        // ---- Background (swap for pixel-art image later) ----
        var background = FullScreenImage(root, "Background", BgColor);
        controller.background = background;

        // ---- Top-left "Ai" logo chip ----
        var logo = Text(root, "Logo_Ai", new Vector2(0, 1), new Vector2(60, -55), new Vector2(120, 80), 46, TextAnchor.MiddleLeft, "Ai");
        logo.color = AccentCyan;
        logo.fontStyle = FontStyle.BoldAndItalic;

        // ---- Title ----
        var title = Text(root, "Title", new Vector2(0.5f, 1f), new Vector2(0, -230), new Vector2(1200, 260), 130, TextAnchor.MiddleCenter, "CODEX\nWORLD");
        title.color = AccentCyan;
        title.fontStyle = FontStyle.Bold;
        title.lineSpacing = 0.85f;

        // ---- Menu button column (centered, below title) ----
        var menu = Panel(root, "MenuColumn", new Vector2(0.5f, 0.5f), new Vector2(0, -80), new Vector2(560, 470), Color.clear).transform;
        controller.playButton        = MenuButton(menu, "Btn_Play",        0, "▶  PLAY",         PlayColor,   Color.black);
        controller.challengesButton  = MenuButton(menu, "Btn_Challenges",  1, ">_  CHALLENGES",     ButtonColor, TextLight);
        controller.leaderboardButton = MenuButton(menu, "Btn_Leaderboard", 2, "▲  LEADERBOARD", ButtonColor, TextLight);
        controller.codexButton       = MenuButton(menu, "Btn_Codex",       3, "▤  CODEX",       ButtonColor, TextLight);
        controller.settingsButton    = MenuButton(menu, "Btn_Settings",    4, "⚙  SETTINGS",    ButtonColor, TextLight);
        controller.exitButton        = MenuButton(menu, "Btn_Exit",        5, "→  EXIT",        ButtonColor, TextLight);

        // Wire the buttons through their OnClick() lists (visible/editable in
        // the Inspector) instead of runtime AddListener calls.
        UnityEventTools.AddPersistentListener(controller.playButton.onClick, controller.OnPlay);
        // CHALLENGES doubles as the achievements opener for now.
        UnityEventTools.AddPersistentListener(controller.challengesButton.onClick, controller.OnAchievements);
        UnityEventTools.AddPersistentListener(controller.leaderboardButton.onClick, controller.OnLeaderboard);
        UnityEventTools.AddPersistentListener(controller.codexButton.onClick, controller.OnCodex);
        UnityEventTools.AddPersistentListener(controller.settingsButton.onClick, controller.OnSettings);
        UnityEventTools.AddPersistentListener(controller.exitButton.onClick, controller.OnExit);

        // ---- Profile card (top-right) ----
        BuildProfileCard(root, controller);

        BuildEventSystem();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = canvasGO;
        Debug.Log("Codex World: draft main menu built. Review the hierarchy, then save the scene (Ctrl+S). " +
                  "Swap the Background/Avatar images and fonts when your assets are ready.");
    }

    // ---------------- Profile card ----------------

    static void BuildProfileCard(Transform root, MainMenuController controller)
    {
        var card = Panel(root, "ProfileCard", new Vector2(1f, 1f), new Vector2(-230, -75), new Vector2(420, 110), PanelColor);
        Outline(card.gameObject, AccentCyan);

        // Avatar placeholder (left square)
        var avatar = Box(card.transform, "Avatar", new Vector2(0f, 0.5f), new Vector2(60, 0), new Vector2(78, 78), new Color(0.18f, 0.30f, 0.45f, 1f));
        controller.avatar = avatar;
        var avatarHint = Text(avatar.transform, "Hint", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(78, 40), 11, TextAnchor.MiddleCenter, "AVATAR");
        avatarHint.color = new Color(1, 1, 1, 0.5f);

        // Name
        controller.playerNameText = Text(card.transform, "PlayerName", new Vector2(0f, 1f), new Vector2(205, -28), new Vector2(240, 30), 22, TextAnchor.MiddleLeft, "CODE_MASTER");
        controller.playerNameText.color = TextLight;
        controller.playerNameText.fontStyle = FontStyle.Bold;

        // XP
        controller.xpText = Text(card.transform, "XP", new Vector2(0f, 1f), new Vector2(205, -55), new Vector2(240, 24), 16, TextAnchor.MiddleLeft, "0 XP");
        controller.xpText.color = AccentCyan;

        // XP bar (track + fill)
        Box(card.transform, "XPBarTrack", new Vector2(0f, 0f), new Vector2(205, 28), new Vector2(230, 12), new Color(0f, 0f, 0f, 0.5f));
        var fill = Box(card.transform, "XPBarFill", new Vector2(0f, 0f), new Vector2(205, 28), new Vector2(230, 12), AccentCyan);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 0.6f;
        controller.xpBarFill = fill;

        // Level badge (right square)
        var badge = Box(card.transform, "LevelBadge", new Vector2(1f, 0.5f), new Vector2(-45, 0), new Vector2(70, 70), new Color(0.12f, 0.22f, 0.36f, 1f));
        Outline(badge.gameObject, AccentCyan);
        controller.levelText = Text(badge.transform, "LevelNumber", new Vector2(0.5f, 0.5f), new Vector2(0, 6), new Vector2(70, 44), 30, TextAnchor.MiddleCenter, "1");
        controller.levelText.color = TextLight;
        controller.levelText.fontStyle = FontStyle.Bold;
        var lvlLabel = Text(badge.transform, "LVL", new Vector2(0.5f, 0f), new Vector2(0, 12), new Vector2(70, 18), 12, TextAnchor.MiddleCenter, "LVL");
        lvlLabel.color = AccentCyan;
    }

    // ---------------- Building blocks ----------------

    static Button MenuButton(Transform column, string name, int index, string label, Color bg, Color textColor)
    {
        const float h = 66f, gap = 12f;
        var go = new GameObject(name);
        go.transform.SetParent(column, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(520, h);
        rt.anchoredPosition = new Vector2(0, -index * (h + gap));
        var img = go.AddComponent<Image>();
        img.color = bg;
        var btn = go.AddComponent<Button>();
        Outline(go, new Color(AccentCyan.r, AccentCyan.g, AccentCyan.b, 0.5f));

        var text = Text(go.transform, "Label", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(480, h), 30, TextAnchor.MiddleLeft, label);
        text.color = textColor;
        text.fontStyle = FontStyle.Bold;
        // Indent label so the leading glyph reads like an icon.
        var trt = text.rectTransform;
        trt.offsetMin = new Vector2(70, trt.offsetMin.y);
        trt.offsetMax = new Vector2(-30, trt.offsetMax.y);
        return btn;
    }

    static Image FullScreenImage(Transform parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    static Image Panel(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    // Alias of Panel; separate name avoids shadowing the UnityEngine.UI.Image type.
    static Image Box(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color color)
        => Panel(parent, name, anchor, pos, size, color);

    static void Outline(GameObject go, Color color)
    {
        var o = go.AddComponent<Outline>();
        o.effectColor = color;
        o.effectDistance = new Vector2(2, -2);
    }

    static Font UIFont() => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

    static Text Text(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, int fontSize, TextAnchor align, string content)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var text = go.AddComponent<Text>();
        text.font = UIFont();
        text.fontSize = fontSize;
        text.alignment = align;
        text.color = Color.white;
        text.text = content;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    static void BuildEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();
    }

    static void RemoveRoot(string name)
    {
        var go = GameObject.Find(name);
        if (go != null) Object.DestroyImmediate(go);
    }
}
