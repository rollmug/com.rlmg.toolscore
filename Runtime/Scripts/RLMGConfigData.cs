namespace rlmg.Tools.Core
{
    using System;
    using UnityEngine;

    [Serializable]
    public class RLMGConfigData
    {
        public DisplayData[] displays;

        public string attractPath;

        /// <summary>
        /// In seconds, the duration of no user input after which
        /// the AttractTimeout component(s) will time out.
        /// </summary>
        public int attractTimeoutDuration = 60;

        // todo - scroll sensitivity?
        // todo - touch sensitivity?

        public string logLevel = "log";

        public string logLocation = "application";

        public string logFolderName = "Exhibit Logs";

        public string logFileName = "exhibit_log";

        public int logMaxDays = 30;

        public bool doLogFilePerSession = false;

        public bool doLogDebugLogs = false;

        public string debugLogLevel = "log";

        public bool doLogDebugStackTrace = false;

        public LogType LogLevel => GetLogType(logLevel);

        public LogDestinationPath LogLocation
        {
            get
            {
                switch (logLocation.ToLower())
                {
                    case "desktop":
                        return LogDestinationPath.Desktop;
                    case "application":
                        return LogDestinationPath.Application;
                    case "streamingassets":
                        return LogDestinationPath.StreamingAssets;
                }

                return LogDestinationPath.Application;
            }
        }

        public LogType DebugLogLevel => GetLogType(debugLogLevel);

        public LogType GetLogType(string logtype)
        {
            switch (logtype.ToLower())
            {
                case "error":
                case "fatal":
                    return LogType.Error;
                case "assert":
                    return LogType.Assert;
                case "warning":
                case "warn":
                    return LogType.Warning;
                case "log":
                case "debug":
                case "info":
                    return LogType.Log;
                case "exception":
                case "verbose":
                case "trace":
                case "all":
                    return LogType.Exception;
            }

            return LogType.Log;
        }
    }

}