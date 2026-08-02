// Codex World — Profile edit panel (scene-authored)
// Opened by clicking the avatar on the main-menu profile card. Lets the player
// edit their profile name, pick an avatar icon from a few options, and choose
// up to 3 achievement badges to display. SAVE persists via GameProfile and
// updates the profile card; BACK closes without saving (back to the main menu).
// While open, a dark overlay dims the menu behind it (draft stand-in for a real
// blur, which needs a URP render feature + shader once the art pass happens).
// Buttons are wired through their OnClick() lists in the Inspector.

using UnityEngine;
using UnityEngine.UI;

public class ProfilePanel : MonoBehaviour
{
    [Header("Root (the object shown/hidden)")]
    [Tooltip("Object toggled on/off when opening/closing. Defaults to this GameObject.")]
    public GameObject panelRoot;

    [Header("Editing controls")]
    public InputField nameInput;
    [Tooltip("Icon option buttons, in the same order as GameProfile.IconGlyphs.")]
    public Button[] iconOptions;
    [Tooltip("Badge toggles, in the same order as GameProfile.BadgeNames. First 3 ticked are shown.")]
    public Toggle[] badgeToggles;
    [Tooltip("Badge toggle labels - restyled by Open() when a badge is still locked.")]
    public Text[] badgeLabels;
    [Tooltip("Badge icon sprites (ACH_Badge_0..5) shown on the profile card.")]
    public Sprite[] badgeSprites;

    [Header("Buttons")]
    public Button saveButton;
    public Button backButton;

    [Header("Profile card display (main menu)")]
    public Text cardNameText;
    public Image cardAvatar;
    public Text cardAvatarGlyph;
    [Tooltip("Avatar portraits in icon-option order (AV_0..AV_4). Glyph/color fallback is used when empty.")]
    public Sprite[] avatarSprites;
    [Tooltip("Mini badge squares on the profile card (3 slots).")]
    public Image[] cardBadgeSlots;
    public Text[] cardBadgeGlyphs;

    int _selectedIcon;

    void Awake()
    {
        GameProfile.Load();
        RefreshCard();
        Hide();
    }

    // ---------------- OnClick targets ----------------

    // Avatar button: open the editor pre-filled with the saved profile.
    public void Open()
    {
        GameProfile.Load();
        // Show the panel first: an InputField assigned while its GameObject is
        // inactive keeps the value but never redraws its label, so it looks empty.
        Root().SetActive(true);
        if (nameInput) nameInput.text = GameProfile.Name;
        SelectIcon(GameProfile.Icon);
        for (int i = 0; i < badgeToggles.Length; i++)
        {
            if (!badgeToggles[i]) continue;
            bool unlocked = GameProfile.BadgeUnlocked(i);
            badgeToggles[i].interactable = unlocked;
            badgeToggles[i].isOn = unlocked && GameProfile.Badges.Contains(i);
            if (badgeLabels != null && i < badgeLabels.Length && badgeLabels[i])
            {
                badgeLabels[i].text = GameProfile.BadgeNames[i] + (unlocked ? "" : "   [LOCKED]");
                badgeLabels[i].color = unlocked ? GameProfile.BadgeColors[i] : new Color(0.62f, 0.66f, 0.72f);
            }
        }

        transform.SetAsLastSibling();
    }

    // BACK button: discard edits and return to the main menu.
    public void Close() => Hide();

    // Icon option buttons pass their index as the OnClick int argument.
    public void SelectIcon(int index)
    {
        _selectedIcon = Mathf.Clamp(index, 0, GameProfile.IconGlyphs.Length - 1);
        for (int i = 0; i < iconOptions.Length; i++)
        {
            if (!iconOptions[i]) continue;
            var outline = iconOptions[i].GetComponent<Outline>();
            if (outline) outline.enabled = i == _selectedIcon;
        }
    }

    // SAVE button: persist and refresh the profile card display.
    public void Save()
    {
        if (nameInput) GameProfile.Name = nameInput.text;
        GameProfile.Icon = _selectedIcon;

        GameProfile.Badges.Clear();
        for (int i = 0; i < badgeToggles.Length && GameProfile.Badges.Count < GameProfile.MaxBadges; i++)
            if (badgeToggles[i] && badgeToggles[i].isOn && GameProfile.BadgeUnlocked(i))
                GameProfile.Badges.Add(i);

        GameProfile.Save();
        RefreshCard();
        Hide();
    }

    // ---------------- Card display ----------------

    public void RefreshCard()
    {
        if (cardNameText) cardNameText.text = GameProfile.Name;
        if (cardAvatar)
        {
            if (avatarSprites != null && avatarSprites.Length > 0)
            {
                cardAvatar.sprite = avatarSprites[Mathf.Clamp(GameProfile.Icon, 0, avatarSprites.Length - 1)];
                cardAvatar.color = Color.white;
            }
            else cardAvatar.color = GameProfile.IconColors[GameProfile.Icon];
        }
        if (cardAvatarGlyph) cardAvatarGlyph.text = GameProfile.IconGlyphs[GameProfile.Icon];

        for (int slot = 0; slot < cardBadgeSlots.Length; slot++)
        {
            bool used = slot < GameProfile.Badges.Count;
            if (cardBadgeSlots[slot]) cardBadgeSlots[slot].gameObject.SetActive(used);
            if (!used) continue;
            int badge = GameProfile.Badges[slot];
            bool hasArt = badgeSprites != null && badge < badgeSprites.Length && badgeSprites[badge] != null;
            if (cardBadgeSlots[slot])
            {
                cardBadgeSlots[slot].sprite = hasArt ? badgeSprites[badge] : null;
                cardBadgeSlots[slot].color = hasArt ? Color.white : GameProfile.BadgeColors[badge];
            }
            if (slot < cardBadgeGlyphs.Length && cardBadgeGlyphs[slot])
                cardBadgeGlyphs[slot].text = hasArt ? "" : GameProfile.BadgeGlyphs[badge];
        }
    }

    void Hide() => Root().SetActive(false);

    GameObject Root() => panelRoot != null ? panelRoot : gameObject;
}
