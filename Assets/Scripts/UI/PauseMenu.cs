// Codex World — Pause menu (scene-authored)
// ESC toggles this overlay during gameplay and pauses the game behind it
// (Time.timeScale = 0). Buttons (wired via OnClick() in the Inspector):
//   Resume              — close and unpause
//   Restart Level       — reload the current level from scratch
//   Settings            — open the shared SettingsPanel on top (stays paused)
//   Return to Main Menu — unpause and load the MainMenu scene
// ESC while the settings overlay is open closes settings first.
// Uses the new Input System.

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [Header("Root (the object shown/hidden)")]
    [Tooltip("Object toggled on/off when opening/closing. Defaults to this GameObject.")]
    public GameObject panelRoot;

    [Header("Scene references")]
    public GameController controller;
    public SettingsPanel settingsPanel;

    [Header("Buttons")]
    public Button resumeButton;
    public Button restartButton;
    public Button settingsButton;
    public Button returnButton;

    public bool IsOpen => Root().activeSelf;

    void Awake()
    {
        Time.timeScale = 1f;
        Hide();
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null || !kb.escapeKey.wasPressedThisFrame) return;

        // ESC closes the settings overlay first if it is on top.
        if (settingsPanel != null && settingsPanel.panelRoot != null && settingsPanel.panelRoot.activeSelf)
        {
            settingsPanel.Close();
            return;
        }
        if (IsOpen) Resume();
        else Open();
    }

    // ---------------- OnClick targets ----------------

    public void Open()
    {
        Time.timeScale = 0f;
        Root().SetActive(true);
        transform.SetAsLastSibling();
    }

    public void Resume()
    {
        Time.timeScale = 1f;
        Hide();
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        Hide();
        if (controller != null) controller.RestartLevel();
    }

    public void OpenSettings()
    {
        if (settingsPanel != null) settingsPanel.Open(); // game stays paused behind it
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        if (controller != null) controller.ReturnToMenu();
    }

    void Hide() => Root().SetActive(false);

    GameObject Root() => panelRoot != null ? panelRoot : gameObject;
}
