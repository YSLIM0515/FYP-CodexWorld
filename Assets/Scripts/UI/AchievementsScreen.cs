// Codex World — Achievements screen (scene-authored, live)
// Full-screen overlay showing the 18 achievements in Bronze/Silver/Gold tier
// rows. All cards are locked by default; AchievementManager persists unlocks
// and Refresh() restyles every card, the overall progress bar, tier counts and


using UnityEngine;
using UnityEngine.UI;

public class AchievementsScreen : MonoBehaviour
{
    [Header("Root (the object shown/hidden)")]
    [Tooltip("Object toggled on/off when opening/closing. Defaults to this GameObject.")]
    public GameObject panelRoot;

    [Header("Overall progress")]
    public Image progressFill;
    public Text progressLabel;
    public Text fractionLabel;

    [Header("Cards & tiers ")]
    public AchievementCard[] cards;
    public Text[] tierCounts;            // Bronze / Silver / Gold "n / 6"
    [Tooltip("Icon sprites in AchievementManager.Defs order (for the recent panel).")]
    public Sprite[] icons;

    [Header("Recently Unlocked slots")]
    [Tooltip("\"Nothing unlocked yet\" hint - hidden once anything is unlocked.")]
    public GameObject recentEmptyHint;
    public GameObject[] recentRoots;
    public Image[] recentIcons;
    public Text[] recentNames;
    public Text[] recentDescs;
    public Text[] recentTimes;

    [Header("Buttons")]
    public Button backButton;
    public Button viewAllButton;

    void Awake() => Hide();

    // Opened from the main menu (OnClick) — always shows current saved state.
    public void Open()
    {
        Refresh();
        Root().SetActive(true);
        transform.SetAsLastSibling();
    }

    
    public void Close() => Hide();

    public void Refresh()
    {
        int total = AchievementManager.Defs.Length;
        int count = 0;

        if (cards != null)
            foreach (var card in cards)
            {
                if (card == null) continue;
                bool unlocked = AchievementManager.IsUnlocked(card.index);
                if (unlocked) count++;
                card.Apply(unlocked);
            }

        SetProgress(count, total);

        if (tierCounts != null)
            for (int t = 0; t < tierCounts.Length; t++)
            {
                if (tierCounts[t] == null) continue;
                int c = 0;
                for (int i = t * 6; i < t * 6 + 6 && i < total; i++)
                    if (AchievementManager.IsUnlocked(i)) c++;
                tierCounts[t].text = c + " / 6";
            }

        if (recentRoots != null)
        {
            var recent = AchievementManager.Recent(recentRoots.Length);
            if (recentEmptyHint) recentEmptyHint.SetActive(recent.Length == 0);
            for (int s = 0; s < recentRoots.Length; s++)
            {
                bool used = s < recent.Length;
                if (recentRoots[s] != null) recentRoots[s].SetActive(used);
                if (!used) continue;
                int id = recent[s];
                if (recentIcons != null && s < recentIcons.Length && recentIcons[s] != null && icons != null && id < icons.Length)
                    recentIcons[s].sprite = icons[id];
                if (recentNames != null && s < recentNames.Length && recentNames[s] != null)
                    recentNames[s].text = AchievementManager.Defs[id].Name;
                if (recentDescs != null && s < recentDescs.Length && recentDescs[s] != null)
                    recentDescs[s].text = AchievementManager.Defs[id].Desc;
                if (recentTimes != null && s < recentTimes.Length && recentTimes[s] != null)
                    recentTimes[s].text = "Unlocked: " + AchievementManager.UnlockTime(id);
            }
        }
    }

    public void SetProgress(int unlocked, int total)
    {
        float f = total > 0 ? (float)unlocked / total : 0f;
        if (progressFill) progressFill.fillAmount = f;
        if (progressLabel) progressLabel.text = Mathf.RoundToInt(f * 100f) + "%";
        if (fractionLabel) fractionLabel.text = $"{unlocked} / {total}";
    }

    void Hide() => Root().SetActive(false);

    GameObject Root() => panelRoot != null ? panelRoot : gameObject;
}
