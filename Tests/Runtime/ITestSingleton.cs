namespace rlmg.Tools.Core.Tests
{
    using System;

    /// <summary>
    /// What the singleton tests need from a test subclass, so the same fixtures cover
    /// <see cref="SingletonDoNotDestroy{T}"/> and <see cref="Singleton{T}"/>.
    /// </summary>
    public interface ITestSingleton
    {
        event Action SomethingHappened;

        event Action SomethingElseHappened;

        int SubscriberCount { get; }
    }
}
