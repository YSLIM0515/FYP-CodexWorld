// Codex World — Pixel level number on the profile card
// Renders the player's level using the Number.png digit sprites (Number_0..9)
// instead of plain text, fitted inside the card's level frame. The level comes
// from total XP (same rule as MainMenuController: level = XP / xpPerLevel + 1),
// so it ticks up whenever the player crosses a level-up XP threshold. Refresh()
// runs on enable (the menu reloads on return from a level, so XP is current)
// and can also be called by MainMenuController after awarding XP.

using UnityEngine;
using UnityEngine.UI;

public class LevelNumberDisplay : MonoBehaviour
{
    [Tooltip("Digit slots, left to right. The number is right-aligned into them.")]
    public Image[] digitSlots;
    [Tooltip("Digit sprites 0-9, in order (Number_0 .. Number_9).")]
    public Sprite[] numerals;
    [Tooltip("XP needed per level — keep in sync with MainMenuController.xpPerLevel.")]
    public int xpPerLevel = 5000;
    [Tooltip("Animator on the level badge; its 'Pulse' trigger fires when the level went up since last seen.")]
    public Animator pulseAnimator;

    const string SeenKey = "cw_seen_level";

    void OnEnable() => Refresh();

    public void Refresh()
    {
        int level = xpPerLevel > 0 ? (ProgressManager.TotalXP / xpPerLevel) + 1 : 1;
        SetNumber(level);

        int seen = PlayerPrefs.GetInt(SeenKey, 0);
        if (seen > 0 && level > seen && isActiveAndEnabled)
        {
            // Wait out the scene fade so the pulse is actually visible; the
            // seen-level is only stored once the pulse really fires, so an
            // interrupted pulse retries on the next menu visit.
            StopAllCoroutines();
            StartCoroutine(PulseSoon(level));
            return;
        }
        if (level != seen) PlayerPrefs.SetInt(SeenKey, level);
    }

    System.Collections.IEnumerator PulseSoon(int level)
    {
        yield return new WaitForSeconds(0.6f);
        if (pulseAnimator != null && pulseAnimator.isActiveAndEnabled)
            pulseAnimator.SetTrigger("Pulse");
        PlayerPrefs.SetInt(SeenKey, level);
    }

    public void SetNumber(int value)
    {
        if (digitSlots == null || numerals == null || numerals.Length < 10) return;

        string s = Mathf.Max(0, value).ToString();
        int slots = digitSlots.Length;
        // Right-align: rightmost slot = last digit; unused left slots hide.
        for (int i = 0; i < slots; i++)
        {
            var slot = digitSlots[slots - 1 - i]; // fill from the right
            if (slot == null) continue;
            if (i < s.Length)
            {
                int d = s[s.Length - 1 - i] - '0';
                slot.sprite = numerals[Mathf.Clamp(d, 0, 9)];
                slot.gameObject.SetActive(true);
            }
            else
            {
                // Toggle the object (not just the Image) so a layout group
                // re-centers the remaining digits.
                slot.gameObject.SetActive(false);
            }
        }
    }
}
