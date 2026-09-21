namespace rlmg.Tools.Core.Examples.Editor
{
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine.SceneManagement;

    [InitializeOnLoad]
    public static class ExampleRLMGConfigLoaderImporter
    {
        static ExampleRLMGConfigLoaderImporter()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {

            if (scene.name.Contains("ExampleRLMGConfigLoader"))
                ImportStreamingAssets();
        }
        private static void ImportStreamingAssets()
        {
            SampleStreamingAssetsImporterUtils.CopySampleFilesToStreamingAssets(
                importerScriptName: nameof(ExampleRLMGConfigLoaderImporter),
                destStreamingAssetsSubFolderName: "RLMG Core Samples/RLMG Config Loader");
        }
    }

}
