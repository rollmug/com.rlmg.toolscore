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

            // 3. Copy only files not already there - so nothing is repeated every assembly reload, local
            // edits are never overwritten, and files a newer sample version adds (or moves into a
            // subdirectory) still arrive in a previously imported destination
            int copiedCount = CopyMissingFilesRecursively(sourcePath, destPath);

            if (copiedCount > 0)
            {
                // Refresh the AssetDatabase so Unity acknowledges the new files
                AssetDatabase.Refresh();
                Debug.Log($"[Package Sample] Successfully auto-imported {copiedCount} streaming asset(s) to: {destPath}");
            }
        }

        private static int CopyMissingFilesRecursively(string sourceDir, string targetDir)
        {
            int copiedCount = 0;

            foreach (string newPath in Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories))
            {
                // Skip any accidental meta files in the source folder
                if (newPath.EndsWith(".meta")) continue;

                string targetPath = newPath.Replace(sourceDir, targetDir);
                if (File.Exists(targetPath)) continue;

                Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                File.Copy(newPath, targetPath);
                copiedCount++;
            }

            return copiedCount;
        }
    }

}