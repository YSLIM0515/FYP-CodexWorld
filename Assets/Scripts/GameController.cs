// Codex World — Game controller (scene-authored)
// Everything visual lives in the scene hierarchy; this component references
// those objects and runs the game: command queue, level switching, interpreter
// execution, and playback. Nothing is created at Play time.

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

    int _levelIndex;
    LevelDefinition _level;
    readonly List<CommandToken> _queue = new List<CommandToken>();
    bool _isRunning;

    void Awake()
    {
        ProgressManager.BaseXP = baseXP;
        ProgressManager.BonusXP = bonusXP;
        ProgressManager.LevelCount = levels.Length;

        // Buttons call the public handlers below through their OnClick() lists
        // in the Inspector (Tools > Codex World > Wire Game Buttons).
    }

    void Start()
    {
        int start = startAtLevel > 0
            ? Mathf.Clamp(startAtLevel - 1, 0, levels.Length - 1)
            : Mathf.Clamp(ProgressManager.HighestUnlocked, 0, levels.Length - 1);
        LoadLevel(start);
    }

    // ---------------- Level flow ----------------

    void LoadLevel(int index)
    {
        StopAllCoroutines();
        _isRunning = false;
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
        ui.ConfigureForLevel(_level, index, levels.Length, ProgressManager.IsCompleted(index));
        ui.RefreshXP();
        UpdateQueueText();
        ui.SetStatus("Build a command sequence, then press RUN.  (Left-drag rotates camera, right-drag moves view, scroll zooms)");
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
        if (target >= levels.Length) return;
        if (target > ProgressManager.HighestUnlocked)
        {
            ui.SetStatusBad("That region is still corrupted — complete this level to unlock it.");
            return;
        }
        LoadLevel(target);
    }

    public void ResetSave()
    {
        if (_isRunning) return;
        ProgressManager.ResetAll();
        LoadLevel(0);
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

        _isRunning = true;
        ui.ShowNextButton(false);

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
            int gained = ProgressManager.CompleteLevel(_levelIndex, bonus);
            ui.RefreshXP();

            bool isLast = _levelIndex == levels.Length - 1;
            if (isLast)
            {
                ui.SetStatusGood("NULL VIRUS PURGED! All regions of Codex World are restored." + (gained > 0 ? $"  +{gained} XP" : ""));
            }
            else
            {
                string msg = "SYSTEM RESTORED! Sequence succeeded.";
                if (gained > 0) msg += $"  +{gained} XP";
                if (bonus) msg += "  (optimal solution!)";
                ui.SetStatusGood(msg);
                ui.ShowNextButton(true);
            }
        }
        else if (result.Failed)
        {
            yield return avatar.PlayFail();
            ui.SetStatusBad($"ERROR: {result.FailureReason}.  Hint: {_level.hint}");
        }
        else
        {
            ui.SetStatusBad($"Sequence ended at {state.Position} — the beacon was not reached.  Hint: {_level.hint}");
        }

        _isRunning = false;
    }
}
