using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Caveman
{
    /// <summary>
    /// Headless standalone build. Run with Unity CLOSED:
    ///   Unity -batchmode -quit -projectPath &lt;proj&gt; -executeMethod Caveman.GameBuilder.BuildWindows -logFile &lt;log&gt;
    /// Produces a self-contained Windows player the user can run like any other game. Its saves live in the
    /// standalone's own persistentDataPath (AppData\LocalLow\...), independent of the dev project — so ongoing
    /// code changes never touch an installed copy or its saves.
    /// </summary>
    public static class GameBuilder
    {
        const string Scene = "Assets/UnitySave.unity";
        const string OutDirDefault = @"C:\Users\charl\Downloads\CavemanPrototype";
        const string Exe = "CavemanPrototype.exe";

        public static void BuildWindows()
        {
            // Output folder: env var CAVEMAN_BUILD_OUT if set (so I can retarget without editing code), else Downloads.
            string outDir = System.Environment.GetEnvironmentVariable("CAVEMAN_BUILD_OUT");
            if (string.IsNullOrEmpty(outDir)) outDir = OutDirDefault;
            // Identity drives the window title + the save-file folder (AppData\LocalLow\<company>\<product>).
            PlayerSettings.companyName = "CavemanDev";
            PlayerSettings.productName = "Caveman Prototype";

            // Make the game scene the startup + index 0 (so GameMenu's "Restart" scene-reload works in the build).
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Scene, true) };

            Directory.CreateDirectory(outDir);
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { Scene },
                locationPathName = Path.Combine(outDir, Exe),
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None,
            };

            Debug.Log($"[GameBuilder] Building to {opts.locationPathName} ...");
            BuildReport report = BuildPipeline.BuildPlayer(opts);
            var s = report.summary;
            if (s.result == BuildResult.Succeeded)
                Debug.Log($"[GameBuilder] BUILD SUCCEEDED -> {opts.locationPathName}  ({s.totalSize / (1024 * 1024)} MB, {s.totalTime.TotalSeconds:F0}s)");
            else
                Debug.LogError($"[GameBuilder] BUILD FAILED: result={s.result} errors={s.totalErrors}");
        }
    }
}
