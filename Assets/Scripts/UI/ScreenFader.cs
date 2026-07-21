// Codex World — scene transition fader
// Lives on the ScreenFade overlay (black Image + Animator). The Animator plays
// FadeIn automatically on scene load; LoadScene() plays the FadeOut state
// ('Out' trigger), blocks clicks while the screen darkens, then switches
// scene. Falls back to an instant load when no fader exists in the scene.
// Uses unscaled time so the fade still works if the game is paused.

using System.Collections;
using UnityEngine;

public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance;

    [Tooltip("Seconds to wait for the FadeOut animation before loading.")]
    public float fadeOutTime = 0.35f;

    Animator _anim;
    UnityEngine.UI.Image _img;
    bool _leaving;

    void Awake()
    {
        Instance = this;
        _anim = GetComponent<Animator>();
        _img = GetComponent<UnityEngine.UI.Image>();
        if (_anim != null) _anim.updateMode = AnimatorUpdateMode.UnscaledTime;
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    public static void LoadScene(string sceneName)
    {
        if (Instance == null)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
            return;
        }
        if (!Instance._leaving)
            Instance.StartCoroutine(Instance.FadeThenLoad(sceneName));
    }

    IEnumerator FadeThenLoad(string sceneName)
    {
        _leaving = true;
        if (_img != null) _img.raycastTarget = true; // swallow clicks mid-transition
        if (_anim != null) _anim.SetTrigger("Out");
        yield return new WaitForSecondsRealtime(fadeOutTime);
        Time.timeScale = 1f; // in case we left from a paused game
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }
}
