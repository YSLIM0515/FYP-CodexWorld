// Codex World — The Codex guidebook screen (scene-authored)
// Book layout: entry list on the left (locked pages show ??? + padlock, same
// treatment as the achievements screen), the recovered page on the right with
// WHAT IT IS / IN CODEX WORLD / IN REAL CODE / IN THE REAL WORLD sections.
// Buttons are wired through their OnClick() lists (CODEX menu button -> Open,
// rows -> Select(index), back -> Close).

using UnityEngine;
using UnityEngine.UI;

public class CodexScreen : MonoBehaviour
{
    [Header("Root (the object shown/hidden)")]
    public GameObject panelRoot;
    public Text progressLabel;

    [Header("Entry list (assigned by the scene build)")]
    public Button[] rowButtons;
    public Image[] rowPlates;
    public Image[] rowIcons;
    public Text[] rowNames;
    public GameObject[] rowLocks;

    [Header("Page")]
    public GameObject pageRoot;      // section content (hidden when locked)
    public Image pageIcon;
    public Text pageTitle;
    public Text whatText;
    public Text inGameText;
    public Text codeText;
    public Text realText;
    public GameObject lockedRoot;    // padlock + recovery hint
    public Text lockedHint;

    [Tooltip("Entry icons in CodexLibrary order.")]
    public Sprite[] icons;

    static readonly Color LockedIcon = new Color(0.44f, 0.47f, 0.53f, 1f);
    static readonly Color LockedText = new Color(0.62f, 0.66f, 0.72f, 1f);
    static readonly Color LightText = new Color(0.9f, 0.95f, 1f, 1f);
    static readonly Color SelectedPlate = Color.white;
    static readonly Color IdlePlate = new Color(0.55f, 0.62f, 0.72f, 1f);
    static readonly Color LockedPlate = new Color(0.38f, 0.42f, 0.48f, 1f);

    int _selected;

    void Awake() => Hide();

    // CODEX menu button (OnClick).
    public void Open()
    {
        Refresh();
        Select(_selected);
        Root().SetActive(true);
        transform.SetAsLastSibling();
    }

    // BACK button (OnClick).
    public void Close() => Hide();

    // Entry rows pass their index as the OnClick int argument.
    public void Select(int index)
    {
        _selected = Mathf.Clamp(index, 0, CodexLibrary.Entries.Length - 1);
        var e = CodexLibrary.Entries[_selected];
        bool unlocked = CodexLibrary.IsUnlocked(_selected);

        if (pageIcon)
        {
            if (icons != null && _selected < icons.Length && icons[_selected] != null)
                pageIcon.sprite = icons[_selected];
            pageIcon.color = unlocked ? Color.white : LockedIcon;
        }
        if (pageTitle)
        {
            pageTitle.text = unlocked ? e.Name : "PAGE NOT RECOVERED";
            pageTitle.color = unlocked ? new Color(1f, 0.8f, 0.2f) : LockedText;
        }
        if (pageRoot) pageRoot.SetActive(unlocked);
        if (lockedRoot) lockedRoot.SetActive(!unlocked);
        if (unlocked)
        {
            if (whatText) whatText.text = e.What;
            if (inGameText) inGameText.text = e.InGame;
            if (codeText) codeText.text = e.Code;
            if (realText) realText.text = e.RealWorld;
        }
        else if (lockedHint) lockedHint.text = e.UnlockHint;

        StyleRows();
    }

    public void Refresh()
    {
        if (progressLabel)
            progressLabel.text = CodexLibrary.UnlockedCount() + " / " + CodexLibrary.Entries.Length + " PAGES RECOVERED";

        for (int i = 0; i < CodexLibrary.Entries.Length; i++)
        {
            bool unlocked = CodexLibrary.IsUnlocked(i);
            if (rowIcons != null && i < rowIcons.Length && rowIcons[i])
                rowIcons[i].color = unlocked ? Color.white : LockedIcon;
            if (rowNames != null && i < rowNames.Length && rowNames[i])
            {
                rowNames[i].text = unlocked ? CodexLibrary.Entries[i].Name : "? ? ?";
                rowNames[i].color = unlocked ? LightText : LockedText;
            }
            if (rowLocks != null && i < rowLocks.Length && rowLocks[i])
                rowLocks[i].SetActive(!unlocked);
        }
        StyleRows();
    }

    void StyleRows()
    {
        if (rowPlates == null) return;
        for (int i = 0; i < rowPlates.Length; i++)
        {
            if (!rowPlates[i]) continue;
            bool unlocked = CodexLibrary.IsUnlocked(i);
            rowPlates[i].color = i == _selected ? SelectedPlate : unlocked ? IdlePlate : LockedPlate;
        }
    }

    void Hide() => Root().SetActive(false);

    GameObject Root() => panelRoot != null ? panelRoot : gameObject;
}
