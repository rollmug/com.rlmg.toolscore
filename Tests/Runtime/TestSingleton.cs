namespace rlmg.Tools.Core.Tests
{
    using System;

    /// <summary>
    /// Minimal <see cref="SingletonDoNotDestroy{T}"/> subclass for the singleton tests.
    /// </summary>
    public class TestSingleton : SingletonDoNotDestroy<TestSingleton>, ITestSingleton
    {
        public event Action SomethingHappened;

        public event Action SomethingElseHappened;

        public int SubscriberCount =>
            (SomethingHappened?.GetInvocationList().Length ?? 0) +
            (SomethingElseHappened?.GetInvocationList().Length ?? 0);
    }
}
