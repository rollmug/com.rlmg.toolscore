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

        [SerializeField]
        protected bool doAutoFindAppManager = true;

        /// <summary>
        /// Optionally present AttractVideoPlayer instance, which this will configure.
        /// </summary>
        [SerializeField]
        protected AttractVideoPlayer attractVideoPlayer;

        [SerializeField]
        protected bool doAutoFindAttractVideoPlayer = true;

        /// <summary>
        /// Optionally present AttractTimeout instances, which this will configure.
        /// </summary>
        [SerializeField]
        protected AttractTimeout[] attractTimeouts;

        [SerializeField]
        protected bool doAutoFindAttractTimeouts = true;

        /// <summary>
        /// The configuration data loaded
        /// </summary>
        public RLMGConfigData Data;

        protected override void Awake()
        {
            if (appManager == null && doAutoFindAppManager)
                appManager = FindAnyObjectByType<AppManager>();

            if (attractVideoPlayer == null && doAutoFindAttractVideoPlayer)
                attractVideoPlayer = FindAnyObjectByType<AttractVideoPlayer>();

            if ((attractTimeouts == null ||
                attractTimeouts.Length == 0) &&
                doAutoFindAttractTimeouts)
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

            if (Data != null)
                ApplyConfigData(Data);

            yield break;
        }

        /// <summary>
        /// Applies loaded config data to the logger, attract and app manager components in RLMG Core.
        /// </summary>
        protected virtual void ApplyConfigData(RLMGConfigData data)
        {
            if (data.loggerConfig != null)
                RLMGLogger.Instance.Configure(data.loggerConfig);

            if (appManager != null &&
                data.appManagerConfig != null)
            {
                appManager.Configure(data.appManagerConfig);
            }

            if (attractVideoPlayer != null &&
                !string.IsNullOrEmpty(data.attractPath))
            {
                attractVideoPlayer.LoadVideo(
                    data.attractPath);
            }

            if (attractTimeouts != null)
                foreach (var t in attractTimeouts)
                    t.TimeoutDuration = data.attractTimeoutDuration;
        }
    }

}