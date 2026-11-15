using System;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace NoRegRats
{
    /// <summary>
    /// Builds the Windows executable of Level1 (the whole game) into Desktop/NoRegRats_Draft/Build
    /// and zips it for the submission.
    /// </summary>
    public static class BuildTool
    {
        const string LevelScene = "Assets/Scenes/Level1.unity";

        [MenuItem("Tools/NoRegRats/Build Windows")]
        public static void BuildWindows()
        {
            string buildFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "NoRegRats_Draft", "Build");
            string gameFolder = Path.Combine(buildFolder, "NoRegRats");
            string zipPath = Path.Combine(buildFolder, "NoRegRats_Windows.zip");
            Directory.CreateDirectory(buildFolder);
            if (Directory.Exists(gameFolder))
                Directory.Delete(gameFolder, true);

            var options = new BuildPlayerOptions
            {
                scenes = new[] { LevelScene },
                locationPathName = Path.Combine(gameFolder, "NoRegRats.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };
            BuildSummary summary = BuildPipeline.BuildPlayer(options).summary;
            if (summary.result != BuildResult.Succeeded)
            {
                Debug.LogError("Build failed: " + summary.result + " (" + summary.totalErrors + " errors, see the Console).");
                return;
            }

            if (File.Exists(zipPath))
                File.Delete(zipPath);
            ZipFile.CreateFromDirectory(gameFolder, zipPath);
            Debug.Log("Build done: " + gameFolder + "\nZip: " + zipPath);
            EditorUtility.RevealInFinder(zipPath);
        }
    }
}
