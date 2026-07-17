namespace rlmg.Tools.Core
{
    using System;
    using UnityEngine;
    using UnityEngine.InputSystem;

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

    public class AppManager : MonoBehaviour
    {
        /// <summary>
        /// An empty list will do no override from what's set in the inspector.
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

        [SerializeField]
        protected int vSyncCount = 1;

        [SerializeField]
        protected int targetFrameRate = 60;

        protected virtual void Start()
        {
            RLMGLogger.Log("Application Awake - version #" + Application.version);

            Cursor.visible = Application.isEditor;

            SetupDisplays();
            SetupFrequency();
        }

        protected virtual void Update()
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
                Application.Quit();

            if (Keyboard.current.spaceKey.wasPressedThisFrame)
                Cursor.visible = !Cursor.visible;
        }

        protected virtual void OnApplicationQuit()
        {
            RLMGLogger.Log(
                string.Format(
                    "Application Quit at {0:MM / dd / yy H: mm:ss zzz}",
                    System.DateTime.Now
                    )
                );
        }

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
        }

        protected virtual void SetupDisplays()
        {
            if (displaysData.Length == 0)
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

            // Set up other _displaysData
            if (Display.displays.Length > 1 &&
                displaysData.Length > 1)
            {
                for (int i = 1; i < displaysData.Length; i++)
                {
                    DisplayData data = displaysData[i];

                    if (data == null)
                        continue;

                    data.dimensions.x = Math.Max(data.dimensions.x, 10);
                    data.dimensions.y = Math.Max(data.dimensions.y, 10);

                    Display.displays[i].Activate();
                    Display.displays[i].SetParams(
                        data.dimensions.x,
                        data.dimensions.y,
                        0,
                        0);

                    if (doDebugLogSetup)
                        RLMGLogger.Log(
                            string.Format(
                                "Activated display {0}.\nRendering Res: {1} x {2}\nSystem Res: {3} x {4}",
                                i + 1,
                                Display.displays[i].renderingWidth,
                                Display.displays[i].renderingHeight,
                                Display.displays[i].systemWidth,
                                Display.displays[i].systemHeight
                                ));                
                }
            }
        }

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

        protected void SetupFrequency()
        {
            QualitySettings.vSyncCount = vSyncCount;
            Application.targetFrameRate = targetFrameRate;
        }
    }
}