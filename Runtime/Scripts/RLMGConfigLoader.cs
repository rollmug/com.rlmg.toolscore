namespace rlmg.Tools.Core
{
    using rlmg.Tools.ContentLoading;
    using System.Collections;
    using UnityEngine;
    using UnityEngine.Networking;

    [DefaultExecutionOrder(-100)]
    public class RLMGConfigLoader : ContentLoader
    {
        [SerializeField]
        protected AppManager appManager;

        [SerializeField]
        protected AttractVideoPlayer attractVideoPlayer;

        [SerializeField]
        protected AttractTimeout[] attractTimeouts;

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
            Data = JsonUtility.FromJson<RLMGConfigData>(webRequest.downloadHandler.text);

            if (Data == null)
                yield break;

            RLMGLogger.Instance.Configure(
                Data.LogLevel,
                Data.LogLocation,
                Data.logFolderName,
                Data.logFileName,
                Data.logMaxDays,
                Data.doLogFilePerSession,
                Data.doLogDebugLogs,
                Data.DebugLogLevel,
                Data.doLogDebugStackTrace);

            if (appManager != null &&
                Data.displays != null &&
                Data.displays.Length > 0)
            {
                appManager.Configure(
                    doResetResolution: true,
                    displays: Data.displays);
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