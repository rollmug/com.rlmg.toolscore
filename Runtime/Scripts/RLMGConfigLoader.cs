namespace rlmg.Tools.Core
{
    using rlmg.Tools.ContentLoading;
    using System.Collections;
    using UnityEngine;
    using UnityEngine.Networking;
    using Newtonsoft.Json;

    /// <summary>
    /// Configures the logger, attract and app manager components in RLMG Core
    /// </summary>
    /// <remarks>Will Instantiate a RLMGLogger instance if not already present.</remarks>
    [DefaultExecutionOrder(-100)]
    public class RLMGConfigLoader : ContentLoader
    {
        /// <summary>
        /// Optionally present AppManager instance, which this will configure.
        /// </summary>
        [SerializeField]
        protected AppManager appManager;

        /// <summary>
        /// Optionally present AttractVideoPlayer instance, which this will configure.
        /// </summary>
        [SerializeField]
        protected AttractVideoPlayer attractVideoPlayer;

        /// <summary>
        /// Optionally present AttractTimeout instances, which this will configure.
        /// </summary>
        [SerializeField]
        protected AttractTimeout[] attractTimeouts;

        /// <summary>
        /// The configuration data loaded
        /// </summary>
        public RLMGConfigData Data;

        protected override void Awake()
        {
            if (appManager == null)
                appManager = FindAnyObjectByType<AppManager>();

            if (attractVideoPlayer == null)
                attractVideoPlayer = FindAnyObjectByType<AttractVideoPlayer>();

            if (attractTimeouts == null ||
                attractTimeouts.Length == 0)
                attractTimeouts = FindObjectsByType<AttractTimeout>(FindObjectsSortMode.InstanceID);

            base.Awake();
        }

        /// <summary>
        /// Set up our most frequently used components.
        /// The base method does nothing.
        /// </summary>
        /// <param name="webRequest"></param>
        /// <returns></returns>
        protected override IEnumerator OnLocalSuccess(UnityWebRequest webRequest)
        {
            // Using Newtonsoft to support nullable types
            Data = JsonConvert.DeserializeObject<RLMGConfigData>(webRequest.downloadHandler.text);

            if (Data == null)
                yield break;

            if (Data.loggerConfig != null)
                RLMGLogger.Instance.Configure(Data.loggerConfig);

            if (appManager != null &&
                Data.appManagerConfig != null)
            {
                appManager.Configure(Data.appManagerConfig);
            }

            if (attractVideoPlayer != null &&
                !string.IsNullOrEmpty(Data.attractPath))
            {
                attractVideoPlayer.LoadVideo(
                    Data.attractPath);
            }

            if (attractTimeouts != null)
                foreach (var t in attractTimeouts)
                    t.TimeoutDuration = Data.attractTimeoutDuration;
        }
    }

}