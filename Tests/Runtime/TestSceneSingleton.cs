namespace rlmg.Tools.Core.Tests
{
    using System;

    /// <summary>
    /// Minimal <see cref="Singleton{T}"/> subclass for the singleton tests.
    /// </summary>
    public class TestSceneSingleton : Singleton<TestSceneSingleton>, ITestSingleton
    {
        public event Action SomethingHappened;

        public event Action SomethingElseHappened;

        public int SubscriberCount =>
            (SomethingHappened?.GetInvocationList().Length ?? 0) +
            (SomethingElseHappened?.GetInvocationList().Length ?? 0);
    }
}
