// Codex World — Student's Journey progress dashboard (scene-authored)
// Three region panels (Sequence Valley / Loop Factory / Logic Forest), each with
// a radial mastery dial, a levels-completed counter, a 3-star count and the XP
// earned in that region. Stats derive from ProgressManager (cw_done_{i} /
// cw_bonus_{i}, 3 levels per region); the optimal-solve flag stands in for the
// 3-star rating until per-level stars are persisted. Buttons are wired through
// their OnClick() lists (menu button -> Open, back button -> Close).

using UnityEngine;
using UnityEngine.UI;

public class StudentJourneyScreen : MonoBehaviour
{
    [Header("Root (the object shown/hidden)")]
    public GameObject panelRoot;

    [Header("Header")]
    public Text playerLabel;
    public Text totalXPLabel;

    [Header("Per-region (Sequence Valley, Loop Factory, Logic Forest)")]
    public Image[] ringFills;          // radial-filled mastery dials
    public Text[] masteryLabels;       // "78%"
    public Text[] completedLabels;     // "2 / 3"
    public Text[] starLabels;          // optimal (3-star) count "1 / 3"
    public Text[] xpLabels;            // "250 XP"

    [Header("Buttons")]
    public Button backButton;

    const int LevelsPerRegion = 3;

    void Awake() => Hide();

    // STUDENT'S JOURNEY menu button (OnClick) — always shows saved progress.
    public void Open()
    {
        Refresh();
        Root().SetActive(true);
        transform.SetAsLastSibling();
    }

    // BACK button (OnClick).
    public void Close() => Hide();

    public void Refresh()
    {
        GameProfile.Load();
        if (playerLabel) playerLabel.text = "Player: " + GameProfile.Name;
        if (totalXPLabel) totalXPLabel.text = ProgressManager.TotalXP + " / " +
            ProgressManager.LevelCount * (ProgressManager.BaseXP + ProgressManager.BonusXP) + " XP";

        int regions = ringFills != null ? ringFills.Length : 0;
        for (int r = 0; r < regions; r++)
        {
            int done = 0, bonus = 0, threeStar = 0, starSum = 0;
            for (int l = r * LevelsPerRegion; l < (r + 1) * LevelsPerRegion; l++)
            {
                if (ProgressManager.IsCompleted(l)) done++;
                if (ProgressManager.HasBonus(l)) bonus++;
                int s = ProgressManager.Stars(l);
                starSum += s;
                if (s >= 3) threeStar++;
            }
            // Mastery = earned stars out of the region's 9 possible.
            float mastery = starSum / (3f * LevelsPerRegion);
            int xp = done * ProgressManager.BaseXP + bonus * ProgressManager.BonusXP;

            if (ringFills[r]) ringFills[r].fillAmount = mastery;
            Set(masteryLabels, r, Mathf.RoundToInt(mastery * 100f) + "%");
            Set(completedLabels, r, done + " / " + LevelsPerRegion);
            Set(starLabels, r, threeStar + " / " + LevelsPerRegion);
            Set(xpLabels, r, xp + " XP");
        }
    }

    static void Set(Text[] arr, int i, string value)
    {
        if (arr != null && i < arr.Length && arr[i]) arr[i].text = value;
    }

    void Hide() => Root().SetActive(false);

    GameObject Root() => panelRoot != null ? panelRoot : gameObject;
}
