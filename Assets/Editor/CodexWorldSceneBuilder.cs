// Codex World — one-time scene builder (Editor tool)
// Menu: Tools > Codex World > Build Scene Content
// Generates the 9 levels, AXIOM, UI canvas, and materials as REAL objects in
// the open scene, then wires all references on GameController. Run it once,
// save the scene, and edit everything visually afterwards. Safe to re-run:
// it replaces the generated roots (custom edits to them are reset).

using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class CodexWorldSceneBuilder
{
    // ---------------- Level data (build-time only; scene becomes the source of truth) ----------------

    class Row
    {
        public string Name, Region, Story, Hint;
        public int W, H, Theme, Optimal;
        public Vector2Int Start, Goal;
        public Facing Face;
        public Vector2Int[] Obstacles;
        public bool Loops, Conds;
    }

    static readonly Color[,] Themes = // [region, 0=ground 1=groundAlt 2=obstacle 3=background]
    {
        { new Color(0.45f,0.68f,0.38f), new Color(0.40f,0.62f,0.34f), new Color(0.38f,0.10f,0.32f), new Color(0.07f,0.12f,0.09f) },
        { new Color(0.55f,0.50f,0.42f), new Color(0.49f,0.44f,0.37f), new Color(0.42f,0.09f,0.30f), new Color(0.11f,0.09f,0.07f) },
        { new Color(0.24f,0.42f,0.30f), new Color(0.20f,0.37f,0.26f), new Color(0.40f,0.08f,0.34f), new Color(0.04f,0.09f,0.07f) },
        { new Color(0.37f,0.33f,0.52f), new Color(0.32f,0.28f,0.46f), new Color(0.50f,0.06f,0.32f), new Color(0.06f,0.05f,0.12f) },
    };

    static Row[] Rows() => new[]
    {
        new Row { Name="First Steps", Region="Sequence Valley", Theme=0, W=3, H=5, Start=new Vector2Int(1,0), Face=Facing.North, Goal=new Vector2Int(1,3), Obstacles=new Vector2Int[0], Loops=false, Conds=false, Optimal=3,
            Story="The World Compiler awakens AXIOM in Sequence Valley. Reach the data beacon by running commands in order.",
            Hint="Commands execute top to bottom. Three MOVE FORWARD commands reach the beacon." },
        new Row { Name="The Turn", Region="Sequence Valley", Theme=0, W=5, H=4, Start=new Vector2Int(0,0), Face=Facing.North, Goal=new Vector2Int(3,2), Obstacles=new Vector2Int[0], Loops=false, Conds=false, Optimal=6,
            Story="The Null Virus bent this pathway sideways. AXIOM must change direction mid-route.",
            Hint="TURN commands rotate AXIOM but do not move it. Go up 2, turn right, then go 3." },
        new Row { Name="Winding Path", Region="Sequence Valley", Theme=0, W=4, H=5, Start=new Vector2Int(0,0), Face=Facing.North, Goal=new Vector2Int(1,3), Obstacles=new[]{ new Vector2Int(0,2), new Vector2Int(2,1) }, Loops=false, Conds=false, Optimal=6,
            Story="Corrupted blocks bar the direct route. Plan the full sequence before you run it.",
            Hint="Touching a corrupted block ends the run. Weave around them: up, right, then up again." },
        new Row { Name="Conveyor Run", Region="Loop Factory", Theme=1, W=9, H=3, Start=new Vector2Int(0,1), Face=Facing.East, Goal=new Vector2Int(8,1), Obstacles=new[]{ new Vector2Int(1,0), new Vector2Int(3,2), new Vector2Int(5,0), new Vector2Int(7,2) }, Loops=true, Conds=false, Optimal=4,
            Story="Welcome to Loop Factory. The assembly line is long — repeating yourself is inefficient. Loops repeat commands for you.",
            Hint="LOOP x4 containing MOVE, MOVE performs 8 steps with only 4 blocks." },
        new Row { Name="Gear Steps", Region="Loop Factory", Theme=1, W=5, H=5, Start=new Vector2Int(0,0), Face=Facing.North, Goal=new Vector2Int(3,3), Obstacles=new[]{ new Vector2Int(0,2), new Vector2Int(1,3), new Vector2Int(2,4) }, Loops=true, Conds=false, Optimal=6,
            Story="Broken gears form a staircase. Spot the repeating pattern in the climb.",
            Hint="One stair = MOVE, TURN RIGHT, MOVE, TURN LEFT. Loop that pattern 3 times." },
        new Row { Name="The Perimeter", Region="Loop Factory", Theme=1, W=4, H=4, Start=new Vector2Int(0,0), Face=Facing.North, Goal=new Vector2Int(3,3), Obstacles=new[]{ new Vector2Int(1,1), new Vector2Int(1,2), new Vector2Int(2,1), new Vector2Int(2,2) }, Loops=true, Conds=false, Optimal=6,
            Story="The factory core is walled off by corruption. AXIOM must walk the outer rim.",
            Hint="One side of the rim = MOVE x3 then TURN RIGHT. Two loops of that reach the far corner." },
        new Row { Name="Fork in the Woods", Region="Logic Forest", Theme=2, W=4, H=4, Start=new Vector2Int(0,0), Face=Facing.North, Goal=new Vector2Int(1,3), Obstacles=new[]{ new Vector2Int(0,1), new Vector2Int(2,1) }, Loops=true, Conds=true, Optimal=8,
            Story="The forest shifts and blocks paths without warning. AXIOM must check before it moves.",
            Hint="IF OBSTACLE runs its block only when something blocks the tile ahead. Check, side-step right, then head north." },
        new Row { Name="Shifting Maze", Region="Logic Forest", Theme=2, W=5, H=3, Start=new Vector2Int(0,0), Face=Facing.North, Goal=new Vector2Int(3,0), Obstacles=new[]{ new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(4,2) }, Loops=true, Conds=true, Optimal=6,
            Story="Deep in the maze, the same routine must survive changing surroundings. Combine LOOP with IF.",
            Hint="Loop 3 times: IF OBSTACLE turn right, then MOVE. The check only fires when needed." },
        new Row { Name="Null Virus Core", Region="Algorithm Tower", Theme=3, W=9, H=3, Start=new Vector2Int(0,1), Face=Facing.East, Goal=new Vector2Int(8,1), Obstacles=new[]{ new Vector2Int(2,1), new Vector2Int(5,1), new Vector2Int(0,0), new Vector2Int(8,0) }, Loops=true, Conds=true, Optimal=13,
            Story="The final chamber. The Null Virus drops corrupted blocks along the corridor. Write one routine that survives the whole run.",
            Hint="Loop 4 times: IF OBSTACLE do a dodge (TL, M, TR, M, M, TR, M, TL), then MOVE. The dodge climbs over a block and returns." },
    };

    // ---------------- Menu entry ----------------

    [MenuItem("Tools/Codex World/Build Scene Content")]
    public static void Build()
    {
        RemoveRoot("Levels"); RemoveRoot("AXIOM"); RemoveRoot("GameCanvas");
        RemoveRoot("EventSystem"); RemoveRoot("GameController"); RemoveRoot("GameBootstrap"); RemoveRoot("World");

        var goalMat = Mat("Goal", new Color(0.15f, 0.9f, 0.45f), 0.6f, new Color(0.05f, 0.7f, 0.3f));
        var bodyMat = Mat("AxiomBody", new Color(0.25f, 0.55f, 0.95f), 0.55f, new Color(0.25f, 0.55f, 0.95f) * 0.4f);
        var noseMat = Mat("AxiomNose", new Color(0.95f, 0.9f, 0.3f), 0.5f, new Color(0.95f, 0.9f, 0.3f) * 0.5f);
        var spawnMat = Mat("SpawnPad", new Color(0.3f, 0.9f, 0.9f), 0.5f, new Color(0.1f, 0.4f, 0.4f));

        var rows = Rows();
        var levelsRoot = new GameObject("Levels");
        var defs = new LevelDefinition[rows.Length];
        for (int i = 0; i < rows.Length; i++)
        {
            defs[i] = BuildLevel(rows[i], i, levelsRoot.transform, goalMat, spawnMat);
            defs[i].gameObject.SetActive(i == 0);
        }

        var avatar = BuildAvatar(bodyMat, noseMat, rows[0].Start);
        var camCtl = SetupCamera();
        var ui = BuildUI();
        BuildEventSystem();

        var bootGO = new GameObject("GameController");
        var boot = bootGO.AddComponent<GameController>();
        boot.levels = defs;
        boot.avatar = avatar;
        boot.cameraController = camCtl;
        boot.ui = ui;

        WireAll(ui, boot);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = bootGO;
        Debug.Log("Codex World: scene content built. Review the hierarchy, then save the scene (Ctrl+S).");
    }

    static void RemoveRoot(string name)
    {
        var go = GameObject.Find(name);
        if (go != null) Object.DestroyImmediate(go);
    }

    // ---------------- Button OnClick wiring (Inspector-visible) ----------------

    // Non-destructive: rewires the OnClick() lists of the buttons already in the
    // open scene without rebuilding levels/UI. Use after script changes.
    [MenuItem("Tools/Codex World/Wire Game Buttons (OnClick)")]
    public static void WireButtons()
    {
        var ui = Object.FindAnyObjectByType<GameUI>();
        var boot = Object.FindAnyObjectByType<GameController>();
        if (ui == null || boot == null)
        {
            Debug.LogError("Codex World: open the CodexWorld scene (with GameCanvas + GameController) first.");
            return;
        }
        WireAll(ui, boot);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Codex World: game buttons wired via OnClick(). Save the scene (Ctrl+S).");
    }

    static void WireAll(GameUI ui, GameController boot)
    {
        Wire(ui.moveForwardButton, boot.AddMoveForward);
        Wire(ui.turnLeftButton, boot.AddTurnLeft);
        Wire(ui.turnRightButton, boot.AddTurnRight);
        WireInt(ui.loop2Button, boot.AddLoop, 2);
        WireInt(ui.loop3Button, boot.AddLoop, 3);
        WireInt(ui.loop4Button, boot.AddLoop, 4);
        Wire(ui.endBlockButton, boot.EndBlock);
        Wire(ui.ifObstacleButton, boot.AddIfObstacle);
        Wire(ui.ifClearButton, boot.AddIfClear);
        Wire(ui.runButton, boot.Run);
        Wire(ui.undoButton, boot.UndoToken);
        Wire(ui.clearButton, boot.ClearQueue);
        Wire(ui.prevButton, boot.GoPrev);
        Wire(ui.nextButton, boot.GoNext);
        Wire(ui.nextLevelButton, boot.GoNext);
        Wire(ui.resetSaveButton, boot.ResetSave);
    }

    static void Wire(Button button, UnityEngine.Events.UnityAction handler)
    {
        if (button == null) return;
        ClearOnClick(button);
        UnityEventTools.AddPersistentListener(button.onClick, handler);
        EditorUtility.SetDirty(button);
    }

    static void WireInt(Button button, UnityEngine.Events.UnityAction<int> handler, int arg)
    {
        if (button == null) return;
        ClearOnClick(button);
        UnityEventTools.AddIntPersistentListener(button.onClick, handler, arg);
        EditorUtility.SetDirty(button);
    }

    static void ClearOnClick(Button button)
    {
        while (button.onClick.GetPersistentEventCount() > 0)
            UnityEventTools.RemovePersistentListener(button.onClick, 0);
    }

    // ---------------- Redesigned in-game HUD (per concept mock-up) ----------------
    // Top bar (level | map, XP, objective + star conditions, settings gear),
    // right command palette + queue panel, and win/fail result overlays.
    // Rebuilds GameCanvas only — levels/AXIOM/camera are untouched.

    static readonly Color HudBar    = new Color(0.02f, 0.07f, 0.09f, 0.97f);
    static readonly Color HudPanelC = new Color(0.03f, 0.10f, 0.12f, 0.94f);
    static readonly Color HudBtnC   = new Color(0.05f, 0.16f, 0.19f, 1f);
    static readonly Color HudCyan   = new Color(0.45f, 0.85f, 1f, 1f);
    static readonly Color HudLight  = new Color(0.88f, 0.94f, 1f, 1f);
    static readonly Color HudGold   = new Color(1f, 0.8f, 0.2f, 1f);
    static readonly Color RunGreen  = new Color(0.13f, 0.45f, 0.22f, 1f);
    static readonly Color ClearRed  = new Color(0.55f, 0.13f, 0.13f, 1f);
    static readonly Color WinCol    = new Color(0.02f, 0.11f, 0.12f, 0.98f);
    static readonly Color FailCol   = new Color(0.22f, 0.05f, 0.07f, 0.98f);
    static readonly Color ReturnBlue= new Color(0.10f, 0.20f, 0.38f, 1f);

    [MenuItem("Tools/Codex World/Build Game HUD (Redesign)")]
    public static void BuildGameHUD()
    {
        var boot = Object.FindAnyObjectByType<GameController>();
        if (boot == null)
        {
            Debug.LogError("Codex World: open the CodexWorld scene (with GameController) first.");
            return;
        }
        RemoveRoot("GameCanvas");

        var canvasGO = new GameObject("GameCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600, 900);
        canvasGO.AddComponent<GraphicRaycaster>();
        var ui = canvasGO.AddComponent<GameUI>();
        var root = canvasGO.transform;

        // ---- Top bar ----
        var bar = HudPanel(root, "TopBar", HudBar);
        var brt = bar.rectTransform;
        brt.anchorMin = new Vector2(0, 1); brt.anchorMax = new Vector2(1, 1);
        brt.pivot = new Vector2(0.5f, 1); brt.offsetMin = new Vector2(0, -46); brt.offsetMax = Vector2.zero;

        ui.title = HudText(bar.transform, "LevelTitle", 18, TextAnchor.MiddleLeft, HudLight, FontStyle.Bold);
        TL(ui.title.rectTransform, 16, 0, 430, 46);
        ui.xpText = HudText(bar.transform, "XP", 17, TextAnchor.MiddleLeft, HudCyan, FontStyle.Bold);
        TL(ui.xpText.rectTransform, 470, 0, 180, 46);
        ui.objectiveText = HudText(bar.transform, "Objective", 15, TextAnchor.MiddleLeft, HudGold, FontStyle.Bold);
        TL(ui.objectiveText.rectTransform, 680, 2, 560, 24);
        ui.starConditionText = HudText(bar.transform, "StarConditions", 13, TextAnchor.MiddleLeft, new Color(0.78f, 0.84f, 0.92f), FontStyle.Normal);
        TL(ui.starConditionText.rectTransform, 680, 25, 700, 20);
        ui.settingsButton = HudBtn(bar.transform, "Btn_Settings", "⚙", HudBtnC, HudLight, 20);
        var srt = (RectTransform)ui.settingsButton.transform;
        srt.anchorMin = srt.anchorMax = new Vector2(1, 0.5f); srt.pivot = new Vector2(1, 0.5f);
        srt.sizeDelta = new Vector2(42, 34); srt.anchoredPosition = new Vector2(-10, 0);

        // ---- Level nav + story (top-left, under bar) ----
        ui.prevButton = HudBtn(root, "Btn_Prev", "<", HudBtnC, HudLight, 15);
        TL((RectTransform)ui.prevButton.transform, 16, 56, 36, 28);
        ui.levelLabel = HudText(root, "LevelLabel", 14, TextAnchor.MiddleCenter, HudLight, FontStyle.Normal);
        TL(ui.levelLabel.rectTransform, 56, 56, 130, 28);
        ui.nextButton = HudBtn(root, "Btn_Next", ">", HudBtnC, HudLight, 15);
        TL((RectTransform)ui.nextButton.transform, 190, 56, 36, 28);
        ui.resetSaveButton = HudBtn(root, "Btn_ResetSave", "Reset Save", HudBtnC, HudLight, 11);
        TL((RectTransform)ui.resetSaveButton.transform, 236, 58, 84, 24);
        ui.story = HudText(root, "Story", 15, TextAnchor.UpperLeft, new Color(0.85f, 0.89f, 0.96f), FontStyle.Normal);
        TL(ui.story.rectTransform, 18, 92, 640, 64);
        ui.story.horizontalOverflow = HorizontalWrapMode.Wrap;

        // ---- Right command panel ----
        var panel = HudPanel(root, "CommandPanel", HudPanelC);
        var prt = panel.rectTransform;
        prt.anchorMin = prt.anchorMax = new Vector2(1, 1); prt.pivot = new Vector2(1, 1);
        prt.sizeDelta = new Vector2(400, 620); prt.anchoredPosition = new Vector2(-10, -56);
        var o = panel.gameObject.AddComponent<Outline>();
        o.effectColor = new Color(HudCyan.r, HudCyan.g, HudCyan.b, 0.5f); o.effectDistance = new Vector2(2, -2);
        var p = panel.transform;

        var palHdr = HudText(p, "PaletteHeader", 15, TextAnchor.MiddleLeft, HudCyan, FontStyle.Bold);
        palHdr.text = "COMMAND PALETTE";
        TL(palHdr.rectTransform, 16, 10, 300, 24);

        // grid of command buttons (3 columns, 118x54, 8px gaps)
        System.Func<Transform, string, string, int, int, Button> pal = (parent, nm, label, col, rowY) =>
        {
            var b = HudBtn(parent, nm, label, HudBtnC, HudLight, 14);
            TL((RectTransform)b.transform, 14 + col * 126, rowY, 118, 54);
            return b;
        };
        ui.moveForwardButton = pal(p, "Btn_MoveForward", "MOVE", 0, 40);
        ui.turnLeftButton = pal(p, "Btn_TurnLeft", "TURN LEFT", 1, 40);
        ui.turnRightButton = pal(p, "Btn_TurnRight", "TURN RIGHT", 2, 40);

        var loopRow = new GameObject("LoopRow");
        loopRow.transform.SetParent(p, false);
        TL(loopRow.AddComponent<RectTransform>(), 0, 0, 400, 620);
        ui.loopRow = loopRow;
        ui.loop2Button = pal(loopRow.transform, "Btn_Loop2", "LOOP x2", 0, 102);
        ui.loop3Button = pal(loopRow.transform, "Btn_Loop3", "LOOP x3", 1, 102);
        ui.loop4Button = pal(loopRow.transform, "Btn_Loop4", "LOOP x4", 2, 102);
        ui.endBlockButton = pal(loopRow.transform, "Btn_EndBlock", "END BLOCK", 0, 164);

        var condRow = new GameObject("ConditionalRow");
        condRow.transform.SetParent(p, false);
        TL(condRow.AddComponent<RectTransform>(), 0, 0, 400, 620);
        ui.conditionalRow = condRow;
        ui.ifObstacleButton = pal(condRow.transform, "Btn_IfObstacle", "IF OBSTACLE", 1, 164);
        ui.ifClearButton = pal(condRow.transform, "Btn_IfClear", "IF CLEAR", 2, 164);

        var queueHdr = HudText(p, "QueueHeader", 15, TextAnchor.MiddleLeft, HudCyan, FontStyle.Bold);
        queueHdr.text = "COMMAND QUEUE";
        TL(queueHdr.rectTransform, 16, 232, 300, 24);
        var queueBg = HudPanel(p, "QueueBg", new Color(0f, 0f, 0f, 0.4f));
        TL(queueBg.rectTransform, 14, 260, 372, 220);
        ui.queueText = HudText(queueBg.transform, "Queue", 13, TextAnchor.UpperLeft, new Color(0.75f, 0.95f, 0.75f), FontStyle.Normal);
        var qrt = ui.queueText.rectTransform;
        qrt.anchorMin = Vector2.zero; qrt.anchorMax = Vector2.one;
        qrt.offsetMin = new Vector2(10, 8); qrt.offsetMax = new Vector2(-10, -8);
        ui.queueText.horizontalOverflow = HorizontalWrapMode.Wrap;
        ui.queueText.verticalOverflow = VerticalWrapMode.Truncate;

        ui.runButton = HudBtn(p, "Btn_Run", "RUN  ▶", RunGreen, HudLight, 17);
        TL((RectTransform)ui.runButton.transform, 14, 498, 140, 46);
        ui.undoButton = HudBtn(p, "Btn_Undo", "UNDO", HudBtnC, HudLight, 14);
        TL((RectTransform)ui.undoButton.transform, 166, 498, 100, 46);
        ui.clearButton = HudBtn(p, "Btn_Clear", "CLEAR", ClearRed, HudLight, 14);
        TL((RectTransform)ui.clearButton.transform, 278, 498, 108, 46);

        // ---- Status line (bottom center) ----
        ui.status = HudText(root, "Status", 15, TextAnchor.MiddleCenter, HudGold, FontStyle.Normal);
        var strt = ui.status.rectTransform;
        strt.anchorMin = strt.anchorMax = new Vector2(0.5f, 0f); strt.pivot = new Vector2(0.5f, 0f);
        strt.sizeDelta = new Vector2(1000, 28); strt.anchoredPosition = new Vector2(-100, 14);

        // ---- Win panel ----
        var win = HudPanel(root, "WinPanel", WinCol);
        C((RectTransform)win.transform, 480, 400);
        var wo = win.gameObject.AddComponent<Outline>(); wo.effectColor = HudGold; wo.effectDistance = new Vector2(2, -2);
        ui.winPanel = win.gameObject;
        var winTitle = HudText(win.transform, "Title", 28, TextAnchor.MiddleCenter, HudGold, FontStyle.Bold);
        winTitle.text = "LEVEL COMPLETE!";
        TL(winTitle.rectTransform, 0, 20, 480, 40);
        ui.winStars = new Text[3];
        for (int s = 0; s < 3; s++)
        {
            ui.winStars[s] = HudText(win.transform, "Star" + (s + 1), 54, TextAnchor.MiddleCenter, HudGold, FontStyle.Bold);
            ui.winStars[s].text = "★";
            TL(ui.winStars[s].rectTransform, 140 + s * 70 - 35, 66, 70, 70);
        }
        ui.winMessage = HudText(win.transform, "Message", 15, TextAnchor.MiddleCenter, HudLight, FontStyle.Normal);
        TL(ui.winMessage.rectTransform, 30, 160, 420, 50);
        ui.winMessage.horizontalOverflow = HorizontalWrapMode.Wrap;
        ui.winXPText = HudText(win.transform, "XPAwarded", 20, TextAnchor.MiddleCenter, HudCyan, FontStyle.Bold);
        TL(ui.winXPText.rectTransform, 0, 226, 480, 34);
        ui.nextLevelButton = HudBtn(win.transform, "Btn_NextLevel", "NEXT LEVEL  ▶", RunGreen, HudLight, 16);
        var nrt = (RectTransform)ui.nextLevelButton.transform;
        nrt.anchorMin = nrt.anchorMax = new Vector2(0.5f, 0f); nrt.pivot = new Vector2(0.5f, 0f);
        nrt.sizeDelta = new Vector2(220, 48); nrt.anchoredPosition = new Vector2(0, 26);

        // ---- Fail panel ----
        var fail = HudPanel(root, "FailPanel", FailCol);
        C((RectTransform)fail.transform, 480, 360);
        var fo = fail.gameObject.AddComponent<Outline>(); fo.effectColor = new Color(1f, 0.4f, 0.35f); fo.effectDistance = new Vector2(2, -2);
        ui.failPanel = fail.gameObject;
        var failTitle = HudText(fail.transform, "Title", 28, TextAnchor.MiddleCenter, new Color(1f, 0.5f, 0.42f), FontStyle.Bold);
        failTitle.text = "TRY AGAIN!";
        TL(failTitle.rectTransform, 0, 20, 480, 40);
        ui.failReason = HudText(fail.transform, "Reason", 15, TextAnchor.MiddleCenter, HudLight, FontStyle.Bold);
        TL(ui.failReason.rectTransform, 30, 84, 420, 70);
        ui.failReason.horizontalOverflow = HorizontalWrapMode.Wrap;
        ui.failTip = HudText(fail.transform, "Tip", 13, TextAnchor.MiddleCenter, new Color(0.85f, 0.75f, 0.55f), FontStyle.Normal);
        TL(ui.failTip.rectTransform, 30, 170, 420, 60);
        ui.failTip.horizontalOverflow = HorizontalWrapMode.Wrap;
        ui.retryButton = HudBtn(fail.transform, "Btn_Retry", "RETRY", ClearRed, HudLight, 15);
        var rrt = (RectTransform)ui.retryButton.transform;
        rrt.anchorMin = rrt.anchorMax = new Vector2(0.5f, 0f); rrt.pivot = new Vector2(0.5f, 0f);
        rrt.sizeDelta = new Vector2(180, 46); rrt.anchoredPosition = new Vector2(-100, 24);
        ui.returnButton = HudBtn(fail.transform, "Btn_Return", "MAIN MENU", ReturnBlue, HudLight, 15);
        var rert = (RectTransform)ui.returnButton.transform;
        rert.anchorMin = rert.anchorMax = new Vector2(0.5f, 0f); rert.pivot = new Vector2(0.5f, 0f);
        rert.sizeDelta = new Vector2(180, 46); rert.anchoredPosition = new Vector2(100, 24);

        win.gameObject.SetActive(false);
        fail.gameObject.SetActive(false);

        // ---- Wire everything ----
        boot.ui = ui;
        WireAll(ui, boot);
        Wire(ui.retryButton, boot.Retry);
        Wire(ui.returnButton, boot.ReturnToMenu);

        // Settings overlay lives on this canvas; gear button opens it.
        SettingsPanelBuilder.Build();
        var gear = GameObject.Find("Btn_OpenSettings");
        if (gear != null) Object.DestroyImmediate(gear);
        var settings = Object.FindAnyObjectByType<SettingsPanel>(FindObjectsInactive.Include);
        if (settings != null)
        {
            settings.openButton = ui.settingsButton;
            Wire(ui.settingsButton, settings.Open);
        }

        BuildEventSystem();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = canvasGO;
        Debug.Log("Codex World: redesigned game HUD built and wired. Save the scene (Ctrl+S).");
    }

    // ---------------- Pause menu (ESC) ----------------
    // Dim overlay + button stack: Resume / Restart Level / Settings / Return to
    // Main Menu. Also removes the top-bar settings gear — settings now lives here.

    [MenuItem("Tools/Codex World/Build Pause Menu")]
    public static void BuildPauseMenu()
    {
        var boot = Object.FindAnyObjectByType<GameController>();
        var canvasGO = GameObject.Find("GameCanvas");
        if (boot == null || canvasGO == null)
        {
            Debug.LogError("Codex World: open the CodexWorld scene and run Build Game HUD first.");
            return;
        }
        var canvas = canvasGO.transform;
        var old = canvas.Find("PauseMenu");
        if (old != null) Object.DestroyImmediate(old.gameObject);

        var rootGO = new GameObject("PauseMenu");
        rootGO.transform.SetParent(canvas, false);
        var rootRt = rootGO.AddComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero; rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero; rootRt.offsetMax = Vector2.zero;
        var pm = rootGO.AddComponent<PauseMenu>();
        pm.controller = boot;

        var content = new GameObject("Content");
        content.transform.SetParent(rootGO.transform, false);
        var crt = content.AddComponent<RectTransform>();
        crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
        crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;
        pm.panelRoot = content;

        var dim = HudPanel(content.transform, "Dimmer", new Color(0f, 0f, 0f, 0.7f));
        var drt = dim.rectTransform;
        drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
        drt.offsetMin = Vector2.zero; drt.offsetMax = Vector2.zero;

        var window = HudPanel(content.transform, "Window", new Color(0.045f, 0.06f, 0.075f, 0.98f));
        C(window.rectTransform, 380, 440);
        var wo = window.gameObject.AddComponent<Outline>();
        wo.effectColor = new Color(HudCyan.r, HudCyan.g, HudCyan.b, 0.6f);
        wo.effectDistance = new Vector2(2, -2);

        var labels = new[] { "Resume", "Restart Level", "Settings", "Return to Main Menu" };
        var btns = new Button[labels.Length];
        for (int i = 0; i < labels.Length; i++)
        {
            btns[i] = HudBtn(window.transform, "Btn_" + labels[i].Replace(" ", ""), labels[i],
                new Color(0.07f, 0.09f, 0.11f, 1f), HudLight, 19);
            var bo = btns[i].gameObject.AddComponent<Outline>();
            bo.effectColor = new Color(0.3f, 0.85f, 0.7f, 0.8f); // teal edge like the mock-up
            bo.effectDistance = new Vector2(2, -2);
            TL((RectTransform)btns[i].transform, 30, 28 + i * 98, 320, 76);
        }
        pm.resumeButton = btns[0];
        pm.restartButton = btns[1];
        pm.settingsButton = btns[2];
        pm.returnButton = btns[3];

        Wire(pm.resumeButton, pm.Resume);
        Wire(pm.restartButton, pm.RestartLevel);
        Wire(pm.settingsButton, pm.OpenSettings);
        Wire(pm.returnButton, pm.ReturnToMenu);

        // Settings moves into the pause menu: drop the top-bar gear.
        var settings = Object.FindAnyObjectByType<SettingsPanel>(FindObjectsInactive.Include);
        if (settings != null)
        {
            pm.settingsPanel = settings;
            settings.openButton = null;
        }
        if (boot.ui != null && boot.ui.settingsButton != null)
        {
            Object.DestroyImmediate(boot.ui.settingsButton.gameObject);
            boot.ui.settingsButton = null;
        }

        content.SetActive(false); // hidden until ESC; PauseMenu.Awake also hides it
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = rootGO;
        Debug.Log("Codex World: pause menu built (ESC to open). Save the scene (Ctrl+S).");
    }

    // HUD building blocks
    static Image HudPanel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    static Text HudText(Transform parent, string name, int size, TextAnchor align, Color color, FontStyle style)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var t = go.AddComponent<Text>();
        t.font = UIFont();
        t.fontSize = size;
        t.alignment = align;
        t.color = color;
        t.fontStyle = style;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    static Button HudBtn(Transform parent, string name, string label, Color bg, Color textColor, int fontSize)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var img = go.AddComponent<Image>();
        img.color = bg;
        var btn = go.AddComponent<Button>();
        var t = HudText(go.transform, "Label", fontSize, TextAnchor.MiddleCenter, textColor, FontStyle.Bold);
        t.text = label;
        var trt = t.rectTransform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
        return btn;
    }

    // anchor top-left of parent, x/y measured from the top-left corner
    static void TL(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(x, -y);
    }

    static void C(RectTransform rt, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(0, 0);
    }

    // ---------------- Materials (saved as editable assets) ----------------

    static Material Mat(string name, Color color, float smoothness = 0.35f, Color? emission = null)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder("Assets/Materials/CodexWorld"))
            AssetDatabase.CreateFolder("Assets/Materials", "CodexWorld");

        string path = $"Assets/Materials/CodexWorld/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Smoothness", smoothness);
        if (emission.HasValue)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emission.Value);
        }
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // ---------------- World objects ----------------

    static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    static LevelDefinition BuildLevel(Row row, int index, Transform parent, Material goalMat, Material spawnMat)
    {
        var groundMat = Mat($"R{row.Theme}_Ground", Themes[row.Theme, 0]);
        var groundAlt = Mat($"R{row.Theme}_GroundAlt", Themes[row.Theme, 1]);
        var obstacleMat = Mat($"R{row.Theme}_Corrupted", Themes[row.Theme, 2], 0.2f, Themes[row.Theme, 2] * 0.6f);

        var root = new GameObject($"Level{index + 1}_{row.Name.Replace(" ", "")}");
        root.transform.SetParent(parent);
        var def = root.AddComponent<LevelDefinition>();
        def.levelName = row.Name;
        def.regionTitle = row.Region;
        def.story = row.Story;
        def.hint = row.Hint;
        def.allowLoops = row.Loops;
        def.allowConditionals = row.Conds;
        def.optimalTokens = row.Optimal;
        def.cameraBackground = Themes[row.Theme, 3];

        var tiles = new GameObject("Tiles").transform;
        tiles.SetParent(root.transform);
        for (int x = 0; x < row.W; x++)
            for (int y = 0; y < row.H; y++)
                Primitive(PrimitiveType.Cube, $"Ground_{x}_{y}", tiles,
                    new Vector3(x, -0.1f, y), new Vector3(0.96f, 0.2f, 0.96f),
                    (x + y) % 2 == 0 ? groundMat : groundAlt);

        var obstacles = new GameObject("Obstacles").transform;
        obstacles.SetParent(root.transform);
        foreach (var o in row.Obstacles)
        {
            var cube = Primitive(PrimitiveType.Cube, $"Corrupted_{o.x}_{o.y}", obstacles,
                new Vector3(o.x, 0.4f, o.y), new Vector3(0.85f, 0.8f, 0.85f), obstacleMat);
            cube.AddComponent<CellMarker>().type = CellType.Obstacle;
        }

        var goal = new GameObject("Goal");
        goal.transform.SetParent(root.transform);
        goal.transform.position = new Vector3(row.Goal.x, 0f, row.Goal.y);
        goal.AddComponent<CellMarker>().type = CellType.Goal;
        Primitive(PrimitiveType.Cylinder, "GoalPad", goal.transform,
            new Vector3(row.Goal.x, 0.03f, row.Goal.y), new Vector3(0.8f, 0.03f, 0.8f), goalMat);
        Primitive(PrimitiveType.Cylinder, "GoalBeam", goal.transform,
            new Vector3(row.Goal.x, 0.9f, row.Goal.y), new Vector3(0.12f, 0.9f, 0.12f), goalMat);

        var spawn = Primitive(PrimitiveType.Cube, "Spawn", root.transform,
            new Vector3(row.Start.x, 0.01f, row.Start.y), new Vector3(0.4f, 0.04f, 0.4f), spawnMat);
        var marker = spawn.AddComponent<CellMarker>();
        marker.type = CellType.Spawn;
        marker.spawnFacing = row.Face;

        return def;
    }

    static PlayerAvatar BuildAvatar(Material bodyMat, Material noseMat, Vector2Int startCell)
    {
        var root = new GameObject("AXIOM");
        root.transform.position = new Vector3(startCell.x, 0f, startCell.y);
        var avatar = root.AddComponent<PlayerAvatar>();

        var body = Primitive(PrimitiveType.Capsule, "Body", root.transform,
            root.transform.position + new Vector3(0f, 0.42f, 0f), new Vector3(0.45f, 0.42f, 0.45f), bodyMat);
        var nose = Primitive(PrimitiveType.Cube, "Nose", root.transform,
            root.transform.position + new Vector3(0f, 0.55f, 0.28f), new Vector3(0.14f, 0.14f, 0.2f), noseMat);

        avatar.bodyRenderer = body.GetComponent<Renderer>();
        return avatar;
    }

    static GodViewCamera SetupCamera()
    {
        var cam = Camera.main;
        if (cam == null)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            cam = go.AddComponent<Camera>();
        }
        var ctl = cam.GetComponent<GodViewCamera>();
        if (ctl == null) ctl = cam.gameObject.AddComponent<GodViewCamera>();
        return ctl;
    }

    static void BuildEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();
    }

    // ---------------- UI ----------------

    static readonly Color ButtonColor = new Color(0.16f, 0.17f, 0.22f, 0.95f);

    static GameUI BuildUI()
    {
        var canvasGO = new GameObject("GameCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600, 900);
        canvasGO.AddComponent<GraphicRaycaster>();
        var ui = canvasGO.AddComponent<GameUI>();
        var t = canvasGO.transform;

        // Header
        ui.title = Text(t, "Title", new Vector2(0.5f, 1f), new Vector2(0, -30), new Vector2(1100, 34), 26, TextAnchor.MiddleCenter);
        ui.title.fontStyle = FontStyle.Bold;
        ui.story = Text(t, "Story", new Vector2(0.5f, 1f), new Vector2(0, -62), new Vector2(1100, 44), 15, TextAnchor.UpperCenter);
        ui.story.color = new Color(0.8f, 0.82f, 0.9f);
        ui.status = Text(t, "Status", new Vector2(0.5f, 1f), new Vector2(0, -108), new Vector2(1100, 30), 18, TextAnchor.MiddleCenter);
        ui.status.color = new Color(1f, 0.9f, 0.4f);

        // Level navigation (top left)
        ui.prevButton = Btn(t, "PrevLevel", new Vector2(0f, 1f), new Vector2(45, -32), new Vector2(50, 36), "<");
        ui.levelLabel = Text(t, "LevelLabel", new Vector2(0f, 1f), new Vector2(160, -32), new Vector2(170, 36), 16, TextAnchor.MiddleCenter);
        ui.nextButton = Btn(t, "NextLevel", new Vector2(0f, 1f), new Vector2(275, -32), new Vector2(50, 36), ">");

        // XP + reset save (top right)
        ui.xpText = Text(t, "XP", new Vector2(1f, 1f), new Vector2(-90, -30), new Vector2(160, 30), 18, TextAnchor.MiddleRight);
        ui.xpText.color = new Color(0.55f, 0.9f, 1f);
        ui.resetSaveButton = Btn(t, "ResetSave", new Vector2(1f, 1f), new Vector2(-90, -64), new Vector2(110, 26), "Reset Save");
        ui.resetSaveButton.GetComponentInChildren<Text>().fontSize = 12;

        // Queue readout
        ui.queueText = Text(t, "Queue", new Vector2(0.5f, 0f), new Vector2(0, 252), new Vector2(1300, 30), 16, TextAnchor.MiddleCenter);
        ui.queueText.color = new Color(0.75f, 0.95f, 0.75f);

        // Basic commands row
        var basicRow = RowGroup(t, "BasicRow", 195);
        ui.moveForwardButton = Btn(basicRow, "MoveForward", new Vector2(0.5f, 0.5f), new Vector2(-160, 0), new Vector2(150, 38), "Move Forward");
        ui.turnLeftButton = Btn(basicRow, "TurnLeft", new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(150, 38), "Turn Left");
        ui.turnRightButton = Btn(basicRow, "TurnRight", new Vector2(0.5f, 0.5f), new Vector2(160, 0), new Vector2(150, 38), "Turn Right");

        // Loop row
        var loopRow = RowGroup(t, "LoopRow", 148);
        ui.loopRow = loopRow.gameObject;
        ui.loop2Button = Btn(loopRow, "Loop2", new Vector2(0.5f, 0.5f), new Vector2(-240, 0), new Vector2(150, 38), "Loop x2");
        ui.loop3Button = Btn(loopRow, "Loop3", new Vector2(0.5f, 0.5f), new Vector2(-80, 0), new Vector2(150, 38), "Loop x3");
        ui.loop4Button = Btn(loopRow, "Loop4", new Vector2(0.5f, 0.5f), new Vector2(80, 0), new Vector2(150, 38), "Loop x4");
        ui.endBlockButton = Btn(loopRow, "EndBlock", new Vector2(0.5f, 0.5f), new Vector2(240, 0), new Vector2(150, 38), "End Block");

        // Conditional row
        var condRow = RowGroup(t, "ConditionalRow", 101);
        ui.conditionalRow = condRow.gameObject;
        ui.ifObstacleButton = Btn(condRow, "IfObstacle", new Vector2(0.5f, 0.5f), new Vector2(-80, 0), new Vector2(150, 38), "If Obstacle");
        ui.ifClearButton = Btn(condRow, "IfClear", new Vector2(0.5f, 0.5f), new Vector2(80, 0), new Vector2(150, 38), "If Clear");

        // Control row
        var controlRow = RowGroup(t, "ControlRow", 40);
        ui.runButton = Btn(controlRow, "Run", new Vector2(0.5f, 0.5f), new Vector2(-250, 0), new Vector2(150, 44), "RUN");
        ui.runButton.GetComponent<Image>().color = new Color(0.13f, 0.42f, 0.22f, 0.95f);
        ui.runButton.GetComponentInChildren<Text>().fontStyle = FontStyle.Bold;
        ui.undoButton = Btn(controlRow, "Undo", new Vector2(0.5f, 0.5f), new Vector2(-85, 0), new Vector2(150, 44), "Undo");
        ui.clearButton = Btn(controlRow, "Clear", new Vector2(0.5f, 0.5f), new Vector2(80, 0), new Vector2(150, 44), "Clear");
        ui.nextLevelButton = Btn(controlRow, "NextLevelBig", new Vector2(0.5f, 0.5f), new Vector2(245, 0), new Vector2(150, 44), "Next Level >");
        ui.nextLevelButton.GetComponent<Image>().color = new Color(0.15f, 0.3f, 0.5f, 0.95f);
        ui.nextLevelButton.gameObject.SetActive(false);

        return ui;
    }

    static Transform RowGroup(Transform parent, string name, float y)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0, y);
        rt.sizeDelta = Vector2.zero;
        return go.transform;
    }

    static Font UIFont() => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

    static Text Text(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, int fontSize, TextAnchor align)
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
        return text;
    }

    static Button Btn(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, string label)
    {
        var go = new GameObject($"Btn_{name}");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        go.AddComponent<Image>().color = ButtonColor;
        var btn = go.AddComponent<Button>();

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);
        var textRt = textGO.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.sizeDelta = Vector2.zero;
        var text = textGO.AddComponent<Text>();
        text.font = UIFont();
        text.fontSize = 15;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.text = label;
        return btn;
    }
}
