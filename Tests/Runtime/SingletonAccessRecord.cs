namespace rlmg.Tools.Core.Tests
{
    using System;

    /// <summary>
    /// Records the outcome of one attempt to reach a test singleton's <c>Instance</c>, so tests can
    /// inspect it after the accessing behaviour (or thread) is gone.
    /// </summary>
    public sealed class SingletonAccessRecord
    {
        public bool WasAttempted { get; private set; }

        public ITestSingleton Instance { get; private set; }

        public Exception Exception { get; private set; }

        public void Record(Func<ITestSingleton> access)
        {
            WasAttempted = true;

            try
            {
                Instance = access();
            }
            catch (Exception e)
            {
                Exception = e;
            }
        }
    }
}
