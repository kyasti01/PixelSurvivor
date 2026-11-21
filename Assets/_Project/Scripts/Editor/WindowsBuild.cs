using System.IO;
using System.IO.Compression;
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
                string zip = CreateShareableZip();
                Debug.Log($"[PixelSurvivor] Windows build done: {Path.GetFullPath(path)} " +
                          $"({summary.totalSize / (1024f * 1024f):0.0} MB, {summary.totalTime.TotalSeconds:0}s). " +
                          $"Send this zip to other players: {Path.GetFullPath(zip)}");
                EditorUtility.RevealInFinder(zip);
            }
            else
            {
                Debug.LogError($"[PixelSurvivor] Windows build {summary.result}: {summary.totalErrors} error(s). See the Console.");
            }
        }

        /// <summary>
        /// Zips the build into Builds/PixelSurvivor_Windows.zip, leaving out Unity's debug folders
        /// ("..._DontShip...") that must not be distributed. Everything sits in a PixelSurvivor/ folder
        /// inside the zip, so players extract it and run PixelSurvivor.exe.
        /// </summary>
        private static string CreateShareableZip()
        {
            string zipPath = Path.Combine(Path.GetDirectoryName(OutputDir) ?? "Builds", "PixelSurvivor_Windows.zip");
            if (File.Exists(zipPath)) File.Delete(zipPath);

            string root = Path.GetFullPath(OutputDir);
            using ZipArchive zip = ZipFile.Open(zipPath, ZipArchiveMode.Create);
            foreach (string file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
            {
                string relative = file.Substring(root.Length + 1);
                if (relative.Contains("DontShip") || relative.Contains("DoNotShip")) continue;
                zip.CreateEntryFromFile(file, "PixelSurvivor/" + relative.Replace('\\', '/'), System.IO.Compression.CompressionLevel.Optimal);
            }
            return zipPath;
        }
    }
}
