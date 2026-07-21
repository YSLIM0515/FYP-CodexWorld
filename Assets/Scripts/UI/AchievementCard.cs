// Codex World — One achievement card on the achievements screen.
// Holds the card's visual parts; Apply() switches between the locked look
// (grey frame, dimmed icon, padlock) and the unlocked look (tier frame,
// full-color icon). Driven by AchievementsScreen.Refresh().

using UnityEngine;
using UnityEngine.UI;

public class AchievementCard : MonoBehaviour
{
    [Tooltip("Index into AchievementManager.Defs.")]
    public int index;

    public Image frame;
    public Sprite lockedFrameSprite;
    public Sprite unlockedFrameSprite;   // tier-colored frame
    public Image icon;
    public GameObject padlock;
    public Text nameText;
    public Text descText;
    public Color unlockedNameColor = new Color(1f, 0.8f, 0.2f);

    // Light enough for ~4.5:1 contrast on the dark card (WCAG 1.4.3) while
    // still clearly reading as "locked" next to unlocked cards.
    static readonly Color LockedIcon = new Color(0.44f, 0.47f, 0.53f, 1f);
    static readonly Color LockedText = new Color(0.68f, 0.72f, 0.78f, 1f);
    static readonly Color LightText = new Color(0.88f, 0.94f, 1f, 1f);

    public void Apply(bool unlocked)
    {
        if (frame != null) frame.sprite = unlocked ? unlockedFrameSprite : lockedFrameSprite;
        if (icon != null) icon.color = unlocked ? Color.white : LockedIcon;
        if (padlock != null) padlock.SetActive(!unlocked);
        if (nameText != null) nameText.color = unlocked ? unlockedNameColor : LockedText;
        if (descText != null) descText.color = unlocked ? LightText : LockedText;
    }
}
