namespace rlmg.Tools.Core.Tests
{
    using System;

    using UnityEngine;

    /// <summary>
    /// Base for behaviours that reach a test singleton from their lifecycle callbacks.
    /// <see cref="InstanceGetter"/> picks which singleton's <c>Instance</c> they read; create them via
    /// <see cref="SingletonTestUtility.CreateAccessor{TAccessor, TSingleton}"/> so it's set before Awake.
    /// </summary>
    public abstract class TestSingletonAccessor : MonoBehaviour
    {
        public Func<ITestSingleton> InstanceGetter { get; set; }
    }
}
