using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PixelSurvivor.EditorTools
{
    /// <summary>One-click Windows build into &lt;project&gt;/Builds/Windows.</summary>
    public static class WindowsBuild
    {
        private const string OutputDir = "Builds/Windows";
        private const string ExeName = "PixelSurvivor.exe";
        private static readonly string[] Scenes =
        {
            "Assets/_Project/Scenes/MainMenu.unity",
            "Assets/_Project/Scenes/Game.unity",
        };

        [MenuItem("PixelSurvivor/Build Windows (.exe)", priority = 20)]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("PixelSurvivor", "Exit Play mode first.", "OK");
                return;
            }

            string[] missing = Scenes.Where(s => AssetDatabase.LoadMainAssetAtPath(s) == null).ToArray();
            if (missing.Length > 0)
            {
                EditorUtility.DisplayDialog("PixelSurvivor",
                    "Run 'PixelSurvivor > Build Prototype' first. Missing:\n" + string.Join("\n", missing), "OK");
                return;
            }

            string path = Path.Combine(OutputDir, ExeName);
            var options = new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = path,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[PixelSurvivor] Windows build done: {Path.GetFullPath(path)} " +
                          $"({summary.totalSize / (1024f * 1024f):0.0} MB, {summary.totalTime.TotalSeconds:0}s)");
                EditorUtility.RevealInFinder(path);
            }
            else
            {
                Debug.LogError($"[PixelSurvivor] Windows build {summary.result}: {summary.totalErrors} error(s). See the Console.");
            }
        }
    }
}
