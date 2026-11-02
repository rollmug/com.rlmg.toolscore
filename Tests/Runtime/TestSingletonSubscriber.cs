namespace rlmg.Tools.Core.Tests
{
    /// <summary>
    /// Subscribes to the test singleton's events in Awake/OnEnable and unsubscribes through its
    /// <c>Instance</c> in OnDisable/OnDestroy, recording what it got back.
    /// </summary>
    public class TestSingletonSubscriber : TestSingletonAccessor
    {
        public SingletonAccessRecord OnDisableAccess { get; } = new();

        public SingletonAccessRecord OnDestroyAccess { get; } = new();

        private void Awake()
        {
            InstanceGetter().SomethingElseHappened += OnSomethingElseHappened;
        }

        private void OnEnable()
        {
            InstanceGetter().SomethingHappened += OnSomethingHappened;
        }

        private void OnDisable()
        {
            OnDisableAccess.Record(() =>
            {
                ITestSingleton instance = InstanceGetter();
                instance.SomethingHappened -= OnSomethingHappened;
                return instance;
            });
        }

        private void OnDestroy()
        {
            OnDestroyAccess.Record(() =>
            {
                ITestSingleton instance = InstanceGetter();
                instance.SomethingElseHappened -= OnSomethingElseHappened;
                return instance;
            });
        }

        private void OnSomethingHappened() { }

        private void OnSomethingElseHappened() { }
    }
}
