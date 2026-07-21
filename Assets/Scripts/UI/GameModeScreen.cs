// Codex World — Game mode select (scene-authored)
// Opened by the PLAY button. BEGINNER MODE is the original 9-level journey;
// CHALLENGE MODE unlocks once every beginner level is completed (reusing the
// achievements padlock UI while locked). The chosen mode is saved to the
// cw_mode PlayerPrefs key (0 = beginner, 1 = challenge) so the game scene can
// branch on it when challenge content lands. Buttons wired via OnClick() lists.

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameModeScreen : MonoBehaviour
{
    [Header("Root (the object shown/hidden)")]
    public GameObject panelRoot;

    [Header("Mode cards")]
    public Button beginnerButton;
    public Button challengeButton;
    public Image challengeFrame;
    public Sprite challengeLockedFrame;     // grey achievements frame
    public Sprite challengeUnlockedFrame;   // gold achievements frame
    public Image challengeIcon;
    public GameObject challengePadlock;     // reused ACH_Lock sprite
    public Text challengeHint;

    [Header("Buttons")]
    public Button backButton;

    const string GameScene = "CodexWorld";

    static readonly Color LockedIcon = new Color(0.44f, 0.47f, 0.53f, 1f);

    void Awake() => Hide();

    // Challenge opens only after the whole beginner journey is finished.
    public static bool ChallengeUnlocked
    {
        get
        {
            for (int i = 0; i < ProgressManager.LevelCount; i++)
                if (!ProgressManager.IsCompleted(i)) return false;
            return true;
        }
    }

    // PLAY button (OnClick).
    public void Open()
    {
        Refresh();
        Root().SetActive(true);
        transform.SetAsLastSibling();
    }

    // BACK button (OnClick).
    public void Close() => Hide();

    // BEGINNER MODE card (OnClick) — the original game.
    public void PlayBeginner()
    {
        PlayerPrefs.SetInt("cw_mode", 0);
        PlayerPrefs.Save();
        ScreenFader.LoadScene(GameScene);
    }

    // CHALLENGE MODE card (OnClick) — interactable only once unlocked.
    public void PlayChallenge()
    {
        if (!ChallengeUnlocked) return;
        PlayerPrefs.SetInt("cw_mode", 1);
        PlayerPrefs.Save();
        ScreenFader.LoadScene(GameScene);
    }

    public void Refresh()
    {
        bool unlocked = ChallengeUnlocked;
        if (challengeButton) challengeButton.interactable = unlocked;
        if (challengeFrame && challengeLockedFrame && challengeUnlockedFrame)
            challengeFrame.sprite = unlocked ? challengeUnlockedFrame : challengeLockedFrame;
        if (challengeIcon) challengeIcon.color = unlocked ? Color.white : LockedIcon;
        if (challengePadlock) challengePadlock.SetActive(!unlocked);
        if (challengeHint) challengeHint.text = unlocked
            ? "Harder puzzles - tighter command limits!"
            : "Complete Beginner Mode to unlock";
    }

    void Hide() => Root().SetActive(false);

    GameObject Root() => panelRoot != null ? panelRoot : gameObject;
}
