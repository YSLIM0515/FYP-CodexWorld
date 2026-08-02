// Codex World — First-run welcome screen (scene-authored)
// Shown once, before the player ever reaches the menu: AXIOM introduces itself
// and asks for a name. Leaving the field blank keeps GameProfile.DefaultName
// ("CODE_MASTER"), so the player can always just press START.
// The profile panel stays what it was — an editor for an existing profile.
//
// When the intro cutscene lands, clear playOnFirstRun and call Show() from the
// end of the cutscene instead.

using UnityEngine;
using UnityEngine.UI;

public class WelcomeScreen : MonoBehaviour
{
    [Header("Root (the object shown/hidden)")]
    public GameObject panelRoot;

    [Header("Controls")]
    public InputField nameInput;
    public Button startButton;

    [Header("Refresh targets")]
    [Tooltip("Profile card on the main menu - refreshed once a name is chosen.")]
    public ProfilePanel profilePanel;
    public MainMenuController mainMenu;

    [Header("Behaviour")]
    [Tooltip("Show automatically on the first run. Turn off once a cutscene calls Show().")]
    public bool playOnFirstRun = true;

    // Decided in Awake, not Start: this component sits on the object it hides,
    // and Start never runs on an inactive GameObject.
    void Awake()
    {
        // Listener first - Show() clears the field, and that has to reach
        // RefreshStartButton to grey START out again.
        if (nameInput) nameInput.onValueChanged.AddListener(_ => RefreshStartButton());

        if (playOnFirstRun && !GameProfile.HasProfile) Show();
        else Hide();
    }

    // START stays dead until there is a name to save, so the player gets an
    // answer to their typing rather than a button that silently does nothing.
    void RefreshStartButton()
    {
        if (startButton)
            startButton.interactable = nameInput && !string.IsNullOrWhiteSpace(nameInput.text);
    }

    public void Show()
    {
        GameProfile.Load();
        // Activate before touching the field: an InputField assigned while its
        // GameObject is inactive keeps the value but never redraws its label.
        Root().SetActive(true);
        transform.SetAsLastSibling();
        if (nameInput)
        {
            nameInput.text = "";
            nameInput.Select();
            nameInput.ActivateInputField();
        }
        RefreshStartButton();
    }

    // START button (OnClick). The button is only interactable once the field has
    // a name, but GameProfile.Save() still falls back to the default if one ever
    // slips through blank.
    public void Confirm()
    {
        GameProfile.Name = nameInput ? nameInput.text : "";
        GameProfile.Save();
        if (profilePanel) profilePanel.RefreshCard();
        if (mainMenu) mainMenu.RefreshProfile();
        Hide();
    }

    void Hide() => Root().SetActive(false);

    GameObject Root() => panelRoot != null ? panelRoot : gameObject;
}
