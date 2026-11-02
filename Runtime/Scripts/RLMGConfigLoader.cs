namespace rlmg.Tools.Core
{
    using rlmg.Tools.ContentLoading;
    using System.Collections;
    using System.IO;
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
        [Header("RLMG Config Loading Settings")]
        /// <summary>
        /// Optional subdirectory of LocalContentDirectory that the config file (contentFileName) is
        /// resolved from. Blank = LocalContentDirectory itself.
        /// </summary>
        [Tooltip("Optional subdirectory of LocalContentDirectory that the config file (contentFileName) is resolved from. Blank = LocalContentDirectory itself.")]
        [SerializeField]
        protected string configSubdirectoryName = "config";

        [SerializeField]
        protected bool doConfigureAppManager = true;

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

        /// <summary>
        /// Name of the optional subdirectory of LocalContentDirectory that the config file is resolved
        /// from. See <see cref="configSubdirectoryName"/>.
        /// </summary>
        public virtual string ConfigSubdirectoryName => configSubdirectoryName;

        /// <summary>
        /// Path to the config file: LocalContentDirectory, plus configSubdirectoryName if set, plus contentFileName.
        /// </summary>
        protected override string localContentPath
        {
            get
            {
                return FileLoadingUtility.CombineWithOptionalSubdirectory(
                        LocalContentDirectory,
                        configSubdirectoryName,
                        contentFileName);
            }
        }

        protected override void Awake()
        {
            if (!AppManager.HasInstance() && doConfigureAppManager)
                _ = AppManager.Instance;

            if (attractVideoPlayer == null && doAutoFindAttractVideoPlayer)
                attractVideoPlayer = FindAnyObjectByType<AttractVideoPlayer>();

            if ((attractTimeouts == null ||
                attractTimeouts.Length == 0) &&
                doAutoFindAttractTimeouts)
                attractTimeouts = FindObjectsByType<AttractTimeout>(FindObjectsSortMode.InstanceID);

            base.Awake();
        }

        /// <summary>
        /// Adds clearing of loaded data every time loading is run
        /// </summary>
        /// <returns></returns>

        protected override IEnumerator MainLoadContent()
        {
            // clear loaded data
            Data = null; 

            return base.MainLoadContent();
        }

        /// <summary>
        /// Parses the config and, with good data, sets up our most frequently used components. If it's
        /// blank or fails to parse, logs an error and the load reports Failed (and AnyLoadFailed fires) -
        /// logged rather than thrown, so loading still finishes. The file itself is left as-is.
        /// </summary>
        /// <param name="webRequest"></param>
        /// <returns></returns>
        protected override IEnumerator OnLocalSuccess(UnityWebRequest webRequest)
        {
            if (!TryParseLoadedConfig(webRequest.downloadHandler.text, out RLMGConfigData data, out string message))
            {
                Debug.LogError(message + "\nThe file is left as-is - fix or delete it.");
                SetStatus(LoadStatus.Failed, "Failed: " + message);
                yield break;
            }

            Data = data;
            ApplyConfigData(Data);
        }

        /// <summary>
        /// Parses loaded config file text as <see cref="RLMGConfigData"/>. Override to parse as a
        /// subclass, via <see cref="TryParseConfig"/>.
        /// </summary>
        protected virtual bool TryParseLoadedConfig(string text, out RLMGConfigData data, out string message)
        {
            return TryParseConfig(text, out data, out message);
        }

        /// <summary>
        /// Parses config file text as <typeparamref name="T"/> (via Newtonsoft, to support nullable
        /// types). Returns false, with <paramref name="message"/> describing why, if it's blank or fails
        /// to parse.
        /// </summary>
        protected bool TryParseConfig<T>(string text, out T data, out string message) where T : RLMGConfigData
        {
            message = null;

            string error;
            try
            {
                data = JsonConvert.DeserializeObject<T>(text);
                error = data == null ? "it's empty" : null;
            }
            catch (JsonException ex)
            {
                data = null;
                error = ex.Message;
            }

            if (error == null)
                return true;

            message = "Failed to parse config file at " + localContentPath + ": " + error;

            return false;
        }

        /// <summary>
        /// The config file is optional - if it doesn't exist, the components keep their Inspector
        /// settings and nothing is logged. A file that exists but fails to load is still logged as an error.
        /// </summary>
        protected override IEnumerator OnLocalFailure(string error, string url)
        {
            if (!File.Exists(localContentPath))
                yield break;

            yield return base.OnLocalFailure(error, url);
        }

        /// <summary>
        /// Writes the current settings of the components this configures out to the config file (see
        /// localContentPath) - the complement to loading it. Returns whether the file was written.
        /// </summary>
        public virtual bool SaveCurrentConfigToDisk()
        {
            return FileLoadingUtility.TryWriteJsonToDisk(GetCurrentConfigData(), localContentPath);
        }

        /// <summary>
        /// Config data reflecting the current settings of the components this configures. Override to
        /// return an <see cref="RLMGConfigData"/> subclass, populated via <see cref="PopulateConfigData"/>.
        /// </summary>
        protected virtual RLMGConfigData GetCurrentConfigData()
        {
            RLMGConfigData data = new RLMGConfigData();
            PopulateConfigData(data);
            return data;
        }

        /// <summary>
        /// Sets every <see cref="RLMGConfigData"/> field from the current settings of the components this
        /// configures. Values with no live source (e.g. no AppManager instance, or the attract path, which
        /// isn't stored after it's loaded) are taken from the last loaded <see cref="Data"/>, if any.
        /// </summary>
        protected void PopulateConfigData(RLMGConfigData data)
        {
            data.appManagerConfig = doConfigureAppManager && AppManager.HasInstance()
                ? AppManager.Instance.GetCurrentConfigurationData()
                : Data?.appManagerConfig;

            data.loggerConfig = RLMGLogger.HasInstance()
                ? RLMGLogger.Instance.GetCurrentConfigurationData()
                : Data?.loggerConfig;

            // todo - once attract / mediaplayers dependency is reversed, these below will be removed
            data.attractPath = Data?.attractPath;

            if (attractTimeouts != null && attractTimeouts.Length > 0 && attractTimeouts[0] != null)
                data.attractTimeoutDuration = attractTimeouts[0].TimeoutDuration;
            else if (Data != null)
                data.attractTimeoutDuration = Data.attractTimeoutDuration;
        }

        /// <summary>
        /// Applies loaded config data to the logger, attract and app manager components in RLMG Core.
        /// </summary>
        protected virtual void ApplyConfigData(RLMGConfigData data)
        {
            if (data == null)
                return;

            if (data.loggerConfig != null)
                RLMGLogger.Instance.Configure(data.loggerConfig);

            if (doConfigureAppManager &&
                AppManager.HasInstance() &&
                data.appManagerConfig != null)
            {
                AppManager.Instance.Configure(data.appManagerConfig);
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