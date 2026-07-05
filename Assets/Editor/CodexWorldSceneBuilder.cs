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
