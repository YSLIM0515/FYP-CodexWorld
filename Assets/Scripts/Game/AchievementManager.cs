// Codex World — Achievement definitions & persistence
// 18 coding-themed achievements (6 per tier, indexes 0-5 Bronze, 6-11 Silver,
// 12-17 Gold). All locked by default. GameController unlocks them when the
// player meets the condition; AchievementsScreen reads the state to render.
// Keys: cw_ach_{i} (0/1), cw_ach_time_{i} ("yyyy-MM-dd HH:mm"), cw_ach_fails (int).

using UnityEngine;

public static class AchievementManager
{
    public enum Tier { Bronze, Silver, Gold }

    public struct Def
    {
        public string Name, Desc;
        public Tier Tier;
        public Def(string n, string d, Tier t) { Name = n; Desc = d; Tier = t; }
    }

    // Order matches the icon grid in AchievementUI.png (18 icons).
    public static readonly Def[] Defs =
    {
        new Def("Hello World", "Solve your first puzzle.", Tier.Bronze),
        new Def("Debugger", "Solve a level after a failed run.", Tier.Bronze),
        new Def("Turn Around", "Solve a level using a turn command.", Tier.Bronze),
        new Def("Valley Clear", "Complete Sequence Valley.", Tier.Bronze),
        new Def("Minimalist", "Solve a level with the optimal commands.", Tier.Bronze),
        new Def("Quick Thinker", "Solve a level in under 30 seconds.", Tier.Bronze),
        new Def("Loop Learner", "Solve a level using a loop.", Tier.Silver),
        new Def("Efficient Coder", "Solve 3 levels optimally.", Tier.Silver),
        new Def("Refactorer", "Clear the queue, then solve a better way.", Tier.Silver),
        new Def("Factory Foreman", "Complete Loop Factory.", Tier.Silver),
        new Def("Nested Mind", "Use a loop inside a loop.", Tier.Silver),
        new Def("Bug Squasher", "Fail 10 runs - and keep going.", Tier.Silver),
        new Def("Logic Master", "Solve a level using an IF conditional.", Tier.Gold),
        new Def("Algorithm Architect", "Combine loops and conditionals.", Tier.Gold),
        new Def("Virus Purger", "Purge the Null Virus.", Tier.Gold),
        new Def("Perfectionist", "Solve every level optimally.", Tier.Gold),
        new Def("Speed Demon", "Purge the virus within 20 minutes of play.", Tier.Gold),
        new Def("True Coder", "Earn every other achievement.", Tier.Gold),
    };

    public static bool IsUnlocked(int i) => PlayerPrefs.GetInt($"cw_ach_{i}", 0) == 1;
    public static string UnlockTime(int i) => PlayerPrefs.GetString($"cw_ach_time_{i}", "");

    // Returns true only when newly unlocked (so callers can announce it once).
    public static bool Unlock(int i)
    {
        if (i < 0 || i >= Defs.Length || IsUnlocked(i)) return false;
        PlayerPrefs.SetInt($"cw_ach_{i}", 1);
        PlayerPrefs.SetString($"cw_ach_time_{i}", System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
        PlayerPrefs.Save();
        return true;
    }

    public static int UnlockedCount()
    {
        int c = 0;
        for (int i = 0; i < Defs.Length; i++) if (IsUnlocked(i)) c++;
        return c;
    }

    // Most recently unlocked indexes, newest first (timestamp format sorts as text).
    public static int[] Recent(int max)
    {
        var list = new System.Collections.Generic.List<int>();
        for (int i = 0; i < Defs.Length; i++) if (IsUnlocked(i)) list.Add(i);
        list.Sort((a, b) => string.CompareOrdinal(UnlockTime(b), UnlockTime(a)));
        if (list.Count > max) list.RemoveRange(max, list.Count - max);
        return list.ToArray();
    }

    public static int RegisterFail()
    {
        int fails = PlayerPrefs.GetInt("cw_ach_fails", 0) + 1;
        PlayerPrefs.SetInt("cw_ach_fails", fails);
        PlayerPrefs.Save();
        return fails;
    }

    public static void ResetAll()
    {
        for (int i = 0; i < Defs.Length; i++)
        {
            PlayerPrefs.DeleteKey($"cw_ach_{i}");
            PlayerPrefs.DeleteKey($"cw_ach_time_{i}");
        }
        PlayerPrefs.DeleteKey("cw_ach_fails");
        PlayerPrefs.Save();
    }
}
