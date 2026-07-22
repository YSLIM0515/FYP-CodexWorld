// Codex World — HUD binder (scene-authored UI)
// The canvas, texts, and buttons are real objects in the hierarchy — restyle
// or rearrange them freely in the Scene view. This component just holds
// references to them; each button's behaviour is configured in its OnClick()
// list in the Inspector, targeting GameController's public handlers.
// Layout (per the in-game concept): top bar (level | map name, XP, objective +
// star conditions, settings gear), right-side command palette + queue panel,
// and two centered result overlays (LEVEL COMPLETE! / TRY AGAIN!).

using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    [Header("Top Bar")]
    public Text title;              // "LEVEL 01  |  FIRST STEPS"
    public Text xpText;
    public Text objectiveText;
    public Text starConditionText;  // how to earn all 3 stars
    public Text levelLabel;         // "Level 1 / 9" nav readout
    public Button settingsButton;   // opens the SettingsPanel

    [Header("Texts")]
    public Text story;
    public Text status;
    public Text queueText;

    [Header("Basic Command Buttons")]
    public Button moveForwardButton;
    public Button turnLeftButton;
    public Button turnRightButton;

    [Header("Loop Buttons")]
    public Button loop2Button;
    public Button loop3Button;
    public Button loop4Button;
    public Button endBlockButton;

    [Header("Conditional Buttons")]
    public Button ifObstacleButton;
    public Button ifClearButton;

    [Header("Control Buttons")]
    public Button runButton;
    public Button undoButton;
    public Button clearButton;
    public Button prevButton;
    public Button nextButton;
    public Button nextLevelButton;  // inside the win panel
    public Button resetSaveButton;
    public Button retryButton;      // inside the fail panel
    public Button returnButton;     // inside the fail panel (back to main menu)

    [Header("Groups (shown/hidden per level)")]
    public GameObject loopRow;
    public GameObject conditionalRow;

    [Header("Result Panels")]
    public GameObject winPanel;
    public Text winMessage;
    public Text winXPText;
    public Text[] winStars;         // 3 star glyphs, colored by rating
    public GameObject failPanel;
    public Text failReason;
    public Text failTip;

    static readonly Color StarOn = new Color(1f, 0.8f, 0.2f);
    static readonly Color StarOff = new Color(0.22f, 0.26f, 0.32f);

    public void ConfigureForLevel(LevelDefinition level, int index, int total, bool completed)
    {
        if (title != null) title.text = $"LEVEL {index + 1:00}  |  {level.levelName.ToUpper()}";
        if (story != null) story.text = level.story;
        if (levelLabel != null) levelLabel.text = $"Level {index + 1} / {total}" + (completed ? "  [DONE]" : "");
        if (objectiveText != null) objectiveText.text = "OBJECTIVE: Reach the data beacon";
        if (starConditionText != null)
            starConditionText.text = $"Stars:  1 = complete the level   |   2 = use {level.optimalTokens} commands or fewer   |   3 = no failed runs";
        if (loopRow != null) loopRow.SetActive(level.allowLoops);
        if (conditionalRow != null) conditionalRow.SetActive(level.allowConditionals);
        HideResultPanels();
    }

    // ---------------- Result panels ----------------

    public void ShowWin(int stars, int xpGained, bool isLast, string message)
    {
        HideResultPanels();
        if (winPanel != null) winPanel.SetActive(true);
        if (winStars != null)
            for (int i = 0; i < winStars.Length; i++)
                if (winStars[i] != null) winStars[i].color = i < stars ? StarOn : StarOff;
        if (winMessage != null) winMessage.text = message;
        if (winXPText != null) winXPText.text = xpGained > 0 ? $"XP AWARDED   +{xpGained} XP" : "Already completed - no new XP";
        // Always keep an exit on the win panel: NEXT LEVEL mid-game, MAIN MENU
        // after the final level (both go through GameController.GoNext).
        ShowNextButton(true);
        if (nextLevelButton != null)
        {
            var label = nextLevelButton.GetComponentInChildren<Text>(true);
            if (label != null) label.text = isLast ? "MAIN MENU" : "NEXT LEVEL";
        }
    }

    public void ShowFail(string reason, string tip)
    {
        HideResultPanels();
        if (failPanel != null) failPanel.SetActive(true);
        if (failReason != null) failReason.text = reason;
        if (failTip != null) failTip.text = "TIP: " + tip;
    }

    public void HideResultPanels()
    {
        if (winPanel != null) winPanel.SetActive(false);
        if (failPanel != null) failPanel.SetActive(false);
    }

    public void ShowNextButton(bool show)
    {
        if (nextLevelButton != null) nextLevelButton.gameObject.SetActive(show);
    }

    // ---------------- Status & readouts ----------------

    // Status messages lead with a glyph so success/failure survives without
    // colour: red and green are the pair red-green colour blindness cannot
    // separate, and this line is the game's main feedback channel (WCAG 1.4.1 —
    // colour must never be the only carrier of meaning). Colour-blind mode also
    // swaps that pair for the cyan/amber used on the board.
    string _statusGlyph = ">", _statusMsg = "";
    int _statusKind;   // 0 neutral, 1 good, 2 bad

    // Plain ASCII on purpose: the check/cross marks were not in either project
    // font and came from an OS fallback face, so they rendered in a different
    // typeface (and would vanish on a platform without that fallback). The
    // bracketed forms also suit the game's terminal theme.
    public void SetStatus(string msg) => ShowStatus(">", msg, 0);
    public void SetStatusGood(string msg) => ShowStatus("[OK]", msg, 1);
    public void SetStatusBad(string msg) => ShowStatus("[X]", msg, 2);

    void ShowStatus(string glyph, string msg, int kind)
    {
        _statusGlyph = glyph;
        _statusMsg = msg;
        _statusKind = kind;
        RefreshStatus();
    }

    // Re-applies the current message in the current palette; also called when
    // colour-blind mode is toggled so the visible line updates immediately.
    public void RefreshStatus()
    {
        if (status == null) return;
        status.text = $"{_statusGlyph}  {_statusMsg}";
        bool cb = ColorBlindMode.Enabled;
        status.color = _statusKind switch
        {
            1 => cb ? new Color(0.35f, 0.85f, 1f) : new Color(0.4f, 1f, 0.55f),
            2 => cb ? new Color(1f, 0.6f, 0.1f) : new Color(1f, 0.45f, 0.4f),
            _ => cb ? new Color(0.95f, 0.95f, 0.95f) : new Color(1f, 0.9f, 0.4f)
        };
    }
    public void SetQueue(string text) => queueText.text = text;
    public void RefreshXP() => xpText.text = $"XP: {ProgressManager.TotalXP}";
}
