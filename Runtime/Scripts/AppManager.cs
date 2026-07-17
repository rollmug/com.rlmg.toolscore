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

        public bool doFullScreen = true;
    }

    public class AppManager : MonoBehaviour
    {
        // todo - target frame rate from inspector

        [SerializeField]
        [Tooltip("The first item in the displaysData list will be used as the primary screen's resolution.")]
        private bool doSetResolution = false;

        /// <summary>
        /// An empty list will do no override from what's set in the inspector.
        /// </summary>
        [Tooltip("An empty list will do no override from what's set in the inspector.")]
        [SerializeField]
        protected DisplayData[] displaysData;

        [SerializeField]
        protected bool doDebugLog = false;

        protected virtual void Start()
        {
            RLMGLogger.Log("Application Awake - version #" + Application.version);

            Cursor.visible = Application.isEditor;

            Application.targetFrameRate = 60;

            SetupDisplays();
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

        public virtual void Configure(
            bool doResetResolution,
            DisplayData[] displays)
        {
            doSetResolution = doResetResolution;
            displaysData = displays;
        }

        protected virtual void SetupDisplays()
        {
            if (!doSetResolution)
                return;

            if (displaysData.Length == 0)
                return;

            if (doDebugLog)
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

            // Set up other displays
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

                    if (doDebugLog)
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
    }
}