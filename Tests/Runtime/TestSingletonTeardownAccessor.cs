namespace rlmg.Tools.Core.Tests
{
    /// <summary>
    /// Reads the test singleton's <c>Instance</c> only in OnDisable/OnDestroy, without having touched it before.
    /// </summary>
    public class TestSingletonTeardownAccessor : TestSingletonAccessor
    {
        public SingletonAccessRecord OnDisableAccess { get; } = new();

        public SingletonAccessRecord OnDestroyAccess { get; } = new();

        private void OnDisable()
        {
            OnDisableAccess.Record(InstanceGetter);
        }

        private void OnDestroy()
        {
            OnDestroyAccess.Record(InstanceGetter);
        }
    }
}
