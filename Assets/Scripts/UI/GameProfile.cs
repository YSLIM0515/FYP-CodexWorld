// Codex World — Player profile store (PlayerPrefs-backed)
// Persists the player's display identity: profile name, chosen avatar icon and
// up to three achievement badges to show on the main-menu profile card.
// Also the single source of truth for the icon/badge options themselves, so the
// ProfilePanel UI, the editor builder, and the card display all stay in sync.
// Keys:
//   cw_prof_name    (string)  display name
//   cw_prof_icon    (int)     index into IconGlyphs/IconColors
//   cw_prof_badges  (string)  csv of badge indices, e.g. "0,2,4" (max 3)

using System.Collections.Generic;
using UnityEngine;

public static class GameProfile
{
    public const int MaxBadges = 3;
    public const string DefaultName = "CODE_MASTER";

    // ---- Avatar icon options (draft placeholders until real art) ----
    public static readonly string[] IconGlyphs = { "</>", "{}", "@", "#", "A*", "&" };
    public static readonly Color[] IconColors =
    {
        new Color(0.45f, 0.85f, 1f),    // cyan
        new Color(0.94f, 0.76f, 0.29f), // gold
        new Color(0.35f, 0.85f, 0.45f), // green
        new Color(0.75f, 0.55f, 0.95f), // purple
        new Color(0.95f, 0.45f, 0.45f), // red
        new Color(0.35f, 0.55f, 0.95f), // blue
    };

    // ---- Badge options (match the achievements screen placeholders) ----
    public static readonly string[] BadgeNames =
        { "Hello World", "Debugger", "Loop Learner", "Efficient Coder", "Logic Master", "Virus Purger" };
    public static readonly string[] BadgeGlyphs = { "</>", "!", "x4", "++", "IF", "@" };
    public static readonly Color[] BadgeColors =
    {
        new Color(0.76f, 0.48f, 0.22f), // bronze
        new Color(0.76f, 0.48f, 0.22f), // bronze
        new Color(0.75f, 0.80f, 0.87f), // silver
        new Color(0.75f, 0.80f, 0.87f), // silver
        new Color(0.94f, 0.76f, 0.29f), // gold
        new Color(0.94f, 0.76f, 0.29f), // gold
    };

    public static string Name = DefaultName;
    public static int Icon;
    public static readonly List<int> Badges = new List<int>();

    public static void Load()
    {
        Name = PlayerPrefs.GetString("cw_prof_name", DefaultName);
        Icon = Mathf.Clamp(PlayerPrefs.GetInt("cw_prof_icon", 0), 0, IconGlyphs.Length - 1);

        Badges.Clear();
        string csv = PlayerPrefs.GetString("cw_prof_badges", "0,2,4");
        foreach (var part in csv.Split(','))
            if (int.TryParse(part, out int i) && i >= 0 && i < BadgeNames.Length && Badges.Count < MaxBadges && !Badges.Contains(i))
                Badges.Add(i);
    }

    public static void Save()
    {
        if (string.IsNullOrWhiteSpace(Name)) Name = DefaultName;
        PlayerPrefs.SetString("cw_prof_name", Name.Trim());
        PlayerPrefs.SetInt("cw_prof_icon", Icon);
        PlayerPrefs.SetString("cw_prof_badges", string.Join(",", Badges));
        PlayerPrefs.Save();
    }
}
