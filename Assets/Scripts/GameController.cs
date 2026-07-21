// Codex World — Game controller

using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class GameController : MonoBehaviour
{
    [Header("Scene References")]
    [Tooltip("Level roots in play order (each has a LevelDefinition).")]
    public LevelDefinition[] levels;
    public PlayerAvatar avatar;
    public GodViewCamera cameraController;
    public GameUI ui;

    [Header("Level Settings")]
    [Tooltip("Level to load when entering Play mode (1-based). 0 = continue from saved progress.")]
    public int startAtLevel = 0;

    [Header("Rewards (XP)")]
    [Tooltip("XP awarded the first time a level is completed.")]
    public int baseXP = 100;
    [Tooltip("Extra XP for solving at or below the level's optimal command count.")]
    public int bonusXP = 50;

    [Header("Challenge Mode")]
    [Tooltip("HINT button (visible in challenge mode only).")]
    public UnityEngine.UI.Button hintButton;
    public UnityEngine.UI.Text hintLabel;

    int _levelIndex;
    LevelDefinition _level;
    readonly List<CommandToken> _queue = new List<CommandToken>();
    bool _isRunning;
    bool _failedThisLevel; // third star = no failed runs on this level
    bool _clearedQueueThisLevel; // for the Refactorer achievement
    float _levelStartTime;       // for the Quick Thinker achievement
    int _attempts;               // runs on this level (analytics)
    TutorialGuide _tutorial;     // contextual lessons, found once per scene

    // Challenge mode state (cw_mode == 1): 5 generated maps, roguelike run.
    bool _challenge;
    ChallengeMap[] _challengeMaps;
    int _challengeUnlocked;      // highest reachable map this run
    int _hintTokens = 3;         // per-run hint budget
    int _hintTier;               // next hint tier on this map (0..2)
    bool _hintUsedThisMap;       // any hint caps the map at 2 stars
    bool _freeHint;              // granted after 2 fails on the same map
    int _failsThisMap;

    int CurrentBudget => _challenge && _challengeMaps != null ? _challengeMaps[_levelIndex].Budget : int.MaxValue;

    void Awake()
    {
        ProgressManager.BaseXP = baseXP;
        ProgressManager.BonusXP = bonusXP;
        _challenge = PlayerPrefs.GetInt("cw_mode", 0) == 1;
        if (_challenge) SetupChallenge();
        if (!_challenge) ProgressManager.LevelCount = levels.Length;

    }

    void Start()
    {
        int start = _challenge ? 0
            : startAtLevel > 0
                ? Mathf.Clamp(startAtLevel - 1, 0, levels.Length - 1)
                : Mathf.Clamp(ProgressManager.HighestUnlocked, 0, levels.Length - 1);
        LoadLevel(start);
    }

    // Generates a fresh 5-map run (new seed every entry) and swaps the level
    // array for the generated roots; beginner progress is never written to.
    void SetupChallenge()
    {
        var template = levels[levels.Length - 1];
        int seed = System.Environment.TickCount & 0xFFFFF;
        var maps = ChallengeGenerator.GenerateRun(seed);
        var parent = new GameObject("ChallengeLevels").transform;
        var defs = new LevelDefinition[maps.Length];
        for (int i = 0; i < maps.Length; i++)
        {
            defs[i] = maps[i] != null ? ChallengeLevelBuilder.Build(maps[i], i, template, parent) : null;
            if (defs[i] == null)
            {
                Debug.LogError("Codex World: challenge generation failed — falling back to beginner mode.");
                _challenge = false;
                return;
            }
        }
        foreach (var l in levels) l.gameObject.SetActive(false);
        _challengeMaps = maps;
        levels = defs;
        _challengeUnlocked = 0;
    }

    // ---------------- Level flow ----------------

    void LoadLevel(int index)
    {
        StopAllCoroutines();
        _isRunning = false;
        _failedThisLevel = false;
        _clearedQueueThisLevel = false;
        _levelStartTime = Time.time;
        _attempts = 0;
        _levelIndex = index;
        _level = levels[index];

        for (int j = 0; j < levels.Length; j++)
            levels[j].gameObject.SetActive(j == index);

        var state = _level.BuildGridState(out var start, out var facing);
        avatar.SnapTo(start, facing);

        var center = new Vector3((state.Width - 1) / 2f, 0f, (state.Height - 1) / 2f);
        float radius = Mathf.Max(state.Width, state.Height) * 0.5f + 1f;
        cameraController.SetTarget(center, radius);
        var cam = cameraController.GetComponent<Camera>();
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = _level.cameraBackground;
        }

        _queue.Clear();
        ui.ConfigureForLevel(_level, index, levels.Length, !_challenge && ProgressManager.IsCompleted(index));
        ui.RefreshXP();
        UpdateQueueText();
        ui.SetStatus("Build a command sequence, then press RUN.  (Left-drag rotates camera, right-drag moves view, scroll zooms)");

        // Challenge extras: per-map hint state + budget briefing + HINT button.
        _failsThisMap = 0;
        _hintTier = 0;
        _hintUsedThisMap = false;
        _freeHint = false;
        if (hintButton != null) hintButton.gameObject.SetActive(_challenge);
        if (_challenge)
        {
            UpdateHintLabel();
            ui.SetStatus($"CHALLENGE {index + 1}/5 — budget {CurrentBudget} commands (optimal {_level.optimalTokens}). Compress with loops and logic!");
        }

        // Contextual tutorial: teaches whatever this level just unlocked.
        if (_tutorial == null) _tutorial = FindAnyObjectByType<TutorialGuide>();
        if (_tutorial != null) _tutorial.OnLevelLoaded(_level, _challenge);

        // Repaint for colour-blind mode: this level's objects (or a freshly
        // built challenge map) only just became active.
        ColorBlindMode.Refresh();
    }

    public void GoPrev()
    {
        if (_isRunning || _levelIndex == 0) return;
        LoadLevel(_levelIndex - 1);
    }

    public void GoNext()
    {
        if (_isRunning) return;
        int target = _levelIndex + 1;
        // Past the final level (win panel "MAIN MENU" button, or nav at the end)
        // — the run is complete, so send the player home.
        if (target >= levels.Length)
        {
            ReturnToMenu();
            return;
        }
        if (_challenge ? target > _challengeUnlocked : target > ProgressManager.HighestUnlocked)
        {
            ui.SetStatusBad("That region is still corrupted — complete this level to unlock it.");
            return;
        }
        LoadLevel(target);
    }

    public void ResetSave()
    {
        if (_isRunning || _challenge) return; // challenge runs never touch the save
        ProgressManager.ResetAll();
        AchievementManager.ResetAll();
        LoadLevel(0); // re-arms the tutorial via its OnLevelLoaded hook
        ui.SetStatus("Save data reset. Starting over in Sequence Valley.");
    }

    // ---------------- Command queue (public methods = OnClick targets) ----------------

    public void AddMoveForward() => AppendToken(CommandToken.Simple(CommandType.MoveForward));
    public void AddTurnLeft() => AppendToken(CommandToken.Simple(CommandType.TurnLeft));
    public void AddTurnRight() => AppendToken(CommandToken.Simple(CommandType.TurnRight));
    public void AddIfObstacle() => AppendToken(CommandToken.Simple(CommandType.IfObstacle));
    public void AddIfClear() => AppendToken(CommandToken.Simple(CommandType.IfClear));

    // The loop count is set per button in the Inspector's OnClick() argument.
    public void AddLoop(int count) => AppendToken(CommandToken.Loop(count));

    public void Run() => StartCoroutine(RunSequence());

    // RETRY button on the fail panel: close it and let the player adjust the queue.
    public void Retry()
    {
        if (_isRunning) return;
        ui.HideResultPanels();
        _level.BuildGridState(out var start, out var facing);
        avatar.SnapTo(start, facing);
        ui.SetStatus("Adjust your sequence, then press RUN again.");
    }

    // Pause menu: reload the current level from scratch (clears the queue).
    public void RestartLevel()
    {
        LoadLevel(_levelIndex);
    }

    // RETURN button on the fail panel: back to the main menu scene.
    public void ReturnToMenu()
    {
        if (Application.CanStreamedLevelBeLoaded("MainMenu"))
            ScreenFader.LoadScene("MainMenu");
        else
            ui.SetStatusBad("MainMenu scene is not in Build Settings (File > Build Settings).");
    }

    Stack<char> OpenBlocks()
    {
        var stack = new Stack<char>();
        foreach (var t in _queue)
        {
            switch (t.Type)
            {
                case CommandType.LoopStart: stack.Push('L'); break;
                case CommandType.IfObstacle:
                case CommandType.IfClear: stack.Push('I'); break;
                case CommandType.LoopEnd:
                case CommandType.IfEnd: if (stack.Count > 0) stack.Pop(); break;
            }
        }
        return stack;
    }

    void AppendToken(CommandToken token)
    {
        if (_isRunning) return;
        _queue.Add(token);
        UpdateQueueText();
    }

    public void EndBlock()
    {
        if (_isRunning) return;
        var open = OpenBlocks();
        if (open.Count == 0)
        {
            ui.SetStatusBad("No open LOOP/IF block to close.");
            return;
        }
        _queue.Add(CommandToken.Simple(open.Peek() == 'L' ? CommandType.LoopEnd : CommandType.IfEnd));
        UpdateQueueText();
    }

    public void UndoToken()
    {
        if (_isRunning || _queue.Count == 0) return;
        _queue.RemoveAt(_queue.Count - 1);
        UpdateQueueText();
    }

    public void ClearQueue()
    {
        if (_isRunning) return;
        if (_queue.Count > 0) _clearedQueueThisLevel = true;
        _queue.Clear();
        UpdateQueueText();
        ui.SetStatus("Queue cleared.");
    }

    void UpdateQueueText()
    {
        if (_queue.Count == 0)
        {
            ui.SetQueue("Queue: (empty)");
            return;
        }
        var sb = new StringBuilder("Queue: ");
        foreach (var t in _queue)
        {
            sb.Append(t.Type switch
            {
                CommandType.MoveForward => "MOVE",
                CommandType.TurnLeft => "LEFT",
                CommandType.TurnRight => "RIGHT",
                CommandType.LoopStart => $"LOOP{t.LoopCount}{{",
                CommandType.IfObstacle => "IF_OBSTACLE{",
                CommandType.IfClear => "IF_CLEAR{",
                CommandType.LoopEnd => "}",
                CommandType.IfEnd => "}",
                _ => t.Type.ToString()
            });
            sb.Append(' ');
        }
        sb.Append($"  [{_queue.Count} blocks]");
        ui.SetQueue(sb.ToString());
    }

    // ---------------- Execution & playback ----------------

    IEnumerator RunSequence()
    {
        if (_isRunning) yield break;
        if (_queue.Count == 0)
        {
            ui.SetStatusBad("The queue is empty — add some commands first.");
            yield break;
        }
        if (OpenBlocks().Count > 0)
        {
            ui.SetStatusBad("You have an unclosed LOOP/IF block. Press End Block first.");
            yield break;
        }
        if (_challenge && _queue.Count > CurrentBudget)
        {
            ui.SetStatusBad($"Over budget! This sector allows {CurrentBudget} command blocks — you queued {_queue.Count}. Compress with loops/logic.");
            yield break;
        }

        _isRunning = true;
        _attempts++;
        ui.HideResultPanels();

        var state = _level.BuildGridState(out var start, out var facing);
        var result = CommandInterpreter.Run(_queue, state);

        avatar.SnapTo(start, facing);
        for (int i = 0; i < result.Snapshots.Count; i++)
        {
            var (pos, face) = result.Snapshots[i];
            ui.SetStatus($"Executing... step {i + 1}/{result.Snapshots.Count}");
            yield return avatar.AnimateTo(pos, face);
        }

        if (result.ReachedGoal)
        {
            yield return avatar.PlayWin();
            bool bonus = _queue.Count <= _level.optimalTokens;
            int gained = _challenge ? 0 : ProgressManager.CompleteLevel(_levelIndex, bonus);
            ui.RefreshXP();

            bool isLast = _levelIndex == levels.Length - 1;
            int stars = 1 + (bonus ? 1 : 0) + (_failedThisLevel ? 0 : 1);
            if (_challenge && _hintUsedThisMap) stars = Mathf.Min(stars, 2); // hints cap at 2 stars
            string msg = _challenge
                ? (isLast ? "CHALLENGE COMPLETE! All 5 corrupted sectors purged."
                          : $"SECTOR CLEARED! {levels.Length - 1 - _levelIndex} corrupted sector(s) remain.")
                : (isLast ? "NULL VIRUS PURGED! All regions of Codex World are restored."
                          : "EXCELLENT WORK! You reached the data beacon.");
            if (_challenge) _challengeUnlocked = Mathf.Max(_challengeUnlocked, _levelIndex + 1);
            else ProgressManager.SetStars(_levelIndex, stars);
            ui.SetStatusGood("SYSTEM RESTORED!" + (bonus ? "  (optimal solution!)" : ""));
            ui.ShowWin(stars, gained, isLast, msg);
            ScanQueue(out bool wLoop, out bool wIf);
            AnalyticsLogger.LogRun(_challenge, _levelIndex, _level.levelName, "win", _attempts,
                Time.time - _levelStartTime, _queue.Count, _level.optimalTokens, wLoop, wIf, _hintTier, stars);
            if (!_challenge) EvaluateAchievements(bonus, isLast);
            UnlockToast.CheckCodex(); // codex rules may have flipped with this win
        }
        else if (result.Failed)
        {
            _failedThisLevel = true;
            yield return avatar.PlayFail();
            ui.SetStatusBad($"ERROR: {result.FailureReason}.");
            ui.ShowFail($"Oops! {result.FailureReason}.", FailTip());
            ScanQueue(out bool fLoop, out bool fIf);
            AnalyticsLogger.LogRun(_challenge, _levelIndex, _level.levelName, "fail_crash", _attempts,
                Time.time - _levelStartTime, _queue.Count, _level.optimalTokens, fLoop, fIf, _hintTier, 0);
            if (!_challenge) RegisterFailAchievements();
        }
        else
        {
            _failedThisLevel = true;
            ui.SetStatusBad("The beacon was not reached.");
            ui.ShowFail($"Sequence ended at {state.Position} — the beacon was not reached.", FailTip());
            ScanQueue(out bool sLoop, out bool sIf);
            AnalyticsLogger.LogRun(_challenge, _levelIndex, _level.levelName, "fail_incomplete", _attempts,
                Time.time - _levelStartTime, _queue.Count, _level.optimalTokens, sLoop, sIf, _hintTier, 0);
            if (!_challenge) RegisterFailAchievements();
        }

        _isRunning = false;
    }

    // Which constructs the current queue uses (analytics + achievements share this).
    void ScanQueue(out bool usedLoop, out bool usedIf)
    {
        usedLoop = false; usedIf = false;
        foreach (var t in _queue)
        {
            if (t.Type == CommandType.LoopStart) usedLoop = true;
            if (t.Type == CommandType.IfObstacle || t.Type == CommandType.IfClear) usedIf = true;
        }
    }

    // ---------------- Challenge hints (see Docs/ChallengeMode-Plan.md) ----------------

    // Beginner keeps its authored tip; challenge hides tips but offers a free
    // hint after two failed runs on the same map (fairness backstop).
    string FailTip()
    {
        if (!_challenge) return _level.hint;
        _failsThisMap++;
        if (_failsThisMap >= 2 && !_hintUsedThisMap) _freeHint = true;
        UpdateHintLabel();
        return _freeHint
            ? "Struggling? Press HINT — this one's free."
            : "No tips in Challenge Mode. Trace the route before you run.";
    }

    // HINT button (OnClick). Escalating ladder: route glow -> program shape ->
    // first commands. Costs one token (or the free hint) and caps the map at 2 stars.
    public void UseHint()
    {
        if (!_challenge || _isRunning) return;
        if (_hintTier > 2)
        {
            ui.SetStatus("All hints for this sector are already revealed.");
            return;
        }
        bool free = _freeHint;
        if (!free && _hintTokens <= 0)
        {
            ui.SetStatusBad("No hint tokens left this run — solve it with pure logic!");
            return;
        }
        var data = _level.GetComponent<ChallengeLevelData>();
        if (data == null || data.Map == null) return;

        switch (_hintTier)
        {
            case 0:
                StartCoroutine(GlowPath(data));
                ui.SetStatusGood("HINT 1: the optimal route lights up for a moment — memorize it!");
                break;
            case 1:
                int loops = 0, ifs = 0;
                foreach (var t in data.Map.Solution)
                {
                    if (t.Type == CommandType.LoopStart) loops++;
                    if (t.Type == CommandType.IfObstacle || t.Type == CommandType.IfClear) ifs++;
                }
                ui.SetStatusGood($"HINT 2: the optimal program is {data.Map.OptimalTokens} tokens — {loops} loop(s), {ifs} IF(s).");
                break;
            default:
                _queue.Clear();
                for (int i = 0; i < Mathf.Min(4, data.Map.Solution.Count); i++)
                    _queue.Add(data.Map.Solution[i]);
                UpdateQueueText();
                ui.SetStatusGood("HINT 3: the optimal program's first commands are in your queue — finish it.");
                break;
        }

        _hintTier++;
        _hintUsedThisMap = true; // star cap
        if (free) _freeHint = false; else _hintTokens--;
        UpdateHintLabel();
    }

    void UpdateHintLabel()
    {
        if (hintLabel != null)
            hintLabel.text = _freeHint ? "HINT (FREE)" : $"HINT x{_hintTokens}";
    }

    IEnumerator GlowPath(ChallengeLevelData data)
    {
        var originals = new List<(Renderer r, Color c)>();
        foreach (var r in data.PathTiles)
            if (r != null)
            {
                originals.Add((r, r.material.color));
                r.material.color = new Color(1f, 0.85f, 0.25f);
            }
        yield return new WaitForSeconds(3f);
        foreach (var (r, c) in originals)
            if (r != null) r.material.color = c;
    }

    // ---------------- Achievements ----------------

    // Checks every unlock condition after a successful run and announces new ones.
    void EvaluateAchievements(bool bonus, bool isLast)
    {
        bool usedTurn = false, usedLoop = false, usedIf = false, nested = false;
        int loopDepth = 0;
        foreach (var t in _queue)
        {
            switch (t.Type)
            {
                case CommandType.TurnLeft:
                case CommandType.TurnRight: usedTurn = true; break;
                case CommandType.LoopStart: if (loopDepth > 0) nested = true; loopDepth++; usedLoop = true; break;
                case CommandType.LoopEnd: if (loopDepth > 0) loopDepth--; break;
                case CommandType.IfObstacle:
                case CommandType.IfClear: usedIf = true; break;
            }
        }

        int bonusCount = 0;
        for (int i = 0; i < levels.Length; i++)
            if (ProgressManager.HasBonus(i)) bonusCount++;
        float elapsed = Time.time - _levelStartTime;

        var unlocked = new List<string>();
        void Try(int id, bool cond)
        {
            if (cond && AchievementManager.Unlock(id))
            {
                unlocked.Add(AchievementManager.Defs[id].Name);
                UnlockToast.Announce("ACHIEVEMENT UNLOCKED", AchievementManager.Defs[id].Name, UnlockToast.AchievementGold);
            }
        }

        Try(0, true);                                  // Hello World
        Try(1, _failedThisLevel);                      // Debugger
        Try(2, usedTurn);                              // Turn Around
        Try(3, _levelIndex == 2);                      // Valley Clear
        Try(4, bonus);                                 // Minimalist
        Try(5, elapsed < 30f);                         // Quick Thinker
        Try(6, usedLoop);                              // Loop Learner
        Try(7, bonusCount >= 3);                       // Efficient Coder
        Try(8, _clearedQueueThisLevel);                // Refactorer
        Try(9, _levelIndex == 5);                      // Factory Foreman
        Try(10, nested);                               // Nested Mind
        Try(12, usedIf);                               // Logic Master
        Try(13, usedLoop && usedIf);                   // Algorithm Architect
        Try(14, isLast);                               // Virus Purger
        Try(15, bonusCount >= levels.Length);          // Perfectionist
        Try(16, isLast && Time.realtimeSinceStartup < 1200f); // Speed Demon
        bool othersAll = true;
        for (int i = 0; i < 17; i++)
            if (!AchievementManager.IsUnlocked(i)) othersAll = false;
        Try(17, othersAll);                            // True Coder

        if (unlocked.Count > 0)
            ui.SetStatusGood("ACHIEVEMENT UNLOCKED: " + string.Join(", ", unlocked));
    }

    // Failure-side achievement (Bug Squasher: 10 total failed runs).
    void RegisterFailAchievements()
    {
        int fails = AchievementManager.RegisterFail();
        if (fails >= 10 && AchievementManager.Unlock(11))
        {
            ui.SetStatus("ACHIEVEMENT UNLOCKED: " + AchievementManager.Defs[11].Name);
            UnlockToast.Announce("ACHIEVEMENT UNLOCKED", AchievementManager.Defs[11].Name, UnlockToast.AchievementGold);
        }
        UnlockToast.CheckCodex(); // the Debugging codex page unlocks on the first fail
    }
}
