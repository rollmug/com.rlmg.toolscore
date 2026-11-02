namespace rlmg.Tools.Core.Tests
{
    using System;
    using System.Linq;
    using System.Reflection;

    using UnityEngine;

    using Object = UnityEngine.Object;

    /// <summary>
    /// Shared setup for the singleton tests: reaching each test singleton's <c>Instance</c>, creating
    /// accessors, and resetting static state, which otherwise carries over between tests in the same Play session.
    /// </summary>
    public static class SingletonTestUtility
    {
        private const BindingFlags k_StaticFieldFlags = BindingFlags.Static | BindingFlags.NonPublic;

        /// <summary>
        /// Reads <typeparamref name="TSingleton"/>'s static <c>Instance</c>, which is declared on a different base
        /// class for each test singleton.
        /// </summary>
        public static Func<ITestSingleton> GetInstanceGetter<TSingleton>() where TSingleton : MonoBehaviour, ITestSingleton
        {
            if (typeof(TSingleton) == typeof(TestSingleton))
                return () => TestSingleton.Instance;

            if (typeof(TSingleton) == typeof(TestSceneSingleton))
                return () => TestSceneSingleton.Instance;

            throw new NotSupportedException($"No Instance getter for {typeof(TSingleton).Name}.");
        }

        /// <summary>
        /// Adds <typeparamref name="TAccessor"/> to an inactive GameObject so its <see cref="TestSingletonAccessor.InstanceGetter"/>
        /// is set before Awake/OnEnable run, then activates it. Pass <paramref name="inactiveGameObject"/> to control
        /// when the GameObject itself was created relative to the singleton.
        /// </summary>
        public static TAccessor CreateAccessor<TAccessor, TSingleton>(GameObject inactiveGameObject = null)
            where TAccessor : TestSingletonAccessor
            where TSingleton : MonoBehaviour, ITestSingleton
        {
            GameObject gameObject = inactiveGameObject != null ? inactiveGameObject : new(typeof(TAccessor).Name);
            gameObject.SetActive(false);

            TAccessor accessor = gameObject.AddComponent<TAccessor>();
            accessor.InstanceGetter = GetInstanceGetter<TSingleton>();

            gameObject.SetActive(true);
            return accessor;
        }

        /// <summary>
        /// Every <typeparamref name="T"/> that exists, active or not, including ones in DontDestroyOnLoad.
        /// </summary>
        public static int CountInstances<T>() where T : MonoBehaviour
        {
            return Resources.FindObjectsOfTypeAll<T>().Length;
        }

        /// <summary>
        /// Destroys every <typeparamref name="T"/> and clears its singleton base class's instance field.
        /// </summary>
        public static void Reset<T>() where T : MonoBehaviour
        {
            foreach (T existing in Resources.FindObjectsOfTypeAll<T>())
                Object.DestroyImmediate(existing.gameObject);

            FindInstanceField<T>().SetValue(null, null);
        }

        /// <summary>
        /// Finds the field by type rather than name, since the two singleton base classes name it differently.
        /// </summary>
        private static FieldInfo FindInstanceField<T>() where T : MonoBehaviour
        {
            return typeof(T).BaseType
                .GetFields(k_StaticFieldFlags)
                .Single(field => field.FieldType == typeof(T));
        }
    }
}
