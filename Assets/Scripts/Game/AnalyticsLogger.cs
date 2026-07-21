// Codex World — Learning analytics logger
// Appends one CSV row per run (win or fail) to codexworld_analytics.csv in
// Application.persistentDataPath. Data feeds the FYP evaluation chapter:
// attempts per level, solve time, token counts vs optimal, loop/IF adoption,
// hint usage and star ratings. Fails silently (a full disk must never break
// gameplay); the file can be opened directly in Excel.

using System.IO;
using UnityEngine;

public static class AnalyticsLogger
{
    static string FilePath => Path.Combine(Application.persistentDataPath, "codexworld_analytics.csv");
    const string Header = "timestamp,mode,level,level_name,result,attempt,time_sec,tokens,optimal_tokens,used_loop,used_if,hints_used,stars";

    public static void LogRun(bool challenge, int level, string levelName, string result,
        int attempt, float timeSec, int tokens, int optimalTokens,
        bool usedLoop, bool usedIf, int hintsUsed, int stars)
    {
        try
        {
            if (!File.Exists(FilePath))
                File.AppendAllText(FilePath, Header + "\n");
            string line = string.Join(",",
                System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                challenge ? "challenge" : "beginner",
                (level + 1).ToString(),
                "\"" + (levelName ?? "").Replace("\"", "'") + "\"",
                result,
                attempt.ToString(),
                timeSec.ToString("F1"),
                tokens.ToString(),
                optimalTokens.ToString(),
                usedLoop ? "1" : "0",
                usedIf ? "1" : "0",
                hintsUsed.ToString(),
                stars.ToString());
            File.AppendAllText(FilePath, line + "\n");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Codex World: analytics write failed — " + e.Message);
        }
    }
}
