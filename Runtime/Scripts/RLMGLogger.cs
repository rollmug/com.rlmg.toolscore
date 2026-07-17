namespace rlmg.Tools.Core
{
    using System;
    using System.IO;
    using System.Text.RegularExpressions;
    using UnityEngine;

    // 
    // contributiongs - use test account in github?

    public enum LogDestinationPath
    {
        Desktop,
        Application,
        StreamingAssets
    }

    public enum LogDelimiter
    {
        Comma,
        Tab
    }

    [Obsolete]
    public enum MESSAGETYPE
    {
        ERROR,
        INFO
    }

    [Serializable]
    public class RLMGLoggerConfigurationData
    {
        /// <summary>
        /// The log level specified as a string (e.g. "error", "warning", "info")
        /// used when configuring which messages should be written to disk.
        /// </summary>
        [Tooltip("String name of the minimum log level to write to disk (e.g. 'error', 'warning', 'info').")]
        public string logLevel;

        /// <summary>
        /// Textual name of the destination location for log files. This can be
        /// mapped to a LogDestinationPath by the loader (e.g. "desktop").
        /// </summary>
        [Tooltip("Textual destination for log files (desktop, application, streamingassets).")]
        public string logLocation;

        /// <summary>
        /// Optional explicit UnityEngine.LogType used to control verbosity. When
        /// set this overrides the string-based logLevel.
        /// </summary>
        [Tooltip("Optional explicit Unity LogType to control which messages are written.")]
        public LogType? verbosity;

        /// <summary>
        /// Optional explicit destination enum that overrides the string-based
        /// logLocation value when present.
        /// </summary>
        [Tooltip("Optional explicit destination enum for log files.")]
        public LogDestinationPath? logDestinationPath;

        /// <summary>
        /// Folder name used when creating the log directory. If not provided a
        /// default folder name is used by the logger.
        /// </summary>
        [Tooltip("Folder name under the chosen destination where logs will be stored.")]
        public string logFolderName;

        /// <summary>
        /// Base file name (without extension) for the generated log files.
        /// </summary>
        [Tooltip("Base file name for log files (extension will be appended automatically).")]
        public string logFileName;

        /// <summary>
        /// Maximum number of days to keep log files before purging. 0 means
        /// never delete old log files.
        /// </summary>
        [Tooltip("Max days to retain log files before deletion. 0 = never delete.")]
        public int? maxDays;

        /// <summary>
        /// Whether the logger should create a new file for each application
        /// session instead of appending to an existing file.
        /// </summary>
        [Tooltip("Create a new log file per application session when true.")]
        public bool? doLogFilePerSession;

        /// <summary>
        /// Whether to capture messages coming from Debug.unityLogger and write
        /// them to disk as well as to the Unity console.
        /// </summary>
        [Tooltip("Capture and write to disk messages emitted through Debug.unityLogger.")]
        public bool? doLogDebugLogs;

        /// <summary>
        /// String-based verbosity used specifically for Debug.unityLogger
        /// messages. This will be mapped to a LogType when applied.
        /// </summary>
        [Tooltip("Minimum log level (string) for Debug.unityLogger messages to be captured.")]
        public string debugLogLevel;

        /// <summary>
        /// Optional explicit LogType to use for filtering Debug.unityLogger
        /// messages (overrides debugLogLevel string when present).
        /// </summary>
        [Tooltip("Optional explicit Unity LogType to filter Debug.unityLogger messages.")]
        public LogType? debugLogVerbosity;

        /// <summary>
        /// When true, include stack traces when writing Debug.unityLogger
        /// messages to disk.
        /// </summary>
        [Tooltip("Include stack traces when persisting Debug.unityLogger messages.")]
        public bool? doLogDebugStackTrace;

        /// <summary>
        /// Computed property that converts the string-based logLevel into the
        /// corresponding UnityEngine.LogType. Returns null if not parseable.
        /// </summary>
        public LogType? LogLevel => GetLogType(logLevel);

        /// <summary>
        /// Computed property that converts the string-based logLocation into the
        /// corresponding LogDestinationPath enum. Returns null if not parseable.
        /// </summary>
        public LogDestinationPath? LogLocation => GetLogDestinationPath(logLocation);

        /// <summary>
        /// Computed property that converts the string-based debugLogLevel into
        /// the corresponding UnityEngine.LogType. Returns null if not parseable.
        /// </summary>
        public LogType? DebugLogLevel => GetLogType(debugLogLevel);

        public LogType? GetLogType(string logtype)
        {
            if (logtype == null)
                return null;

            logtype = logtype.ToLowerInvariant();
            logtype = Regex.Replace(logtype, @"\s+", string.Empty);

            switch (logtype)
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

            return null;
        }

        public LogDestinationPath? GetLogDestinationPath(string locationName)
        {
            if (locationName == null)
                return null;

            locationName = locationName.ToLowerInvariant();
            locationName = Regex.Replace(locationName, @"\s+", string.Empty);

            switch (locationName)
            {
                case "desktop":
                    return LogDestinationPath.Desktop;
                case "application":
                    return LogDestinationPath.Application;
                case "streamingassets":
                    return LogDestinationPath.StreamingAssets;
            }

            return null;
        }
    }

    /// <summary>
    /// The popular RLMGLogger singleton, with a few key features:
    /// 1. Logging to disk, as expected;
    /// 2. Static Log methods that will instantiate a Do Not Destroy on Load logger if one doesn't already exist;
    /// 3. Optional configuration in Unity Inspector; and
    /// 4. Optional listening to Debug.unityLogger, for writing to disk third-party log statements.
    /// </summary>
    public class RLMGLogger : SingletonDoNotDestroy<RLMGLogger>
    {
        [Header("Output")]
        [SerializeField]
        protected LogDestinationPath destPath = LogDestinationPath.Application;

        [SerializeField]
        protected string logFolderName = "Exhibit Logs";

        [SerializeField]
        protected string logFileName = "exhibit_log";
        //todo get exhibit name from like Unity project meta data... wherever the executable file name is defined

        [SerializeField]
        protected LogDelimiter logDelimiter = LogDelimiter.Comma;

        [SerializeField]
        protected string headerLine = "Timestamp, Type, Message, Stack Trace";

        [SerializeField]
        protected string timestampFormat = "yyyy/MM/dd HH:mm:ss";

        /// <summary>
        /// What level of messages should be logged to disk?
        /// Error = fewest messages.
        /// Does not filter messages that are passed to Debug.unityLogger;
        /// this can be separately configured through the native Debug class.
        /// </summary>
        [SerializeField]
        protected LogType verbosity = LogType.Log;

        /// <summary>
        /// Do write to disk when run in Unity Editor?
        /// </summary>
        [SerializeField]
        protected bool doLogToDiskInEditor = false;

        [Header("File Management")]
        /// <summary>
        /// Max days file will be saved before purging. 0 = never delete
        /// </summary>
        [SerializeField]
        [Tooltip("Max days file will be saved before purging. 0 = never delete")]
        protected int maxDays = 30;

        /// <summary>
        /// Do create a new log file per app session?
        /// </summary>
        [SerializeField]
        protected bool doLogFilePerSession = false;

        [Header("Debug.unityLogger Listening")]
        /// <summary>
        /// Do listen for Debug.unityLogger messages and log them to disk?
        /// </summary>
        [SerializeField]
        protected bool doLogDebugLogs = false;

        /// <summary>
        /// What level of Debug.unityLogger messages should be logged to disk?
        /// Error = fewest messages.
        /// </summary>
        [SerializeField]
        protected LogType debugLogVerbosity = LogType.Log;

        /// <summary>
        /// Include stack trace when writing Debug.unityLogger messages to disk?
        /// </summary>
        [SerializeField]
        protected bool doLogDebugStackTrace = false;

        protected bool doSkipLoggingDebugLogs = false;

        protected string logFolderPath;

        protected string logFilePath;

        protected string delimiter = ",";

        protected string extension = ".csv";

        protected virtual void Awake()
        {
            Setup();
        }

        protected virtual void OnEnable()
        {
            Application.logMessageReceived += HandleDebugLog;
        }

        protected virtual void OnDisable()
        {
            Application.logMessageReceived -= HandleDebugLog;
        }

        public virtual void Configure(RLMGLoggerConfigurationData data)
        {
            if (data == null)
                return;

            if (data.verbosity != null)
                verbosity = (LogType)data.verbosity;

            if (data.logLevel != null)
                verbosity = (LogType)data.LogLevel;

            if (data.logDestinationPath != null)
                destPath = (LogDestinationPath)data.logDestinationPath;

            if (data.logLocation != null)
                destPath = (LogDestinationPath)data.LogLocation;

            if (data.logFolderName != null)
                logFolderName = data.logFolderName;

            if (data.logFileName != null)
                logFileName = data.logFileName;

            if (data.maxDays != null)
                maxDays = (int)data.maxDays;

            if (data.doLogFilePerSession != null)
                doLogFilePerSession = (bool)data.doLogFilePerSession;

            if (data.doLogDebugLogs != null)
                doLogDebugLogs = (bool)data.doLogDebugLogs;

            if (data.debugLogVerbosity != null)
                debugLogVerbosity = (LogType)data.debugLogVerbosity;

            if (data.debugLogLevel != null)
                debugLogVerbosity = (LogType)data.DebugLogLevel;

            if (data.doLogDebugStackTrace != null)
                doLogDebugStackTrace = (bool)data.doLogDebugStackTrace;
        }

        protected virtual void Setup()
        {
            if (Application.isEditor && !doLogToDiskInEditor)
                return;

            DateTime Now = DateTime.Now;

            // -------- setup delimiter --------
            delimiter = logDelimiter == LogDelimiter.Comma ? "," : "\t";

            // -------- create log folder if necessary --------
            SetLogPath();

            if (!Directory.Exists(logFolderPath))
            {
                Directory.CreateDirectory(logFolderPath);
            }

            // -------- clear old files if limit is above 0 --------
            if (maxDays > 0)
            {
                // check if need to delete old log files
                string[] filePaths = Directory.GetFiles(logFolderPath);
                foreach (string file in filePaths)
                {
                    DateTime fileDate = File.GetCreationTime(file);
                    int days = Now.Subtract(fileDate).Days;
                    // get rid if older than a month (30 days)
                    if (days > maxDays)
                    {
                        File.Delete(file);
                    }
                }
            }

            // -------- setup filepath --------
            logFileName = logFileName + "_" + Now.Year + "-" + Now.Month + "-" + Now.Day;

            if (doLogFilePerSession)
            {
                int sessionIndex = 1;

                // find index of latest session and add to file path name
                string[] filePaths = Directory.GetFiles(logFolderPath);
                foreach (string file in filePaths)
                {
                    if (file.Contains(logFileName))
                    {
                        // increment index
                        sessionIndex++;
                    }
                }

                logFileName += "-" + sessionIndex.ToString("D4");
            }

            extension = logDelimiter == LogDelimiter.Comma ? ".csv" : ".tsv";
            logFileName += extension;

            logFilePath = Path.Combine(logFolderPath, logFileName);

            if (!File.Exists(logFilePath))
            {
                // create if a new log file
                string header = headerLine + Environment.NewLine;
                File.WriteAllText(logFilePath, header);
            }
        }

        protected virtual void SetLogPath()
        {
            string path = "";
            if (destPath == LogDestinationPath.Desktop)
            {
                path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            }
            else if (destPath == LogDestinationPath.Application)
            {
                path = Application.dataPath;
            }
            else if (destPath == LogDestinationPath.StreamingAssets)
            {
                path = Application.streamingAssetsPath;
            }

            if (string.IsNullOrEmpty(logFolderName))
                logFolderName = "Exhibit Logs";

            logFolderPath = Path.Combine(path, logFolderName);
        }

        /// <summary>
        /// Subscription to Debug.unityLogger messages
        /// </summary>
        /// <param name="message"></param>
        /// <param name="stackTrace"></param>
        /// <param name="type"></param>
        protected virtual void HandleDebugLog(string message, string stackTrace, LogType type)
        {
            if (!doLogDebugLogs)
                return;

            // check if are avoiding logging
            // because the Debug.LogMessage originated from this instance
            if (doSkipLoggingDebugLogs)
                return;

            if ((int)type > (int)debugLogVerbosity)
                return;

            doSkipLoggingDebugLogs = true;

            if (doLogDebugStackTrace)
                WriteLine(type, message, stackTrace);
            else
                WriteLine(type, message);

            doSkipLoggingDebugLogs = false;
        }

        protected virtual void WriteLine(
            LogType type,
            string message,
            string stackTrace = null)
        {
            if (Application.isEditor && !doLogToDiskInEditor)
                return;

            string line = string.Join(
                delimiter,
                DateTime.Now.ToString(timestampFormat),
                type.ToString(),
                message,
                stackTrace);

            string output = line + Environment.NewLine;

            File.AppendAllText(logFilePath, output);
        }

        /// <summary>
        /// Utility method for logging
        /// </summary>
        /// <param name="type"></param>
        /// <param name="message"></param>
        /// <param name="context"></param>
        public static void LogMessage(
            LogType type,
            object message,
            UnityEngine.Object context = null)
        {
            if (Instance == null)
                return;

            string logString = string.Format("{0}:{1}",
                context,
                message);

            Instance.doSkipLoggingDebugLogs = true;

            Instance.WriteLine(type, logString);
            Debug.unityLogger.Log(type, message, context);

            Instance.doSkipLoggingDebugLogs = false;
        }

        /// <summary>
        /// For use like Debug.Log.
        /// Will instantiate a RLMGLogger GameObject if none exists already.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="context"></param>
        public static void Log(
            object message,
            UnityEngine.Object context = null)
        {
            LogMessage(LogType.Log, message, context);
        }

        /// <summary>
        /// For use like Debug.LogWarning.
        /// Will instantiate a RLMGLogger GameObject if none exists already.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="context"></param>
        public static void LogWarning(
            object message,
            UnityEngine.Object context = null)
        {
            LogMessage(LogType.Warning, message, context);
        }

        /// <summary>
        /// For use like Debug.LogError.
        /// Will instantiate a RLMGLogger GameObject if none exists already.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="context"></param>
        public static void LogError(
            object message,
            UnityEngine.Object context = null)
        {
            LogMessage(LogType.Error, message, context);
        }


        [Obsolete]
        public virtual void Log(
            string message,
            MESSAGETYPE type = MESSAGETYPE.INFO
            )
        {
            switch (type)
            {
                case MESSAGETYPE.INFO:
                    LogMessage(LogType.Log, message);
                    return;
                case MESSAGETYPE.ERROR:
                    LogMessage(LogType.Error, message);
                    return;
            }
        }


    }

}