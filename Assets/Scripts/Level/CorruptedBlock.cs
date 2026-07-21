// Codex World — Corrupted data block / virus crystal (obstacle look)


using UnityEngine;

[RequireComponent(typeof(CellMarker))]
public class CorruptedBlock : MonoBehaviour
{
    [Header("Corruption glow")]
    public Renderer[] bodyRenderers;
    public Color glowColor = new Color(0.72f, 0.25f, 1f);
    public float minGlow = 0.2f;
    public float maxGlow = 0.5f;
    public float pulseSpeed = 1.3f;

    [Header("Idle motion")]
    public float hoverHeight = 0.05f;
    public float hoverSpeed = 1.2f;
    public float spinDegreesPerSecond = 10f;

    Material[] _mats;
    Vector3 _basePos;
    float _phase;

    void Awake() => CacheMats();

    void OnEnable()
    {
        _basePos = transform.position;
        _phase = Mathf.Abs(Mathf.Sin(_basePos.x * 12.9898f + _basePos.z * 78.233f) * 43758.5453f) % (Mathf.PI * 2f);
    }

    void CacheMats()
    {
        if (bodyRenderers == null || bodyRenderers.Length == 0)
            bodyRenderers = GetComponentsInChildren<Renderer>();
        var mats = new System.Collections.Generic.List<Material>();
        foreach (var r in bodyRenderers)
            if (r != null && r.GetComponent<ParticleSystem>() == null) mats.Add(r.material);
        _mats = mats.ToArray();
    }

    void Update()
    {
        float t = Time.time * pulseSpeed + _phase;

        float glow = Mathf.Lerp(minGlow, maxGlow, (Mathf.Sin(t) + 1f) * 0.5f);
        foreach (var m in _mats)
            if (m != null && m.HasProperty("_EmissionColor"))
                m.SetColor("_EmissionColor", glowColor * glow);

        var pos = _basePos;
        pos.y += Mathf.Sin(Time.time * hoverSpeed + _phase) * hoverHeight;
        transform.position = pos;
        transform.Rotate(Vector3.up, spinDegreesPerSecond * Time.deltaTime, Space.World);
    }

    // Reshuffle the existing crystal shards for per-obstacle variety. Used by the
    // challenge level builder so cloned obstacles are not identical copies.
    public void Randomize(int seed)
    {
        var crystal = transform.Find("Crystal");
        if (crystal == null) return;
        var rng = new System.Random(seed);

        crystal.localRotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
        crystal.localScale = Vector3.one * (0.85f + (float)rng.NextDouble() * 0.45f);

        var live = new System.Collections.Generic.List<Renderer>();
        for (int i = 0; i < crystal.childCount; i++)
        {
            var shard = crystal.GetChild(i);
            bool main = i == 0;
            bool show = main || rng.NextDouble() > 0.22;   // occasionally drop a shard
            shard.gameObject.SetActive(show);
            if (!show) continue;

            float tilt = main ? (float)rng.NextDouble() * 8f : 12f + (float)rng.NextDouble() * 30f;
            shard.localRotation = Quaternion.AngleAxis((float)rng.NextDouble() * 360f, Vector3.up)
                                * Quaternion.AngleAxis(tilt, Vector3.forward);
            float h = main ? 0.7f + (float)rng.NextDouble() * 0.25f : 0.3f + (float)rng.NextDouble() * 0.4f;
            float w = 0.7f + (float)rng.NextDouble() * 0.5f;
            shard.localScale = new Vector3(w, h, w);

            var r = shard.GetComponent<Renderer>();
            if (r != null) live.Add(r);
        }
        bodyRenderers = live.ToArray();
        CacheMats();
    }
}
