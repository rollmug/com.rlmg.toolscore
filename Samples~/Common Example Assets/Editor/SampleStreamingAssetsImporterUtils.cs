namespace rlmg.Tools.Core.Examples.Editor
{
    using System.IO;
    using UnityEditor;
    using UnityEngine;

    public static class SampleStreamingAssetsImporterUtils
    {
        public static void CopySampleFilesToStreamingAssets(
            string importerScriptName,
            string hiddenStreamingAssetsDirName = "HiddenStreamingAssets~",
            string destStreamingAssetsSubFolderName = "YourPackageSampleName"
            )
        {
            // 1. Locate where the sample was imported in the project
            // This safely resolves the path whether imported via Package Manager or placed locally
            string[] guids = AssetDatabase.FindAssets(importerScriptName + " t:Script");
            if (guids.Length == 0) return;

            string scriptPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            string sampleFolder = Path.GetDirectoryName(Path.GetDirectoryName(scriptPath));

            // 2. Define source and destination paths
            string sourcePath = Path.Combine(sampleFolder, hiddenStreamingAssetsDirName);
            string destPath = Path.Combine(Application.dataPath, "StreamingAssets", destStreamingAssetsSubFolderName);

            if (!Directory.Exists(sourcePath)) return;

            // 3. Perform copy if not already copied (prevents repeating every assembly reload)
            if (!Directory.Exists(destPath))
            {
                Directory.CreateDirectory(destPath);
                CopyFilesRecursively(sourcePath, destPath);

                // Refresh the AssetDatabase so Unity acknowledges the new files
                AssetDatabase.Refresh();
                Debug.Log($"[Package Sample] Successfully auto-imported streaming assets to: {destPath}");
            }
        }

        private static void CopyFilesRecursively(string sourceDir, string targetDir)
        {
            foreach (string dirPath in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(sourceDir, targetDir));
            }

            foreach (string newPath in Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories))
            {
                // Skip any accidental meta files in the source folder
                if (newPath.EndsWith(".meta")) continue;

                File.Copy(newPath, newPath.Replace(sourceDir, targetDir), true);
            }
        }
    }

}