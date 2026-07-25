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


    // Remembers the XP the bar last animated to, so a return from a level
    // animates only the newly-earned amount rather than snapping.
    const string ShownKey = "cw_xp_shown";

    public void RefreshProfile()
    {
        int target = ProgressManager.TotalXP;
        int shown = PlayerPrefs.GetInt(ShownKey, target);
        if (isActiveAndEnabled && target > shown)
        {
            StopAllCoroutines();
            StartCoroutine(AnimateXP(shown, target)); // fill up the gained XP
        }
        else
        {
            ApplyProfile(target);
            PlayerPrefs.SetInt(ShownKey, target);
        }
    }

    // Sets the bar, XP text and level for a given XP amount.
    void ApplyProfile(int xp)
    {
        int level = xpPerLevel > 0 ? (xp / xpPerLevel) + 1 : 1;
        int intoLevel = xpPerLevel > 0 ? xp % xpPerLevel : 0;
        if (xpText) xpText.text = $"{xp:N0} XP";
        if (levelText) levelText.text = level.ToString();
        if (xpBarFill) xpBarFill.fillAmount = xpPerLevel > 0 ? (float)intoLevel / xpPerLevel : 0f;
    }

    // Counts XP up from -> to over ~1s; the fill wraps naturally on level-up
    // because the fraction is recomputed each frame.
    System.Collections.IEnumerator AnimateXP(int from, int to)
    {
        const float dur = 1f;
        for (float t = 0f; t < dur; t += Time.unscaledDeltaTime)
        {
            int xp = Mathf.RoundToInt(Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / dur)));
            ApplyProfile(xp);
            yield return null;
        }
        ApplyProfile(to);
        PlayerPrefs.SetInt(ShownKey, to);
        PlayerPrefs.Save();
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
