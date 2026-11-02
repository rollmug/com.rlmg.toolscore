namespace rlmg.Tools.Core
{
    using System.Linq;
    using UnityEngine;

    /// <summary>
    /// Scene-scoped counterpart of <see cref="SingletonDoNotDestroy{T}"/>: same lookup, lazy creation,
    /// duplicate handling and quit safety, but the instance stays in its scene and is destroyed with it.
    ///
    /// Be aware this will not prevent a non singleton constructor
    ///   such as `T myT = new T();`
    /// To prevent that, add `protected T () {}` to your singleton class.
    /// </summary>
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        protected static T instance;

        private static object _lock = new object();

        /// <summary>
        /// Returns the instance of this singleton, creating one if none exists.
        /// </summary>
        public static T Instance
        {
            get
            {
                lock (_lock)
                {
                    if (AppQuitCheck.IsQuitting)
                    {
                        return instance;
                    }

                    if (instance == null)
                    {
                        instance = (T)FindAnyObjectByType(typeof(T));

                        if (instance == null)
                        {
                            // No GameObject with this component in the scene; create one:

                            GameObject singleton = new GameObject();
                            instance = singleton.AddComponent<T>();
                            singleton.name = "(singleton) " + typeof(T).ToString();

                            Debug.Log($"[Singleton: {typeof(T)}] An instance of '{typeof(T)}' is needed in the scene, so '{singleton}' was created.");

                            return instance;
                        }
                        else
                        {
                            Debug.Log($"[Singleton: {typeof(T)}] Setting singleton instance field. Using instance already created on gameobject: {instance.gameObject.name}.");
                        }

                        T[] allFoundInstances;

# if UNITY_5_OR_NEWER
                        allFoundInstances = FindObjectsByType<T>(FindObjectsInactive.Include);
# else
                        allFoundInstances = FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
# endif
                        if (allFoundInstances.Length > 1)
                        {
                            System.Collections.Generic.IEnumerable<T> allFoundExceptCurrent = allFoundInstances.Where(i => i != instance);

                            Debug.Log(
                                $"[Singleton: {typeof(T)}] Multiple instances ({allFoundInstances.Length}) found. Instances on the following gameobjects will be destroyed:\n" +
                                string.Join("\n",allFoundExceptCurrent.Select(i => i.gameObject.name))
                            );

                            foreach (T i in allFoundExceptCurrent)
                                Destroy(i); // component only
                        }
                    }

                    return instance;
                }
            }
        }

        public static bool HasInstance()
        {
            lock (_lock)
            {
                return instance != null;
            }
        }

        private void DestroyDuplicateInstances()
        {
            T[] allFoundInstances;

# if UNITY_5_OR_NEWER
            allFoundInstances = FindObjectsByType<T>(FindObjectsInactive.Include);
# else
            allFoundInstances = FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
# endif
            if (allFoundInstances.Length > 1)
            {
                System.Collections.Generic.IEnumerable<T> allFoundExceptCurrent = allFoundInstances.Where(i => i != instance);

                Debug.Log(
                    $"[Singleton: {typeof(T)}] Multiple instances ({allFoundInstances.Length}) found. Instances on the following gameobjects will be destroyed:\n" +
                    string.Join("\n",allFoundExceptCurrent.Select(i => i.gameObject.name))
                );

                foreach (T i in allFoundExceptCurrent)
                    Destroy(i); // component only
            }
        }

        protected virtual void Awake()
        {
            // run the getter
            var currentInstance = Instance;

            if (currentInstance != null)
                DestroyDuplicateInstances();
        }

        /// <summary>
        /// When Unity quits, it destroys objects in a random order.
        /// If any script calls Instance after it has been destroyed,
        ///   it will create a buggy ghost object that will stay on the Editor scene
        ///   even after stopping playing the Application.
        /// In the Editor, <see cref="AppQuitCheck"/> has already set the flag by now; this covers builds.
        /// </summary>
        protected virtual void OnApplicationQuit()
        {
            AppQuitCheck.MarkQuitting();
        }
    }
}
