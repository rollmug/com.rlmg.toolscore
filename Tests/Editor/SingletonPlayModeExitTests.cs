namespace rlmg.Tools.Core.Tests.Editor
{
    using System;
    using System.Collections;

    using NUnit.Framework;

    using UnityEditor;
    using UnityEngine;
    using UnityEngine.TestTools;

    using Object = UnityEngine.Object;

    /// <summary>
    /// Covers what <see cref="SingletonDoNotDestroy{T}"/> and <see cref="Singleton{T}"/> leave behind when Play Mode
    /// exits: OnApplicationQuit, then OnDisable/OnDestroy in an order Unity doesn't guarantee. These are Edit Mode
    /// tests so they can enter and exit Play Mode themselves.
    ///
    /// The singletons only support Play Mode with domain reload enabled, so these are ignored otherwise.
    /// </summary>
    [TestFixture(typeof(TestSingleton))]
    [TestFixture(typeof(TestSceneSingleton))]
    public class SingletonPlayModeExitTests<TSingleton> where TSingleton : MonoBehaviour, ITestSingleton
    {
        private readonly Func<ITestSingleton> getInstance = SingletonTestUtility.GetInstanceGetter<TSingleton>();

        [SetUp]
        public void SetUp()
        {
            if ((EditorSettings.enterPlayModeOptions & EnterPlayModeOptions.DisableDomainReload) != 0)
                Assert.Ignore("The singletons require domain reload on entering Play Mode.");

            SingletonTestUtility.Reset<TSingleton>();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (EditorApplication.isPlaying)
                yield return new ExitPlayMode();

            SingletonTestUtility.Reset<TSingleton>();
        }

        [UnityTest]
        public IEnumerator Instance_AccessedInOnApplicationQuitWhileInstanceExists_LeavesNoInstanceAfterExitingPlayMode()
        {
            yield return EnterPlayModeWithDomainReload();

            // OnApplicationQuit order isn't guaranteed, so put accessors on both sides of the singleton.
            TestSingletonQuitAccessor accessorCreatedBefore = SingletonTestUtility.CreateAccessor<TestSingletonQuitAccessor, TSingleton>();
            Assert.IsTrue((Object)getInstance() != null, "Precondition: an instance should exist.");
            TestSingletonQuitAccessor accessorCreatedAfter = SingletonTestUtility.CreateAccessor<TestSingletonQuitAccessor, TSingleton>();

            yield return new ExitPlayMode();

            AssertAccessedWithoutException(accessorCreatedBefore.OnApplicationQuitAccess, "OnApplicationQuit");
            AssertAccessedWithoutException(accessorCreatedAfter.OnApplicationQuitAccess, "OnApplicationQuit");
            AssertNoInstanceRemains();
        }

        [UnityTest]
        public IEnumerator Instance_AccessedInOnApplicationQuitWhileNoInstanceExists_LeavesNoInstanceAfterExitingPlayMode()
        {
            yield return EnterPlayModeWithDomainReload();

            TestSingletonQuitAccessor accessor = SingletonTestUtility.CreateAccessor<TestSingletonQuitAccessor, TSingleton>();

            yield return new ExitPlayMode();

            AssertAccessedWithoutException(accessor.OnApplicationQuitAccess, "OnApplicationQuit");
            AssertNoInstanceRemains();
        }

        [UnityTest]
        public IEnumerator Instance_AccessedInOnDisableAndOnDestroyWhileExitingPlayMode_ReturnsExistingInstanceAndAllowsUnsubscribing()
        {
            yield return EnterPlayModeWithDomainReload();

            // Teardown order isn't guaranteed either, so put subscribers on both sides of the singleton.
            GameObject subscriberObjectCreatedBefore = new("Test Singleton Subscriber (created before)");
            subscriberObjectCreatedBefore.SetActive(false);

            ITestSingleton existing = getInstance();

            TestSingletonSubscriber subscriberCreatedBefore =
                SingletonTestUtility.CreateAccessor<TestSingletonSubscriber, TSingleton>(subscriberObjectCreatedBefore);
            TestSingletonSubscriber subscriberCreatedAfter = SingletonTestUtility.CreateAccessor<TestSingletonSubscriber, TSingleton>();

            Assert.AreEqual(4, existing.SubscriberCount, "Precondition: both subscribers should have subscribed to both events.");

            yield return new ExitPlayMode();

            foreach (TestSingletonSubscriber subscriber in new[] { subscriberCreatedBefore, subscriberCreatedAfter })
            {
                SingletonTests<TSingleton>.AssertUnsubscribedThrough(subscriber.OnDisableAccess, existing, "OnDisable");
                SingletonTests<TSingleton>.AssertUnsubscribedThrough(subscriber.OnDestroyAccess, existing, "OnDestroy");
            }

            Assert.AreEqual(0, existing.SubscriberCount, "Subscribers' handlers are still attached.");
        }

        [UnityTest]
        public IEnumerator Instance_AccessedInOnDisableAndOnDestroyWhileExitingPlayModeWithNoInstance_DoesNotCreateInstance()
        {
            yield return EnterPlayModeWithDomainReload();

            TestSingletonTeardownAccessor accessor = SingletonTestUtility.CreateAccessor<TestSingletonTeardownAccessor, TSingleton>();

            yield return new ExitPlayMode();

            AssertAccessedWithoutException(accessor.OnDisableAccess, "OnDisable");
            AssertAccessedWithoutException(accessor.OnDestroyAccess, "OnDestroy");
            Assert.IsTrue((Object)accessor.OnDisableAccess.Instance == null, "Instance in OnDisable returned an instance.");
            Assert.IsTrue((Object)accessor.OnDestroyAccess.Instance == null, "Instance in OnDestroy returned an instance.");
            AssertNoInstanceRemains();
        }

        /// <summary>
        /// Locals set before this are lost to the domain reload, so tests create their objects after it.
        /// </summary>
        private static EnterPlayMode EnterPlayModeWithDomainReload()
        {
            return new EnterPlayMode(expectDomainReload: true);
        }

        private static void AssertAccessedWithoutException(SingletonAccessRecord record, string callbackName)
        {
            Assert.IsTrue(record.WasAttempted, $"{callbackName} never ran.");
            Assert.IsNull(record.Exception, $"Accessing Instance in {callbackName} threw: {record.Exception}");
        }

        /// <summary>
        /// The static field may still reference the destroyed instance until the next Play session's domain
        /// reload clears it; what matters is that no live instance outlives Play Mode.
        /// </summary>
        private static void AssertNoInstanceRemains()
        {
            Assert.AreEqual(0, SingletonTestUtility.CountInstances<TSingleton>(), $"A {typeof(TSingleton).Name} outlived Play Mode.");
        }
    }
}
