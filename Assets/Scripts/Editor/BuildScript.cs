using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using System.IO;

public static class BuildScript
{
    [MenuItem("Build/Build Standalone Windows (OasisSurvival)")]
    public static void BuildStandaloneWindows()
    {
        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        string buildFolder = Path.Combine(projectRoot, "OasisSurvival");
        if (!Directory.Exists(buildFolder))
        {
            Directory.CreateDirectory(buildFolder);
        }

        string exePath = Path.Combine(buildFolder, "Oasis Survival.exe");
        string scenePath = "Assets/Scenes/Oasis/Oasis Survival.unity";
        if (!File.Exists(Path.Combine(projectRoot, scenePath)))
        {
            scenePath = "Assets/Scenes/Oasis Survival.unity";
        }

        Debug.Log($"[BuildScript] Starting build for scene: {scenePath}");
        Debug.Log($"[BuildScript] Target output: {exePath}");

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[] { scenePath },
            locationPathName = exePath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[BuildScript] BUILD SUCCEEDED! Total time: {summary.totalTime.TotalSeconds:F1}s, Size: {summary.totalSize / (1024f * 1024f):F2} MB");
            Debug.Log($"[BuildScript] Output directory: {buildFolder}");
        }
        else
        {
            Debug.LogError($"[BuildScript] BUILD FAILED! Total errors: {summary.totalErrors}");
            foreach (var step in report.steps)
            {
                foreach (var msg in step.messages)
                {
                    if (msg.type == LogType.Error || msg.type == LogType.Exception)
                    {
                        Debug.LogError($"[BuildScript Error] {msg.content}");
                    }
                }
            }
        }
    }
}
