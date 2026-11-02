namespace rlmg.Tools.Core.Tests
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    using NUnit.Framework;

    using UnityEngine;
    using UnityEngine.TestTools;

    using Object = UnityEngine.Object;

    /// <summary>
    /// Covers <see cref="SingletonDoNotDestroy{T}.Instance"/> and <see cref="Singleton{T}.Instance"/> under
    /// concurrent access and when reached from another behaviour's OnDisable/OnDestroy. The OnApplicationQuit and
    /// exit-Play-Mode cases need to cross the Play Mode boundary, so they live in the Editor tests'
    /// <c>SingletonPlayModeExitTests</c>.
    /// </summary>
    [TestFixture(typeof(TestSingleton))]
    [TestFixture(typeof(TestSceneSingleton))]
    public class SingletonTests<TSingleton> where TSingleton : MonoBehaviour, ITestSingleton
    {
        private const int k_ThreadCount = 16;

        private const int k_ThreadJoinTimeoutMilliseconds = 5000;

        private readonly Func<ITestSingleton> getInstance = SingletonTestUtility.GetInstanceGetter<TSingleton>();

        private readonly List<Object> ownedObjects = new();

        [SetUp]
        public void SetUp()
        {
            SingletonTestUtility.Reset<TSingleton>();
        }

        [TearDown]
        public void TearDown()
        {
            // Destroy subscribers before resetting, or their OnDisable would recreate the singleton.
            foreach (Object ownedObject in ownedObjects)
            {
                if (ownedObject != null)
                    Object.DestroyImmediate(ownedObject);
            }

            ownedObjects.Clear();
            SingletonTestUtility.Reset<TSingleton>();
        }

        [Test]
        public void Instance_AccessedFromManyThreadsWhileInstanceExists_ReturnsExistingInstanceOnEveryThread()
        {
            ITestSingleton existing = getInstance();

            List<SingletonAccessRecord> records = AccessInstanceConcurrently(doIncludeMainThread: false);

            foreach (SingletonAccessRecord record in records)
            {
                Assert.IsNull(record.Exception, $"A thread threw while reading an existing instance: {record.Exception}");
                Assert.AreSame(existing, record.Instance, "A thread got back something other than the existing instance.");
            }

            Assert.AreEqual(1, SingletonTestUtility.CountInstances<TSingleton>());
        }

        [Test]
        public void Instance_AccessedFromManyThreadsWhileNoInstanceExists_CreatesOnlyOneInstance()
        {
            List<SingletonAccessRecord> records = AccessInstanceConcurrently(doIncludeMainThread: true);

            // Unity's object APIs throw UnityException off the main thread, so a background thread that wins the
            // race can't create an instance. That's acceptable; any other failure, or a second instance, is not.
            foreach (SingletonAccessRecord record in records)
            {
                if (record.Exception != null)
                    Assert.IsInstanceOf<UnityException>(record.Exception, $"Unexpected exception: {record.Exception}");
            }

            Assert.AreEqual(1, SingletonTestUtility.CountInstances<TSingleton>(), "Concurrent access created more than one instance.");

            ITestSingleton[] returnedInstances = records
                .Select(record => record.Instance)
                .Where(instance => !ReferenceEquals(instance, null))
                .Distinct()
                .ToArray();

            Assert.AreEqual(1, returnedInstances.Length, "Threads got back different instances.");
            Assert.AreSame(getInstance(), returnedInstances[0]);
        }

        [UnityTest]
        public IEnumerator Instance_AccessedInOtherBehavioursOnDisableAndOnDestroy_ReturnsExistingInstanceAndAllowsUnsubscribing()
        {
            ITestSingleton existing = getInstance();

            TestSingletonSubscriber subscriber = SingletonTestUtility.CreateAccessor<TestSingletonSubscriber, TSingleton>();
            ownedObjects.Add(subscriber.gameObject);

            Assert.AreEqual(2, existing.SubscriberCount, "Precondition: the subscriber should have subscribed to both events.");

            Object.Destroy(subscriber.gameObject);
            yield return null;

            AssertUnsubscribedThrough(subscriber.OnDisableAccess, existing, "OnDisable");
            AssertUnsubscribedThrough(subscriber.OnDestroyAccess, existing, "OnDestroy");
            Assert.AreEqual(0, existing.SubscriberCount, "The subscriber's handlers are still attached.");
            Assert.AreEqual(1, SingletonTestUtility.CountInstances<TSingleton>());
        }

        /// <summary>
        /// Shared with the Editor tests, which make the same checks after exiting Play Mode.
        /// </summary>
        public static void AssertUnsubscribedThrough(SingletonAccessRecord record, ITestSingleton expected, string callbackName)
        {
            Assert.IsTrue(record.WasAttempted, $"{callbackName} never ran.");
            Assert.IsNull(record.Exception, $"Unsubscribing in {callbackName} threw: {record.Exception}");
            Assert.AreSame(expected, record.Instance, $"Instance in {callbackName} wasn't the existing instance.");
        }

        /// <summary>
        /// Releases <see cref="k_ThreadCount"/> threads at once (plus the main thread, if requested) to read
        /// the singleton's <c>Instance</c>, and returns what each one got.
        /// </summary>
        private List<SingletonAccessRecord> AccessInstanceConcurrently(bool doIncludeMainThread)
        {
            ConcurrentBag<SingletonAccessRecord> records = new();
            using Barrier barrier = new(k_ThreadCount + (doIncludeMainThread ? 1 : 0));

            Thread[] threads = Enumerable.Range(0, k_ThreadCount)
                .Select(_ => new Thread(() =>
                {
                    SingletonAccessRecord record = new();
                    barrier.SignalAndWait();
                    record.Record(getInstance);
                    records.Add(record);
                }))
                .ToArray();

            foreach (Thread thread in threads)
                thread.Start();

            if (doIncludeMainThread)
            {
                SingletonAccessRecord mainThreadRecord = new();
                barrier.SignalAndWait();
                mainThreadRecord.Record(getInstance);
                records.Add(mainThreadRecord);
            }

            foreach (Thread thread in threads)
                Assert.IsTrue(thread.Join(k_ThreadJoinTimeoutMilliseconds), "A thread didn't finish; Instance may be deadlocked.");

            return records.ToList();
        }
    }
}
