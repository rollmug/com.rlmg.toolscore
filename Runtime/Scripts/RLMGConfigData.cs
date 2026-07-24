namespace rlmg.Tools.Core
{
    using System;
    using UnityEngine;

    /// <summary>
    /// Configuration data object for RLMGConfigLoader
    /// </summary>
    [Serializable]
    public class RLMGConfigData
    {

        /// <summary>
        /// Configuration data for the AppManager component. When present this
        /// data will be used to override values set in the AppManager inspector.
        /// </summary>
        [Tooltip("Configuration overrides for the AppManager (display, vsync, frame rate).")]
        public AppManagerConfigurationData appManagerConfig;

        /// <summary>
        /// Optional path (relative or absolute) to the attract content (e.g. video)
        /// that should be used when the app is idle.
        /// </summary>
        [Tooltip("Path to attract/idle content to play when the app is inactive.")]
        public string attractPath;

        /// <summary>
        /// In seconds, the duration of no user input after which
        /// the AttractTimeout component(s) will time out and the attract content
        /// (if configured) will be shown.
        /// </summary>
        [Tooltip("Seconds of inactivity before attract mode activates.")]
        public int attractTimeoutDuration = 60;

        // todo - scroll sensitivity?
        // todo - touch sensitivity?

        /// <summary>
        /// Configuration settings for the RLMGLogger. When present these values
        /// will be applied to the logger at runtime.
        /// </summary>
        [Tooltip("Logger configuration used to initialize the RLMGLogger at startup.")]
        public RLMGLoggerConfigurationData loggerConfig;

        
    }

}