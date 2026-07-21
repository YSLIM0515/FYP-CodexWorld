// Codex World — Colour-blind mode
// The default board is hard to read with red-green colour blindness (~8% of
// males): the goal beacon is green and sits on green grass, and the violet
// obstacles read as murky grey. This repaints the three gameplay-critical
// elements onto a blue/orange axis, which survives every common form of colour
// vision deficiency, and separates them by brightness as well as hue so the
// board still reads in full monochromacy:
//
//   obstacles (avoid)  -> bright amber, warm and light
//   goal      (reach)  -> bright cyan, cool and light
//   ground / decor     -> desaturated, so nothing competes with the two above
//
// Renderer.material is used deliberately: it instances the material, so the
// shared .mat assets on disk are never modified. Originals are cached and
// restored when the toggle goes off.

using System.Collections.Generic;
using UnityEngine;

public static class ColorBlindMode
{
    public static readonly Color ObstacleGlow = new Color(1f, 0.55f, 0.05f);
    public static readonly Color ObstacleBody = new Color(0.35f, 0.16f, 0.02f);
    public static readonly Color GoalColor = new Color(0.10f, 0.85f, 1f);
    public static readonly Color GroundTint = new Color(0.72f, 0.76f, 0.85f);

    struct Saved
    {
        public Renderer Renderer;
        public Color BaseColor, Emission;
        public bool HadEmission;
    }

    static readonly List<Saved> _saved = new List<Saved>();
    static readonly Dictionary<CorruptedBlock, Color> _savedGlow = new Dictionary<CorruptedBlock, Color>();
    static bool _on;

    public static bool Enabled => _on;

    public static void Apply(bool on)
    {
        Restore();
        _on = on;
        if (on) PaintScene();

        // The status line encodes success/failure partly by colour, so it has to
        // repaint in both directions — not just when the mode is switched on.
        var ui = Object.FindAnyObjectByType<GameUI>();
        if (ui != null) ui.RefreshStatus();
    }

    static void PaintScene()
    {
        // Obstacles: recolour the body and hand the pulse a new glow colour so
        // CorruptedBlock keeps animating in amber instead of violet.
        foreach (var block in Object.FindObjectsByType<CorruptedBlock>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (!_savedGlow.ContainsKey(block)) _savedGlow[block] = block.glowColor;
            block.glowColor = ObstacleGlow;
            foreach (var r in block.GetComponentsInChildren<Renderer>())
                if (r.GetComponent<ParticleSystem>() == null) Paint(r, ObstacleBody, ObstacleGlow * 0.3f);
        }

        foreach (var marker in Object.FindObjectsByType<CellMarker>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (marker.type != CellType.Goal) continue;
            foreach (var r in marker.GetComponentsInChildren<Renderer>())
                Paint(r, GoalColor, GoalColor * 0.55f);
        }

        // Ground and decor: pull the saturation down so the amber and cyan read
        // as the only meaningful colours on the board.
        foreach (var level in Object.FindObjectsByType<LevelDefinition>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            foreach (var child in new[] { "Tiles", "Decor" })
            {
                var root = level.transform.Find(child);
                if (root == null) continue;
                foreach (var r in root.GetComponentsInChildren<Renderer>())
                    Tint(r, GroundTint);
            }
    }

    // Re-applies the current setting; call after new level objects appear.
    public static void Refresh() => Apply(_on);

    static void Paint(Renderer r, Color baseColor, Color emission)
    {
        if (r == null) return;
        var m = r.material;
        if (m == null) return;
        Cache(r, m);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", baseColor);
        else m.color = baseColor;
        if (m.HasProperty("_EmissionColor"))
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emission);
        }
    }

    static void Tint(Renderer r, Color tint)
    {
        if (r == null) return;
        var m = r.material;
        if (m == null || !m.HasProperty("_BaseColor")) return;
        Cache(r, m);
        var c = m.GetColor("_BaseColor");
        // Desaturate toward luminance, then cool it slightly.
        float lum = c.r * 0.299f + c.g * 0.587f + c.b * 0.114f;
        var flat = Color.Lerp(c, new Color(lum, lum, lum, c.a), 0.55f);
        m.SetColor("_BaseColor", new Color(flat.r * tint.r, flat.g * tint.g, flat.b * tint.b, c.a));
    }

    static void Cache(Renderer r, Material m)
    {
        foreach (var s in _saved) if (s.Renderer == r) return;
        bool hasEmis = m.HasProperty("_EmissionColor");
        _saved.Add(new Saved
        {
            Renderer = r,
            BaseColor = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : m.color,
            Emission = hasEmis ? m.GetColor("_EmissionColor") : Color.black,
            HadEmission = hasEmis
        });
    }

    static void Restore()
    {
        foreach (var s in _saved)
        {
            if (s.Renderer == null) continue;
            var m = s.Renderer.material;
            if (m == null) continue;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", s.BaseColor);
            else m.color = s.BaseColor;
            if (s.HadEmission) m.SetColor("_EmissionColor", s.Emission);
        }
        _saved.Clear();

        foreach (var kv in _savedGlow)
            if (kv.Key != null) kv.Key.glowColor = kv.Value;
        _savedGlow.Clear();
    }
}
