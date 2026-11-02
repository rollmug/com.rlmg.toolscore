namespace rlmg.Tools.Core.Tests
{
    /// <summary>
    /// Reads the test singleton's <c>Instance</c> in OnApplicationQuit.
    /// </summary>
    public class TestSingletonQuitAccessor : TestSingletonAccessor
    {
        public SingletonAccessRecord OnApplicationQuitAccess { get; } = new();

        private void OnApplicationQuit()
        {
            OnApplicationQuitAccess.Record(InstanceGetter);
        }
    }
}
