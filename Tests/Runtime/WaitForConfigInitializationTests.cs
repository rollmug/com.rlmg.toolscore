namespace rlmg.Tools.Core.Tests
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;

    using NUnit.Framework;

    using UnityEngine;
    using UnityEngine.TestTools;

    using Object = UnityEngine.Object;

    /// <summary>
    /// Covers the wait-for-config initialization pattern shared by <see cref="RLMGLogger"/> and
    /// <see cref="AppManager"/>, plus <see cref="AppManager.ActivateDisplay"/>'s out-of-range guard.
    /// </summary>
    public class WaitForConfigInitializationTests
    {
        private const BindingFlags k_FieldFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        private readonly List<Object> ownedObjects = new();

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (Object ownedObject in ownedObjects)
            {
                if (ownedObject != null)
                    Object.Destroy(ownedObject);
            }

            ownedObjects.Clear();
            yield return null;
        }

        /// <summary>
        /// Adds <typeparamref name="T"/> to an inactive GameObject so <paramref name="doWaitForConfigToInit"/>
        /// can be set before Awake/Start run, then activates it. Destroys any existing instance first
        /// (e.g. one auto-created by other tests' logging), so the singleton doesn't destroy this one
        /// as a duplicate.
        /// </summary>
        private T CreateWithWaitForConfig<T>(bool doWaitForConfigToInit) where T : MonoBehaviour
        {
#if UNITY_6000_5_OR_NEWER
            foreach (T existing in Object.FindObjectsByType<T>(FindObjectsInactive.Include))
                Object.DestroyImmediate(existing.gameObject);
#else
            foreach (T existing in Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID))
                Object.DestroyImmediate(existing.gameObject);
#endif

            GameObject gameObject = new($"Test {typeof(T).Name}");
            gameObject.SetActive(false);
            ownedObjects.Add(gameObject);

            T component = gameObject.AddComponent<T>();
            SetField(component, "doWaitForConfigToInit", doWaitForConfigToInit);

            gameObject.SetActive(true);
            return component;
        }

        private static T GetField<T>(object target, string fieldName)
        {
            return (T)FindField(target, fieldName).GetValue(target);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FindField(target, fieldName).SetValue(target, value);
        }

        private static FieldInfo FindField(object target, string fieldName)
        {
            for (Type type = target.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo field = type.GetField(fieldName, k_FieldFlags);
                if (field != null)
                    return field;
            }

            throw new MissingFieldException(target.GetType().Name, fieldName);
        }

        [UnityTest]
        public IEnumerator RLMGLogger_WaitingForConfig_InitializesOnConfigure()
        {
            RLMGLogger logger = CreateWithWaitForConfig<RLMGLogger>(doWaitForConfigToInit: true);
            yield return null;

            Assert.IsFalse(GetField<bool>(logger, "isInitialized"), "Shouldn't initialize before Configure.");

            logger.Configure(new RLMGLoggerConfigurationData());

            Assert.IsTrue(GetField<bool>(logger, "isInitialized"));
        }

        [UnityTest]
        public IEnumerator RLMGLogger_NotWaitingForConfig_InitializesOnAwake()
        {
            RLMGLogger logger = CreateWithWaitForConfig<RLMGLogger>(doWaitForConfigToInit: false);
            yield return null;

            Assert.IsTrue(GetField<bool>(logger, "isInitialized"));
        }

        [UnityTest]
        public IEnumerator AppManager_WaitingForConfig_InitializesOnConfigure()
        {
            AppManager appManager = CreateWithWaitForConfig<AppManager>(doWaitForConfigToInit: true);
            yield return null;
            yield return null;

            Assert.IsFalse(GetField<bool>(appManager, "isInitialized"), "Shouldn't initialize on Start.");

            appManager.Configure(new AppManagerConfigurationData());

            Assert.IsTrue(GetField<bool>(appManager, "isInitialized"));
        }

        [UnityTest]
        public IEnumerator AppManager_NotWaitingForConfig_InitializesOnStart()
        {
            AppManager appManager = CreateWithWaitForConfig<AppManager>(doWaitForConfigToInit: false);
            yield return null;
            yield return null;

            Assert.IsTrue(GetField<bool>(appManager, "isInitialized"));
        }

        [Test]
        public void AppManager_ActivateDisplay_OutOfRange_DoesNotThrow()
        {
            GameObject gameObject = new("Test AppManager (inactive)");
            gameObject.SetActive(false);
            ownedObjects.Add(gameObject);
            AppManager appManager = gameObject.AddComponent<AppManager>();

            // Warns in a player; silent in the Editor, which never reports supporting displays
            if (!Application.isEditor)
                LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Can't activate display"));

            Assert.DoesNotThrow(() => appManager.ActivateDisplay(Display.displays.Length + 5));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
