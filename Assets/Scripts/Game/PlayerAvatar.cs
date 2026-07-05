// Codex World — AXIOM avatar (scene-authored)
// AXIOM is a real object in the hierarchy; edit its meshes/materials directly
// in the Scene view. This component only plays back interpreter snapshots and
// the win/fail feedback animations.

using System.Collections;
using UnityEngine;

public class PlayerAvatar : MonoBehaviour
{
    [Header("Playback")]
    [Tooltip("Seconds AXIOM takes to move one tile.")]
    public float moveDuration = 0.32f;
    [Tooltip("Seconds AXIOM takes to rotate 90 degrees.")]
    public float turnDuration = 0.18f;
    [Tooltip("Height of the hop when moving between tiles.")]
    public float hopHeight = 0.25f;

    [Header("References")]
    [Tooltip("Body renderer used for the win/fail emission flash.")]
    public Renderer bodyRenderer;

    Material _bodyMat;
    Color _baseEmission;

    void Awake()
    {
        if (bodyRenderer != null)
        {
            _bodyMat = bodyRenderer.material; // per-instance copy, safe to tint at runtime
            _baseEmission = _bodyMat.HasProperty("_EmissionColor")
                ? _bodyMat.GetColor("_EmissionColor") : Color.black;
        }
    }

    public static Vector3 GridToWorld(Vector2Int cell) => new Vector3(cell.x, 0f, cell.y);
    static float YawOf(Facing facing) => 90f * (int)facing; // North=0, East=90, South=180, West=270

    public void SnapTo(Vector2Int cell, Facing facing)
    {
        StopAllCoroutines();
        transform.position = GridToWorld(cell);
        transform.rotation = Quaternion.Euler(0f, YawOf(facing), 0f);
        if (_bodyMat != null) _bodyMat.SetColor("_EmissionColor", _baseEmission);
    }

    // Animates one snapshot: rotates first if facing changed, then hops to the new cell.
    public IEnumerator AnimateTo(Vector2Int cell, Facing facing)
    {
        var targetRot = Quaternion.Euler(0f, YawOf(facing), 0f);
        if (Quaternion.Angle(transform.rotation, targetRot) > 0.5f)
        {
            var startRot = transform.rotation;
            for (float t = 0f; t < 1f; t += Time.deltaTime / turnDuration)
            {
                transform.rotation = Quaternion.Slerp(startRot, targetRot, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            transform.rotation = targetRot;
        }

        var targetPos = GridToWorld(cell);
        if ((transform.position - targetPos).sqrMagnitude > 0.0001f)
        {
            var startPos = transform.position;
            for (float t = 0f; t < 1f; t += Time.deltaTime / moveDuration)
            {
                float eased = Mathf.SmoothStep(0f, 1f, t);
                var pos = Vector3.Lerp(startPos, targetPos, eased);
                pos.y = Mathf.Sin(eased * Mathf.PI) * hopHeight;
                transform.position = pos;
                yield return null;
            }
            transform.position = targetPos;
        }
    }

    public IEnumerator PlayWin()
    {
        if (_bodyMat != null) _bodyMat.SetColor("_EmissionColor", new Color(0.1f, 0.8f, 0.3f));
        var start = transform.position;
        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.9f)
        {
            transform.rotation = Quaternion.Euler(0f, t * 720f, 0f);
            var pos = start;
            pos.y = Mathf.Abs(Mathf.Sin(t * Mathf.PI * 2f)) * 0.5f;
            transform.position = pos;
            yield return null;
        }
        transform.position = start;
    }

    public IEnumerator PlayFail()
    {
        if (_bodyMat != null) _bodyMat.SetColor("_EmissionColor", new Color(0.9f, 0.1f, 0.1f));
        var start = transform.position;
        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.5f)
        {
            var pos = start;
            pos.x += Mathf.Sin(t * 40f) * 0.06f * (1f - t);
            transform.position = pos;
            yield return null;
        }
        transform.position = start;
    }
}
