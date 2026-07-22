// Codex World — Main menu controller 

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Scene to load on PLAY")]
    public string gameSceneName = "CodexWorld";

    [Header("Menu Buttons")]
    public Button playButton;
    public Button challengesButton;
    public Button leaderboardButton;
    public Button codexButton;
    public Button settingsButton;
    public Button exitButton;

    [Header("Profile Card (top-right)")]
    public Text playerNameText;
    public Text xpText;
    public Text levelText;
    public Image xpBarFill;

    [Header("Screens")]
    public SettingsPanel settingsPanel;
    public AchievementsScreen achievementsScreen;

    [Header("Art (swap later)")]
    [Tooltip("Full-screen background - assign the pixel-art image when ready.")]
    public Image background;
    [Tooltip("Player avatar portrait in the profile card.")]
    public Image avatar;

    [Header("Placeholder progression")]
    public int xpPerLevel = 5000;

    void Start()
    {
        RefreshProfile();
    }


    public void RefreshProfile()
    {
        int totalXP = ProgressManager.TotalXP;
        int level = xpPerLevel > 0 ? (totalXP / xpPerLevel) + 1 : 1;
        int intoLevel = xpPerLevel > 0 ? totalXP % xpPerLevel : 0;

        if (xpText) xpText.text = $"{totalXP:N0} XP";
        if (levelText) levelText.text = level.ToString();
        if (xpBarFill) xpBarFill.fillAmount = xpPerLevel > 0 ? (float)intoLevel / xpPerLevel : 0f;
    }

    // ---------------- Button handlers (OnClick targets) ----------------
    public void OnPlay()
    {
        if (Application.CanStreamedLevelBeLoaded(gameSceneName))
            SceneManager.LoadScene(gameSceneName);
        else
            Debug.LogWarning($"MainMenu: scene '{gameSceneName}' is not in Build Settings. " +
                             "Add it via File > Build Settings to enable PLAY.");
    }

    public void OnSettings()
    {
        if (settingsPanel) settingsPanel.Open();
        else Placeholder("Settings");
    }

    public void OnAchievements()
    {
        if (achievementsScreen) achievementsScreen.Open();
        else Placeholder("Achievements");
    }

    public void OnLeaderboard() => Placeholder("Leaderboard");
    public void OnCodex() => Placeholder("Codex");

    public void OnExit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    static void Placeholder(string feature) =>
        Debug.Log($"MainMenu: '{feature}' is a placeholder for the draft - screen not built yet.");
}
