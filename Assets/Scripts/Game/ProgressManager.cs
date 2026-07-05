// Codex World — Progress & XP persistence
// Lightweight save system on top of PlayerPrefs, per the proposal's
// "Save and State Management System". Keys:
//   cw_xp        (int)  total XP earned
//   cw_done_{i}  (0/1)  level i completed
//   cw_bonus_{i} (0/1)  level i completed at/below the optimal token count
//   cw_unlocked  (int)  highest unlocked level index

using UnityEngine;

public static class ProgressManager
{
    // Defaults; overridden by GameController's Inspector properties on startup.
    public static int BaseXP = 100;
    public static int BonusXP = 50;
    public static int LevelCount = 9;

    public static int TotalXP => PlayerPrefs.GetInt("cw_xp", 0);
    public static int HighestUnlocked => PlayerPrefs.GetInt("cw_unlocked", 0);
    public static bool IsCompleted(int level) => PlayerPrefs.GetInt($"cw_done_{level}", 0) == 1;
    public static bool HasBonus(int level) => PlayerPrefs.GetInt($"cw_bonus_{level}", 0) == 1;

    // Awards XP for a completion and unlocks the next level.
    // Base XP is granted once per level; bonus XP once, whenever first achieved.
    public static int CompleteLevel(int level, bool earnedBonus)
    {
        int gained = 0;

        if (!IsCompleted(level))
        {
            gained += BaseXP;
            PlayerPrefs.SetInt($"cw_done_{level}", 1);
        }
        if (earnedBonus && !HasBonus(level))
        {
            gained += BonusXP;
            PlayerPrefs.SetInt($"cw_bonus_{level}", 1);
        }
        if (gained > 0)
            PlayerPrefs.SetInt("cw_xp", TotalXP + gained);

        if (level + 1 < LevelCount && level + 1 > HighestUnlocked)
            PlayerPrefs.SetInt("cw_unlocked", level + 1);

        PlayerPrefs.Save();
        return gained;
    }

    public static void ResetAll()
    {
        PlayerPrefs.DeleteKey("cw_xp");
        PlayerPrefs.DeleteKey("cw_unlocked");
        for (int i = 0; i < LevelCount; i++)
        {
            PlayerPrefs.DeleteKey($"cw_done_{i}");
            PlayerPrefs.DeleteKey($"cw_bonus_{i}");
        }
        PlayerPrefs.Save();
    }
}
