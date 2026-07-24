namespace rlmg.Tools.Core
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using UnityEngine;

    /// <summary>
    /// Where the log files' main log folder will be located
    /// </summary>
    public enum LogDestinationPath
    {
        Desktop,
        Application,
        StreamingAssets
    }

    /// <summary>
    /// How values should be separated in log files; determines file type.
    /// </summary>
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

    /// <summary>
    /// Configuration data object for RLMGLogger
    /// </summary>
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
        /// Optional explicit <a href="https://docs.unity3d.com/6000.3/Documentation/ScriptReference/LogType.html">UnityEngine.LogType</a> used to control verbosity.
        /// When set this overrides the string-based logLevel.
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
        /// messages. This will be mapped to a <a href="https://docs.unity3d.com/6000.3/Documentation/ScriptReference/LogType.html">UnityEngine.LogType</a> when applied.
        /// </summary>
        [Tooltip("Minimum log level (string) for Debug.unityLogger messages to be captured.")]
        public string debugLogLevel;

        /// <summary>
        /// Optional explicit <a href="https://docs.unity3d.com/6000.3/Documentation/ScriptReference/LogType.html">UnityEngine.LogType</a> to use for filtering Debug.unityLogger
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
        /// corresponding <a href="https://docs.unity3d.com/6000.3/Documentation/ScriptReference/LogType.html">UnityEngine.LogType</a>. Returns null if not parseable.
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

        /// <summary>
        /// Get the native <a href="https://docs.unity3d.com/6000.3/Documentation/ScriptReference/LogType.html">UnityEngine.LogType</a> enum corresponding to the input string (e.g. 'fatal', 'warn', 'verbose').
        /// Employs a custom mapping of string to <a href="https://docs.unity3d.com/6000.3/Documentation/ScriptReference/LogType.html">UnityEngine.LogType</a>.
        /// </summary>
        /// <param name="logtype">representation of log level / verbosity</param>
        /// <returns>Nullable native <a href="https://docs.unity3d.com/6000.3/Documentation/ScriptReference/LogType.html">UnityEngine.LogType</a> enum</returns>
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

        /// <summary>
        /// Get the LogDestinationPath enum corresponding to the input string (e.g. 'desktop', 'Streaming Assets')
        /// </summary>
        /// <param name="locationName">representation of log location / folder name</param>
        /// <returns>Nullable LogDestinationPath enum</returns>
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
    /// The popular RLMGLogger singleton.
    /// Includes the following features:
    /// 1. Logging to disk, as expected;
    /// 2. Static Log methods that will instantiate a Do Not Destroy on Load logger if one doesn't already exist;
    /// 3. Optional configuration in Unity Inspector; and
    /// 4. Optional listening to Debug.unityLogger, for writing to disk third-party log statements.
    /// </summary>
    public class RLMGLogger : SingletonDoNotDestroy<RLMGLogger>
    {
        /// <summary>
        /// What main folder should the logs be written to? (e.g. Desktop, Application root, Streaming Assets)
        /// </summary>
        [Header("Output")]
        [SerializeField]
        protected LogDestinationPath destPath = LogDestinationPath.Application;

        /// <summary>
        /// What subfolder should the logs be written to?
        /// Creates folder if it does not already exist, at destPath (e.g. "Streaming Assets/Exhibit Logs/")
        /// </summary>
        [SerializeField]
        protected string logFolderName = "Exhibit Logs";

        /// <summary>
        /// What base file name should the log files use?
        /// Will have a timestamp appended when written.
        /// </summary>
        [SerializeField]
        protected string logFileName = "exhibit_log";
        //todo get exhibit name from like Unity project meta data... wherever the executable file name is defined

        /// <summary>
        /// What delimiter should the log files use? (i.e. Comma or Tab)
        /// Log files will be saved with the corresponding extension (e.g. "csv" for Comma)
        /// </summary>
        [SerializeField]
        protected LogDelimiter logDelimiter = LogDelimiter.Comma;

        /// <summary>
        /// Contents of the header row.
        /// Comma separated. Will have whitespace trimmed.
        /// </summary>
        [SerializeField]
        protected string headerLine = "Timestamp, Type, Message, Stack Trace";

        /// <summary>
        /// System.Datetime recognized timestamp format string
        /// </summary>
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
        /// Native Debug <a href="https://docs.unity3d.com/6000.3/Documentation/ScriptReference/LogType.html">LogType</a> enum.
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

        /// <summary>
        /// Managed by this class for not logging messages twice when logging things directly AND logging native Debug logging
        /// </summary>
        protected bool doSkipLoggingDebugLogs = false;

        /// <summary>
        /// Managed by this class for building log folder path once.
        /// </summary>
        protected string logFolderPath;

        /// <summary>
        /// Managed by this class for building log file path once.
        /// </summary>
        protected string logFilePath;

        /// <summary>
        /// Managed by this class for choosing log file delimiter.
        /// </summary>
        protected string delimiter = ",";

        /// <summary>
        /// Managed by this class for choosing log file extension once.
        /// </summary>
        protected string extension = ".csv";

        /// <summary>
        /// Invokes Setup
        /// </summary>
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

        /// <summary>
        /// Apply configuration settings to this Instance.
        /// </summary>
        /// <param name="data"></param>
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

        /// <summary>
        /// Set up fields managed by this class, and create files and folders if they do not yet exist.
        /// </summary>
        protected virtual void Setup()
        {
            if (Application.isEditor && !doLogToDiskInEditor)
                return;

            DateTime Now = DateTime.Now;

            // -------- setup delimiter --------
            delimiter = logDelimiter == LogDelimiter.Comma ? "," : "\t";

            // -------- create log folder if necessary --------
            SetLogFolderPath();

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
                IEnumerable<string> headerContents = headerLine.Split(',')
                                      .Select(x => x.Trim())
                                      .Where(x => !string.IsNullOrEmpty(x));

                string header = logDelimiter == LogDelimiter.Comma ?
                    string.Join(",", headerContents) :
                    string.Join("\t", headerContents);

                // create if a new log file
                header += Environment.NewLine;
                File.WriteAllText(logFilePath, header);
            }
        }

        /// <summary>
        /// Sets logFolderPath field
        /// </summary>
        protected virtual void SetLogFolderPath()
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

        /// <summary>
        /// Write a log line to the log file.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="message"></param>
        /// <param name="stackTrace"></param>
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

        /// <summary>
        /// Log a message.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="type"></param>
        [Obsolete("See static methods Log, LogWarning, and LogError")]
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