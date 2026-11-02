namespace rlmg.Tools.Core.Tests
{
    using System;
    using System.Collections;
    using System.IO;
    using System.Reflection;
    using System.Text.RegularExpressions;

    using Newtonsoft.Json.Linq;

    using NUnit.Framework;

    using rlmg.Tools.ContentLoading;
    using rlmg.Tools.ContentLoading.Tests;

    using UnityEngine;
    using UnityEngine.TestTools;

    using Object = UnityEngine.Object;

    /// <summary>
    /// Covers <see cref="RLMGConfigLoader"/>'s optional config file in config/: (a) loading and applying
    /// it, (c) silently continuing when there's none, (d) reporting a malformed one without stalling
    /// loading, and (e) saving the current settings back out - plus the AppManager/RLMGLogger
    /// GetCurrentConfigurationData complements that saving relies on. (b) doesn't apply here:
    /// RLMGConfigLoader has no fallback text asset (NCGMConfigLoader's is covered in the shell's tests).
    /// </summary>
    public class RLMGConfigLoaderTests
    {
        private const string k_ConfigFileName = "config.json";
        private const int k_InspectorTimeoutDuration = 60;

        private LoadingTestRig rig;
        private AttractTimeout attractTimeout;

        [SetUp]
        public void SetUp()
        {
            rig = new LoadingTestRig();
            attractTimeout = rig.Create<AttractTimeout>(timeout => timeout.TimeoutDuration = k_InspectorTimeoutDuration);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            rig.Dispose();
            yield return null;
        }

        private TestableRLMGConfigLoader CreateLoader()
        {
            return rig.Create<TestableRLMGConfigLoader>(loader => loader.Setup(rig.ContentDirectory, k_ConfigFileName, new[] { attractTimeout }));
        }

        private static IEnumerator LoadAndWait(RLMGConfigLoader loader)
        {
            bool finished = false;
            loader.AllLoadingFinished.AddListener(() => finished = true);

            loader.LoadContent();

            yield return LoadingTestRig.WaitUntil(() => finished, "AllLoadingFinished");
        }

        [Test]
        public void ConfigFilePath_IsInConfigSubdirectory_UnlessBlank()
        {
            TestableRLMGConfigLoader loader = CreateLoader();

            Assert.AreEqual(rig.PathInContent("config", k_ConfigFileName), loader.ConfigFilePath);

            loader.SetConfigSubdirectory("");
            Assert.AreEqual(rig.PathInContent(k_ConfigFileName), loader.ConfigFilePath);
        }

        // (a)
        [UnityTest]
        public IEnumerator Load_ConfigOnDisk_IsApplied()
        {
            TestableRLMGConfigLoader loader = CreateLoader();
            rig.WriteFile(loader.ConfigFilePath, "{\"attractTimeoutDuration\": 42}");

            yield return LoadAndWait(loader);

            Assert.IsNotNull(loader.Data);
            Assert.AreEqual(42, loader.Data.attractTimeoutDuration);
            Assert.AreEqual(42, attractTimeout.TimeoutDuration);
            Assert.AreEqual(LoadStatus.Succeeded, loader.CurrentStatus);
            rig.Logs.AssertNoWarningsOrErrors();
        }

        // (c)
        [UnityTest]
        public IEnumerator Load_NoConfig_ContinuesSilently()
        {
            TestableRLMGConfigLoader loader = CreateLoader();

            yield return LoadAndWait(loader);

            Assert.IsNull(loader.Data);
            Assert.AreEqual(k_InspectorTimeoutDuration, attractTimeout.TimeoutDuration);
            Assert.IsFalse(File.Exists(loader.ConfigFilePath));
            rig.Logs.AssertNoWarningsOrErrors();
        }

        /// <summary>A malformed config and a blank one.</summary>
        private static readonly string[] k_InvalidConfigs = { "{ \"attractTimeoutDuration\": ", "  \n" };

        // (d)
        [UnityTest]
        public IEnumerator Load_InvalidConfig_LogsErrorAndReportsFailure([ValueSource(nameof(k_InvalidConfigs))] string invalidConfig)
        {
            TestableRLMGConfigLoader loader = CreateLoader();
            rig.WriteFile(loader.ConfigFilePath, invalidConfig);
            bool loadFailed = false;
            bool loadSucceeded = false;
            loader.AnyLoadFailed.AddListener(_ => loadFailed = true);
            loader.AnyLoadSucceeded.AddListener(_ => loadSucceeded = true);

            LogAssert.Expect(LogType.Error, new Regex("Failed to parse config file"));

            yield return LoadAndWait(loader);

            Assert.IsNull(loader.Data);
            Assert.AreEqual(LoadStatus.Failed, loader.CurrentStatus);
            Assert.IsTrue(loadFailed);
            Assert.IsFalse(loadSucceeded);
            Assert.AreEqual(k_InspectorTimeoutDuration, attractTimeout.TimeoutDuration);
            Assert.AreEqual(invalidConfig, File.ReadAllText(loader.ConfigFilePath), "The file should be left as-is");
        }

        // (e)
        [UnityTest]
        public IEnumerator SaveCurrentConfigToDisk_WritesCurrentSettings_AndLoadsBackTheSame()
        {
            TestableRLMGConfigLoader loader = CreateLoader();
            attractTimeout.TimeoutDuration = 17;

            Assert.IsTrue(loader.SaveCurrentConfigToDisk());

            JObject saved = JObject.Parse(File.ReadAllText(loader.ConfigFilePath));

            Assert.AreEqual(17, (int)saved["attractTimeoutDuration"]);
            Assert.AreEqual(JTokenType.Null, saved["appManagerConfig"].Type, "doConfigureAppManager is off and nothing was loaded");
            Assert.IsTrue(saved.ContainsKey("attractPath"));

            JToken expectedLoggerConfig = RLMGLogger.HasInstance()
                ? JObject.FromObject(RLMGLogger.Instance.GetCurrentConfigurationData())
                : JValue.CreateNull();
            Assert.IsTrue(JToken.DeepEquals(expectedLoggerConfig, saved["loggerConfig"]), "loggerConfig should reflect the live logger, if any:\n" + saved["loggerConfig"]);

            // Loading it back applies the saved value
            attractTimeout.TimeoutDuration = k_InspectorTimeoutDuration;
            TestableRLMGConfigLoader reloaded = CreateLoader();

            yield return LoadAndWait(reloaded);

            Assert.AreEqual(17, attractTimeout.TimeoutDuration);
            rig.Logs.AssertNoWarningsOrErrors();
        }

        // (f) GetCurrentConfigurationData is the complement of Configure
        [UnityTest]
        public IEnumerator RLMGLogger_GetCurrentConfigurationData_ReflectsConfigure()
        {
            RLMGLogger logger = CreateSingletonWaitingForConfig<RLMGLogger>();
            yield return null;

            logger.Configure(new RLMGLoggerConfigurationData
            {
                verbosity = LogType.Warning,
                logDestinationPath = LogDestinationPath.Desktop,
                logFolderName = "Test Logs",
                logFileName = "test_log",
                maxDays = 3,
                doLogFilePerSession = true,
                doLogDebugLogs = true,
                debugLogVerbosity = LogType.Error,
                doLogDebugStackTrace = true,
            });

            RLMGLoggerConfigurationData current = logger.GetCurrentConfigurationData();

            Assert.AreEqual(LogType.Warning, current.verbosity);
            Assert.AreEqual(LogType.Warning, current.LogLevel, "logLevel string should agree with verbosity");
            Assert.AreEqual(LogDestinationPath.Desktop, current.logDestinationPath);
            Assert.AreEqual(LogDestinationPath.Desktop, current.LogLocation, "logLocation string should agree with logDestinationPath");
            Assert.AreEqual("Test Logs", current.logFolderName);
            Assert.AreEqual("test_log", current.logFileName);
            Assert.AreEqual(3, current.maxDays);
            Assert.AreEqual(true, current.doLogFilePerSession);
            Assert.AreEqual(true, current.doLogDebugLogs);
            Assert.AreEqual(LogType.Error, current.debugLogVerbosity);
            Assert.AreEqual(LogType.Error, current.DebugLogLevel);
            Assert.AreEqual(true, current.doLogDebugStackTrace);
        }

        [UnityTest]
        public IEnumerator AppManager_GetCurrentConfigurationData_ReflectsConfigure()
        {
            AppManager appManager = CreateSingletonWaitingForConfig<AppManager>();
            yield return null;

            DisplayData[] displays = { new DisplayData { dimensions = new Vector2Int(1280, 720), doFullScreen = false } };

            appManager.Configure(new AppManagerConfigurationData
            {
                displaysData = displays,
                doDebugLogSetup = false,
                vSyncCount = 0,
                targetFrameRate = 24,
            });

            AppManagerConfigurationData current = appManager.GetCurrentConfigurationData();

            Assert.AreSame(displays, current.displaysData);
            Assert.AreEqual(false, current.doDebugLogSetup);
            Assert.AreEqual(0, current.vSyncCount);
            Assert.AreEqual(24, current.targetFrameRate);
        }

        /// <summary>
        /// Same approach as WaitForConfigInitializationTests: destroys any existing instance first so the
        /// singleton doesn't destroy this one as a duplicate, and waits for config so Configure applies.
        /// </summary>
        private T CreateSingletonWaitingForConfig<T>() where T : MonoBehaviour
        {
#if UNITY_6000_5_OR_NEWER
            foreach (T existing in Object.FindObjectsByType<T>(FindObjectsInactive.Include))
                Object.DestroyImmediate(existing.gameObject);
#else
            foreach (T existing in Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID))
                Object.DestroyImmediate(existing.gameObject);
#endif

            return rig.Create<T>(component => SetField(component, "doWaitForConfigToInit", true));
        }

        private static void SetField(object target, string fieldName, object value)
        {
            for (Type type = target.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return;
                }
            }

            throw new MissingFieldException(target.GetType().Name, fieldName);
        }
    }
}
