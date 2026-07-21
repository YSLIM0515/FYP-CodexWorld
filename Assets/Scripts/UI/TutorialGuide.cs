// Codex World — Contextual tutorial lessons (scene-authored)
// Just-in-time onboarding: each lesson appears once, exactly when its concept
// first becomes available, with a pulsing highlight over the button to press.
//   Basics (fresh save, level 1):  MOVE -> queue idea -> RUN      (cw_tutorial_done)
//   Loops (first allowLoops level): LOOP x2 -> body -> END BLOCK  (cw_tut_loops)
//   Ifs (first allowConditionals level): IF -> body -> END BLOCK  (cw_tut_ifs)
// GameController calls OnLevelLoaded() on every level load; ResetSave clears
// the flags so a fresh save replays all lessons. Listeners are added at
// runtime on purpose — the Inspector-authored OnClick lists stay untouched.

using UnityEngine;
using UnityEngine.UI;

public class TutorialGuide : MonoBehaviour
{
    [Header("References")]
    public GameUI ui;
    [Tooltip("Object toggled on/off (tip box + highlight).")]
    public GameObject panelRoot;
    public Text tipText;
    [Tooltip("Pulsing box moved over the button the player should press.")]
    public RectTransform highlight;

    enum Lesson { None, Basics, Loops, Ifs }

    const string KeyBasics = "cw_tutorial_done";
    const string KeyLoops = "cw_tut_loops";
    const string KeyIfs = "cw_tut_ifs";

    Image _highlightImage;
    Lesson _lesson = Lesson.None;
    int _step = -1;
    int _moves;
    bool _subscribed;

    static bool Done(string key) => PlayerPrefs.GetInt(key, 0) == 1;

    // Called by GameController on every level load (including after ResetSave).
    public void OnLevelLoaded(LevelDefinition level, bool challenge)
    {
        StopLesson();
        if (challenge || ui == null || level == null) return;

        if (!Done(KeyBasics) && ProgressManager.HighestUnlocked == 0 && !level.allowLoops)
            StartLesson(Lesson.Basics);
        else if (level.allowLoops && !Done(KeyLoops))
            StartLesson(Lesson.Loops);
        else if (level.allowConditionals && !Done(KeyIfs))
            StartLesson(Lesson.Ifs);
    }

    void StartLesson(Lesson lesson)
    {
        _lesson = lesson;
        _moves = 0;
        _highlightImage = highlight != null ? highlight.GetComponent<Image>() : null;
        Subscribe();
        ShowStep(0);
    }

    void Update()
    {
        if (_step < 0 || _highlightImage == null) return;
        var c = _highlightImage.color;
        c.a = 0.18f + Mathf.PingPong(Time.unscaledTime * 0.45f, 0.22f);
        _highlightImage.color = c;
    }

    // ---------------- Steps ----------------

    void ShowStep(int step)
    {
        _step = step;
        if (panelRoot != null) panelRoot.SetActive(true);

        var target = TargetFor(step);
        if (highlight != null && target != null)
        {
            // Buttons are authored with a top-left pivot — center on the rect.
            var rt = (RectTransform)target.transform;
            highlight.position = rt.TransformPoint(rt.rect.center);
            highlight.sizeDelta = rt.rect.size + new Vector2(16, 16);
        }
        if (tipText != null) tipText.text = TextFor(step);
    }

    Button TargetFor(int step)
    {
        switch (_lesson)
        {
            case Lesson.Basics: return step < 2 ? ui.moveForwardButton : ui.runButton;
            case Lesson.Loops: return step == 0 ? ui.loop2Button : step == 1 ? ui.moveForwardButton : ui.endBlockButton;
            case Lesson.Ifs: return step == 0 ? ui.ifObstacleButton : step == 1 ? ui.turnLeftButton : ui.endBlockButton;
            default: return null;
        }
    }

    string TextFor(int step)
    {
        switch (_lesson)
        {
            case Lesson.Basics:
                return step == 0
                    ? "Welcome, coder! This robot is AXIOM.\nClick the highlighted MOVE button\nto add a step to your program."
                    : step == 1
                    ? "Each click adds one command to the\nqueue below. Add a couple more\nMOVE commands."
                    : "Your first program is ready!\nPress RUN to make AXIOM execute it.";
            case Lesson.Loops:
                return step == 0
                    ? "NEW COMMAND: LOOP!\nA loop repeats everything inside it.\nClick LOOP x2 to open a repeat block."
                    : step == 1
                    ? "You are inside the loop now.\nAdd a MOVE — the loop will run it twice."
                    : "Close the block with END BLOCK.\nLOOP x4 [ MOVE ] gives 4 moves from\njust 3 blocks — loops save commands!";
            case Lesson.Ifs:
                return step == 0
                    ? "NEW COMMAND: IF!\nIF OBSTACLE runs its block only when a\ncorrupted block is directly ahead. Click it."
                    : step == 1
                    ? "Commands inside the IF run only when\nthe condition is true. Add TURN LEFT\nto dodge the obstacle."
                    : "Close the IF with END BLOCK.\nIF CLEAR is the opposite — it runs\nwhen the path ahead is open.";
            default:
                return "";
        }
    }

    // ---------------- Button handlers ----------------

    void OnBody()
    {
        if (_lesson == Lesson.Basics && _step <= 1)
        {
            _moves++;
            ShowStep(_moves >= 3 ? 2 : 1);
        }
        else if ((_lesson == Lesson.Loops || _lesson == Lesson.Ifs) && _step == 1)
            ShowStep(2);
    }

    void OnLoopStart() { if (_lesson == Lesson.Loops && _step == 0) ShowStep(1); }
    void OnIfStart() { if (_lesson == Lesson.Ifs && _step == 0) ShowStep(1); }
    void OnEndBlock() { if ((_lesson == Lesson.Loops || _lesson == Lesson.Ifs) && _step == 2) Complete(); }
    void OnRun() { if (_lesson == Lesson.Basics) Complete(); }

    // ---------------- Lifecycle ----------------

    void Complete()
    {
        string key = _lesson == Lesson.Basics ? KeyBasics : _lesson == Lesson.Loops ? KeyLoops : KeyIfs;
        PlayerPrefs.SetInt(key, 1);
        PlayerPrefs.Save();
        StopLesson();
    }

    void StopLesson()
    {
        Unsubscribe();
        _lesson = Lesson.None;
        _step = -1;
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    void Subscribe()
    {
        if (_subscribed || ui == null) return;
        _subscribed = true;
        Add(ui.moveForwardButton, OnBody);
        Add(ui.turnLeftButton, OnBody);
        Add(ui.turnRightButton, OnBody);
        Add(ui.runButton, OnRun);
        Add(ui.loop2Button, OnLoopStart);
        Add(ui.loop3Button, OnLoopStart);
        Add(ui.loop4Button, OnLoopStart);
        Add(ui.ifObstacleButton, OnIfStart);
        Add(ui.ifClearButton, OnIfStart);
        Add(ui.endBlockButton, OnEndBlock);
    }

    void Unsubscribe()
    {
        if (!_subscribed || ui == null) return;
        _subscribed = false;
        Remove(ui.moveForwardButton, OnBody);
        Remove(ui.turnLeftButton, OnBody);
        Remove(ui.turnRightButton, OnBody);
        Remove(ui.runButton, OnRun);
        Remove(ui.loop2Button, OnLoopStart);
        Remove(ui.loop3Button, OnLoopStart);
        Remove(ui.loop4Button, OnLoopStart);
        Remove(ui.ifObstacleButton, OnIfStart);
        Remove(ui.ifClearButton, OnIfStart);
        Remove(ui.endBlockButton, OnEndBlock);
    }

    static void Add(Button b, UnityEngine.Events.UnityAction a) { if (b) b.onClick.AddListener(a); }
    static void Remove(Button b, UnityEngine.Events.UnityAction a) { if (b) b.onClick.RemoveListener(a); }
}
