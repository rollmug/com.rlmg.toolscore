namespace rlmg.Tools.Core
{
    using System;
    using UnityEngine;
    using UnityEngine.InputSystem;

    /// <summary>
    /// Configuration data object for each display the software sets up.
    /// </summary>
    [Serializable]
    public class DisplayData
    {
        /// <summary>
        /// Width by height
        /// </summary>
        [Tooltip("Width by height")]
        public Vector2Int dimensions = new Vector2Int(1920, 1080);

        /// <summary>
        /// Whether this display should be presented in fullscreen mode when
        /// activated. When false the display will run in windowed mode.
        /// </summary>
        [Tooltip("Run this display in fullscreen when activated.")]
        public bool doFullScreen = true;
    }

    /// <summary>
    /// Configuration data object for AppManager
    /// </summary>
    [Serializable]
    public class AppManagerConfigurationData
    {
        /// <summary>
        /// Optional array of display configurations. When present these values
        /// are used to configure each connected display and may override the
        /// values set in the AppManager inspector.
        /// </summary>
        [Tooltip("Optional display configuration list. First entry is used for primary display.")]
        public DisplayData[] displaysData;

        /// <summary>
        /// Whether AppManager should emit debug log lines describing the input
        /// and display setup process. When null no override is applied.
        /// </summary>
        [Tooltip("If true, log display and input setup details during startup.")]
        public bool? doDebugLogSetup;

        /// <summary>
        /// Optional vSync count to apply (QualitySettings.vSyncCount). When null
        /// the existing inspector value is left unchanged.
        /// </summary>
        [Tooltip("Optional vSync count override. Leave null to keep inspector value.")]
        public int? vSyncCount;

        /// <summary>
        /// Optional target frame rate to set for the application. When null the
        /// inspector value or default remains in effect.
        /// </summary>
        [Tooltip("Optional target frame rate override. Leave null to keep inspector value.")]
        public int? targetFrameRate;
    }

    /// <summary>
    /// Setup and manager class
    /// Includes the following features:
    /// 1. Logs the app's start and exit,
    /// 2. Monitors key commands to exit the app and show the cursor, and
    /// 3. Sets up input and output features of the Unity game engine.
    /// </summary>
    public class AppManager : SingletonDoNotDestroy<AppManager>
    {
        /// <summary>
        /// An empty list will not override from what's set in the inspector.
        /// The first item in the displaysData list will be used as the primary screen's resolution.
        /// </summary>
        [Tooltip("An empty list will not override what's set in the Editor. The first item in the displaysData list will be used as the primary screen's resolution.")]
        [SerializeField]
        protected DisplayData[] displaysData;

        /// <summary>
        /// Whether to log input and output setup. Does not affect app start and exit logging.
        /// </summary>
        [Tooltip("Whether to log input and output setup. Does not affect app start and exit logging.")]
        [SerializeField]
        protected bool doDebugLogSetup = false;

        /// <summary>
        /// Overrides the Unity player's default setting of 1.
        /// </summary>
        [SerializeField]
        protected int vSyncCount = 1;

        /// <summary>
        /// Overrides the Unity player's default setting. Note that this is ignored when vSyncCount > 0.
        /// </summary>
        [SerializeField]
        protected int targetFrameRate = 60;

        /// <summary>
        /// If true, display and frequency setup is deferred from Start until Configure is called.
        /// </summary>
        [Tooltip("If true, display and frequency setup is deferred from Start until Configure is called.")]
        [SerializeField]
        protected bool doWaitForConfigToInit = false;

        /// <summary>
        /// Managed by this class so Initialize only runs once per instance; Configure calls made
        /// afterward re-apply setup instead.
        /// </summary>
        private bool isInitialized = false;

        /// <summary>
        /// Whether Start should invoke Initialize. When waiting for config, initialization is deferred
        /// to <see cref="Configure"/>.
        /// </summary>
        protected virtual bool DoInitializeOnStart => !doWaitForConfigToInit;

        /// <summary>
        /// Logs app start and invokes Initialize, unless DoInitializeOnStart is false
        /// </summary>
        protected virtual void Start()
        {
            RLMGLogger.Log("Application Awake - version #" + Application.version);

            Cursor.visible = Application.isEditor;

            if (DoInitializeOnStart)
                Initialize();
        }

        /// <summary>
        /// Invokes Setup methods once; subsequent calls are no-ops.
        /// </summary>
        protected void Initialize()
        {
            if (isInitialized)
                return;

            isInitialized = true;

            SetupDisplays();
            SetupFrequency();
        }

        /// <summary>
        /// Monitors for key input
        /// </summary>
        protected virtual void Update()
        {
            if (Keyboard.current == null)
                return;

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
                Application.Quit();

            if (Keyboard.current.spaceKey.wasPressedThisFrame)
                Cursor.visible = !Cursor.visible;
        }

        /// <summary>
        /// Logs app exit
        /// </summary>
        protected override void OnApplicationQuit()
        {
            base.OnApplicationQuit();

            RLMGLogger.Log(
                string.Format(
                    "Application Quit at {0:MM / dd / yy H: mm:ss zzz}",
                    System.DateTime.Now
                    )
                );
        }

        /// <summary>
        /// Apply configuration settings. Initializes if waiting for config to do so; if already
        /// initialized, re-applies display and frequency setup so the new settings take effect.
        /// </summary>
        /// <param name="data"></param>
        public virtual void Configure(AppManagerConfigurationData data)
        {
            if (data == null)
                return;

            if (data.displaysData != null)
                displaysData = data.displaysData;

            if (data.doDebugLogSetup != null)
                doDebugLogSetup = (bool)data.doDebugLogSetup;

            if (data.vSyncCount != null)
                vSyncCount = (int)data.vSyncCount;

            if (data.targetFrameRate != null)
                targetFrameRate = (int)data.targetFrameRate;

            if (isInitialized)
            {
                SetupDisplays();
                SetupFrequency();
            }
            else if (doWaitForConfigToInit)
            {
                Initialize();
            }
        }

        /// <summary>
        /// The complement to <see cref="Configure"/>: configuration data holding this instance's current settings.
        /// </summary>
        public virtual AppManagerConfigurationData GetCurrentConfigurationData()
        {
            return new AppManagerConfigurationData
            {
                displaysData = displaysData,
                doDebugLogSetup = doDebugLogSetup,
                vSyncCount = vSyncCount,
                targetFrameRate = targetFrameRate,
            };
        }

        /// <summary>
        /// Activates the display at displayIndex (0-based, as in Display.displays), applying its
        /// displaysData entry's dimensions if one exists; otherwise the display keeps its native
        /// resolution. Logs a warning and does nothing if no such display is connected.
        /// </summary>
        /// <param name="displayIndex"></param>
        public virtual void ActivateDisplay(int displayIndex)
        {
            if (displayIndex < 0 ||
                displayIndex >= Display.displays.Length)
            {
                if (Application.isEditor)
                {
                    if (doDebugLogSetup)
                        RLMGLogger.Log(
                            string.Format(
                                "Skipping activation of display {0} in the Editor; use a Game view targeting it instead.",
                                displayIndex + 1
                                ));
                    return;
                }

                RLMGLogger.LogWarning(
                    string.Format(
                        "Can't activate display {0}: only {1} display(s) connected.",
                        displayIndex + 1,
                        Display.displays.Length
                        ));
                return;
            }

            // Display.displays[0] is always ON so no need to Activate
            if (displayIndex == 0)
                return;

            Display display = Display.displays[displayIndex];

            if (!display.active)
                display.Activate();

            DisplayData data = displaysData != null && displayIndex < displaysData.Length ?
                displaysData[displayIndex] :
                null;

            if (data != null)
            {
                data.dimensions.x = Math.Max(data.dimensions.x, 10);
                data.dimensions.y = Math.Max(data.dimensions.y, 10);

                display.SetParams(
                    data.dimensions.x,
                    data.dimensions.y,
                    0,
                    0);
            }

            if (doDebugLogSetup)
                RLMGLogger.Log(
                    string.Format(
                        "Activated display {0}.\nRendering Res: {1} x {2}\nSystem Res: {3} x {4}",
                        displayIndex + 1,
                        display.renderingWidth,
                        display.renderingHeight,
                        display.systemWidth,
                        display.systemHeight
                        ));
        }

        /// <summary>
        /// Setup displays used by the app, as set in the displaysData field
        /// </summary>
        protected virtual void SetupDisplays()
        {
            if (displaysData == null ||
                displaysData.Length == 0)
                return;

            if (doDebugLogSetup)
                LogAvailableDisplayData();

            DisplayData first = displaysData[0];

            if (first == null)
                return;

            first.dimensions.x = Math.Max(first.dimensions.x, 10);
            first.dimensions.y = Math.Max(first.dimensions.y, 10);

            Screen.SetResolution(
                first.dimensions.x,
                first.dimensions.y,
                first.doFullScreen
                );

            // Display.displaysData[0] is always ON so no need to Activate

            // Set up other displaysData, for displays that are connected
            int setupCount = Math.Min(displaysData.Length, Display.displays.Length);

            for (int i = 1; i < setupCount; i++)
            {
                if (displaysData[i] == null)
                    continue;

                ActivateDisplay(i);
            }
        }

        /// <summary>
        /// Helper method for optionally logging displays setup
        /// </summary>
        protected virtual void LogAvailableDisplayData()
        {
            string primaryDisplayDebugInfo = "Primary Display    App (screen dims): " + Screen.width + " x " + Screen.height + " (" + ((float)Screen.width / (float)Screen.height) + ")";
            primaryDisplayDebugInfo += "   \"Current Resolution\": " + Screen.currentResolution.width + " x " + Screen.currentResolution.height;
            primaryDisplayDebugInfo += "   Rendering Res: " + Display.main.renderingWidth + " x " + Display.main.renderingHeight + "   System Res: " + Display.main.systemWidth + " x " + Display.main.systemHeight;

            Resolution[] supportedResolutions = Screen.resolutions;

            if (supportedResolutions != null && supportedResolutions.Length > 0)
            {
                primaryDisplayDebugInfo += "\n\nSupported Resolutions:";

                foreach (var res in supportedResolutions)
                {
                    primaryDisplayDebugInfo += "\n" + res.width + "x" + res.height;
                }
            }

            RLMGLogger.Log(primaryDisplayDebugInfo);
        }

        /// <summary>
        /// Set up frame rate-related settings
        /// </summary>
        protected void SetupFrequency()
        {
            QualitySettings.vSyncCount = vSyncCount;
            Application.targetFrameRate = targetFrameRate;
        }
    }
}