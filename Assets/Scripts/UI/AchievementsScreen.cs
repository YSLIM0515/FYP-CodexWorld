// Codex World — Achievements screen (scene-authored, display-only draft)
// Full-screen overlay showing achievements grouped into Bronze/Silver/Gold tier
// rows, an overall progress bar, and a "Recently Unlocked" panel. All cards are
// baked into the scene by AchievementsScreenBuilder as editable objects — this
// component only opens/closes the screen and exposes the progress widgets so a
// real achievement system can drive them later. Buttons are wired through their
// OnClick() lists in the Inspector (Back -> Close, menu button -> Open).

using UnityEngine;
using UnityEngine.UI;

public class AchievementsScreen : MonoBehaviour
{
    [Header("Root (the object shown/hidden)")]
    [Tooltip("Object toggled on/off when opening/closing. Defaults to this GameObject.")]
    public GameObject panelRoot;

    [Header("Overall progress (display only for now)")]
    public Image progressFill;
    public Text progressLabel;
    public Text fractionLabel;

    [Header("Buttons")]
    public Button backButton;
    public Button viewAllButton;

    void Awake() => Hide();

    // Opened from the main menu's ACHIEVEMENTS button (OnClick).
    public void Open()
    {
        Root().SetActive(true);
        transform.SetAsLastSibling();
    }

    // BACK button (OnClick).
    public void Close() => Hide();

    // For the future achievement system: updates the top progress readout.
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
