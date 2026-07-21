// Codex World — Unlock toast banner
// Slide-in banner announcing achievement and codex unlocks the moment they
// happen, instead of leaving them to be discovered in the menus. Achievements
// call Announce() directly (AchievementManager.Unlock returns true exactly
// once). Codex entries unlock by rule, not by event, so CheckCodex() diffs the
// live rules against a seen-set in PlayerPrefs (cw_codex_seen_{i}); the first
// ever check only records the current state (cw_codex_base) so an existing
// save is not spammed with old entries.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UnlockToast : MonoBehaviour
{
    public static UnlockToast Instance;

    public CanvasGroup group;
    public Text headerText;
    public Text titleText;
    public float slideTime = 0.25f;
    public float holdTime = 2.2f;
    public float shownY = -70f;
    public float hiddenY = 90f;

    public static readonly Color AchievementGold = new Color(1f, 0.8f, 0.25f);
    public static readonly Color CodexBlue = new Color(0.5f, 0.8f, 1f);

    readonly Queue<(string header, string title, Color color)> _pending = new Queue<(string, string, Color)>();
    bool _showing;
    RectTransform _rt;

    void Awake()
    {
        Instance = this;
        _rt = (RectTransform)transform;
        if (group != null) { group.alpha = 0f; group.blocksRaycasts = false; group.interactable = false; }
    }

    IEnumerator Start()
    {
        yield return new WaitForSeconds(0.8f); // let the scene fade finish first
        CheckCodex();
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    public static void Announce(string header, string title, Color color)
    {
        if (Instance == null) return;
        Instance._pending.Enqueue((header, title, color));
        if (!Instance._showing) Instance.StartCoroutine(Instance.Pump());
    }

    public static void CheckCodex()
    {
        bool baseline = PlayerPrefs.GetInt("cw_codex_base", 0) == 0;
        for (int i = 0; i < CodexLibrary.Entries.Length; i++)
        {
            if (!CodexLibrary.IsUnlocked(i) || PlayerPrefs.GetInt($"cw_codex_seen_{i}", 0) == 1)
                continue;
            PlayerPrefs.SetInt($"cw_codex_seen_{i}", 1);
            if (!baseline)
                Announce("NEW CODEX ENTRY", CodexLibrary.Entries[i].Name, CodexBlue);
        }
        if (baseline) PlayerPrefs.SetInt("cw_codex_base", 1);
    }

    IEnumerator Pump()
    {
        _showing = true;
        while (_pending.Count > 0)
        {
            var (header, title, color) = _pending.Dequeue();
            if (headerText != null) { headerText.text = header; headerText.color = color; }
            if (titleText != null) titleText.text = title;
            yield return Slide(hiddenY, shownY, 0f, 1f);
            yield return new WaitForSeconds(holdTime);
            yield return Slide(shownY, hiddenY, 1f, 0f);
        }
        _showing = false;
    }

    IEnumerator Slide(float fromY, float toY, float fromA, float toA)
    {
        for (float t = 0f; t < slideTime; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / slideTime);
            _rt.anchoredPosition = new Vector2(_rt.anchoredPosition.x, Mathf.Lerp(fromY, toY, k));
            if (group != null) group.alpha = Mathf.Lerp(fromA, toA, k);
            yield return null;
        }
        _rt.anchoredPosition = new Vector2(_rt.anchoredPosition.x, toY);
        if (group != null) group.alpha = toA;
    }
}
