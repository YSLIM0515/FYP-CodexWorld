// Codex World — presentation hotkeys
// Two shortcuts for demoing the game to a panel inside a fixed time slot:
// queue the solution for the current level, or queue it and run it. Works in
// both beginner and challenge mode.
//
// Both combos need Ctrl+Shift held, so nothing here can fire by accident while
// playing. Untick shortcutsEnabled (or the GameObject) for a public build.

using UnityEngine;

public class PresenterKeys : MonoBehaviour
{
    [Header("Wiring")]
    [Tooltip("Left empty, it finds the GameController in the scene on first use.")]
    public GameController controller;

    [Header("Behaviour")]
    [Tooltip("Off = shortcuts ignored entirely. Leave off in anything the public plays.")]
    public bool shortcutsEnabled = true;

    string _toast = "";
    float _toastUntil;
    GUIStyle _panel, _text;

    void Update()
    {
        if (!shortcutsEnabled) return;

        // Both modifiers, so a stray keypress during play does nothing.
        bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        if (!ctrl || !shift) return;

        if (Input.GetKeyDown(KeyCode.S))
        {
            var c = Controller();
            if (c != null) c.AutoSolve();
            Toast("Solution queued - press RUN");
        }
        else if (Input.GetKeyDown(KeyCode.X))
        {
            var c = Controller();
            if (c != null) c.AutoSolveAndRun();
            Toast("Solving and running");
        }
    }

    GameController Controller()
    {
        if (controller == null) controller = FindAnyObjectByType<GameController>();
        return controller;
    }

    void Toast(string msg)
    {
        _toast = msg;
        _toastUntil = Time.unscaledTime + 2.5f;
    }

    // Small corner confirmation, so it is obvious the key registered.
    void OnGUI()
    {
        if (!shortcutsEnabled) return;
        if (Time.unscaledTime >= _toastUntil || _toast.Length == 0) return;

        EnsureStyles();
        var size = _text.CalcSize(new GUIContent(_toast));
        var box = new Rect(20, Screen.height - size.y - 42, size.x + 28, size.y + 20);
        GUI.Box(box, GUIContent.none, _panel);
        GUI.Label(new Rect(box.x + 14, box.y + 10, size.x, size.y), _toast, _text);
    }

    void EnsureStyles()
    {
        if (_panel != null) return;

        var bg = new Texture2D(1, 1);
        bg.SetPixel(0, 0, new Color(0.02f, 0.05f, 0.09f, 0.92f));
        bg.Apply();

        _panel = new GUIStyle(GUI.skin.box) { normal = { background = bg } };
        _text = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            normal = { textColor = new Color(0.45f, 0.85f, 1f) }
        };
    }
}
