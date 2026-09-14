// Codex World — Learning analytics logger

using System.IO;
using UnityEngine;

public static class AnalyticsLogger
{
    static string FilePath => Path.Combine(Application.persistentDataPath, "codexworld_analytics.csv");
    const string Header = "timestamp,participant,mode,level,level_name,result,attempt,time_sec,tokens,optimal_tokens,used_loop,used_if,hints_used,stars";

    public static void LogRun(bool challenge, int level, string levelName, string result,
        int attempt, float timeSec, int tokens, int optimalTokens,
        bool usedLoop, bool usedIf, int hintsUsed, int stars)
    {
        try
        {
            EnsureHeader();

            // Read straight from PlayerPrefs rather than GameProfile.Name so the
            // column is correct no matter which scene loaded first.
            string participant = PlayerPrefs.GetString("cw_prof_name", "unknown");

            string line = string.Join(",",
                System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                "\"" + participant.Replace("\"", "'") + "\"",
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
            Debug.LogWarning("Codex World: analytics write failed - " + e.Message);
        }
    }

    // A file written before the participant column existed has one fewer field,
    // so appending to it would misalign every later row. Retire it under a dated
    // name instead and start a clean file.
    static void EnsureHeader()
    {
        if (!File.Exists(FilePath))
        {
            File.AppendAllText(FilePath, Header + "\n");
            return;
        }

        using (var reader = new StreamReader(FilePath))
        {
            if (reader.ReadLine() == Header) return;
        }

        string retired = Path.Combine(
            Application.persistentDataPath,
            "codexworld_analytics_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv");
        File.Move(FilePath, retired);
        File.AppendAllText(FilePath, Header + "\n");
        Debug.Log("Codex World: analytics schema changed - previous log kept as " + Path.GetFileName(retired));
    }
}
