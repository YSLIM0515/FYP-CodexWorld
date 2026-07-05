// Codex World — HUD binder (scene-authored UI)
// The canvas, texts, and buttons are real objects in the hierarchy — restyle
// or rearrange them freely in the Scene view. This component just holds
// references to them; each button's behaviour is configured in its OnClick()
// list in the Inspector, targeting GameController's public handlers.

using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    [Header("Texts")]
    public Text title;
    public Text story;
    public Text status;
    public Text queueText;
    public Text xpText;
    public Text levelLabel;

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
    public Button nextLevelButton;
    public Button resetSaveButton;

    [Header("Groups (shown/hidden per level)")]
    public GameObject loopRow;
    public GameObject conditionalRow;

    public void ConfigureForLevel(LevelDefinition level, int index, int total, bool completed)
    {
        if (title != null) title.text = $"{level.regionTitle}  —  {level.levelName}";
        if (story != null) story.text = level.story;
        if (levelLabel != null) levelLabel.text = $"Level {index + 1} / {total}" + (completed ? "  ✓" : "");
        if (loopRow != null) loopRow.SetActive(level.allowLoops);
        if (conditionalRow != null) conditionalRow.SetActive(level.allowConditionals);
        ShowNextButton(false);
    }

    public void ShowNextButton(bool show)
    {
        if (nextLevelButton != null) nextLevelButton.gameObject.SetActive(show);
    }

    public void SetStatus(string msg) { status.text = msg; status.color = new Color(1f, 0.9f, 0.4f); }
    public void SetStatusGood(string msg) { status.text = msg; status.color = new Color(0.4f, 1f, 0.55f); }
    public void SetStatusBad(string msg) { status.text = msg; status.color = new Color(1f, 0.45f, 0.4f); }
    public void SetQueue(string text) => queueText.text = text;
    public void RefreshXP() => xpText.text = $"XP: {ProgressManager.TotalXP}";
}
