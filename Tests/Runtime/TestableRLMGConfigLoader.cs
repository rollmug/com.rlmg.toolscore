namespace rlmg.Tools.Core.Tests
{
    /// <summary>RLMGConfigLoader exposing its config location, isolated from scene-wide components, for tests.</summary>
    public class TestableRLMGConfigLoader : RLMGConfigLoader
    {
        public string ConfigFilePath => localContentPath;

        /// <summary>
        /// Call before the GameObject is activated, so Awake doesn't start loading, auto-find
        /// components, or create an AppManager.
        /// </summary>
        public void Setup(string contentDirectory, string fileName, AttractTimeout[] timeouts)
        {
            loadOnAwake = false;
            ContentDirName = contentDirectory;
            contentFileName = fileName;
            doConfigureAppManager = false;
            doAutoFindAttractVideoPlayer = false;
            doAutoFindAttractTimeouts = false;
            attractTimeouts = timeouts;
        }

        public void SetConfigSubdirectory(string subdirectory)
        {
            configSubdirectoryName = subdirectory;
        }
    }
}
