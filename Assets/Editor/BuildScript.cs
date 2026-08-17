// Codex World — headless Windows build entry point
// Run from the command line with Unity closed:
//   Unity.exe -quit -batchmode -nographics -projectPath <project> \
//             -executeMethod BuildScript.BuildWindows -logFile -
// Scene list and order come from File > Build Settings, so this stays in sync
// with whatever the project is configured to ship.

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildScript
{
    const string OutputPath = "Build/Windows/FYP-CodexWorld.exe";

    public static void BuildWindows()
    {
        var scenes = new List<string>();
        foreach (var s in EditorBuildSettings.scenes)
            if (s.enabled) scenes.Add(s.path);

        if (scenes.Count == 0)
        {
            Debug.LogError("BUILD ABORTED: no enabled scenes in Build Settings.");
            EditorApplication.Exit(1);
            return;
        }

        Debug.Log("BUILD SCENES: " + string.Join(", ", scenes));

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes.ToArray(),
            locationPathName = OutputPath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        });

        var summary = report.summary;
        Debug.Log($"BUILD RESULT: {summary.result} | size {summary.totalSize} bytes | " +
                  $"errors {summary.totalErrors} | warnings {summary.totalWarnings} | took {summary.totalTime}");

        EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
    }
}
