// Codex World — Challenge run summary
// Challenge mode writes no progress and no achievements by design, so without
// this panel a completed run leaves nothing behind. Shown after the last map is
// cleared: maps cleared, stars earned, hints spent, run time, and the run seed
// (the seed is what makes a run reproducible — useful for comparing playtests).

using UnityEngine;
using UnityEngine.UI;

public class ChallengeSummary : MonoBehaviour
{
    [Tooltip("Object shown/hidden. Defaults to this GameObject.")]
    public GameObject panelRoot;

    public Text titleText;
    public Text mapsText;
    public Text starsText;
    public Text hintsText;
    public Text xpText;
    public Text timeText;
    public Text seedText;
    public Text gradeText;

    static readonly Color GradeS = new Color(1f, 0.85f, 0.3f);
    static readonly Color GradeA = new Color(0.5f, 0.9f, 1f);
    static readonly Color GradeB = new Color(0.75f, 0.8f, 0.88f);

    // No Awake-hide here: the panel is authored inactive in the scene, and an
    // inactive object defers Awake until the first SetActive(true) — which would
    // fire Hide() the instant Show() reveals it.

    public void Hide()
    {
        var root = panelRoot != null ? panelRoot : gameObject;
        root.SetActive(false);
    }

    public void Show(int mapsCleared, int mapsTotal, int stars, int hintsUsed, float seconds, int seed, int xpEarned = 0)
    {
        if (xpText != null) xpText.text = xpEarned > 0 ? $"+{xpEarned} XP" : "—";
        int maxStars = mapsTotal * 3;
        if (titleText != null)
            titleText.text = mapsCleared >= mapsTotal ? "RUN COMPLETE" : "RUN ENDED";
        if (mapsText != null) mapsText.text = $"{mapsCleared} / {mapsTotal}";
        if (starsText != null) starsText.text = $"{stars} / {maxStars}";
        if (hintsText != null) hintsText.text = hintsUsed == 0 ? "None" : hintsUsed.ToString();
        if (timeText != null)
        {
            int m = Mathf.FloorToInt(seconds / 60f);
            int s = Mathf.FloorToInt(seconds % 60f);
            timeText.text = $"{m}:{s:00}";
        }
        // The seed reproduces this exact run — handy when comparing playtests.
        if (seedText != null) seedText.text = $"SEED {seed:X}";

        if (gradeText != null)
        {
            float ratio = maxStars > 0 ? (float)stars / maxStars : 0f;
            bool perfect = mapsCleared >= mapsTotal && hintsUsed == 0;
            string grade = perfect && ratio >= 0.95f ? "S"
                         : ratio >= 0.8f ? "A"
                         : ratio >= 0.6f ? "B"
                         : ratio >= 0.4f ? "C" : "D";
            gradeText.text = grade;
            gradeText.color = grade == "S" ? GradeS : grade == "A" ? GradeA : GradeB;
        }

        var root = panelRoot != null ? panelRoot : gameObject;
        root.SetActive(true);
        root.transform.SetAsLastSibling();
    }
}
