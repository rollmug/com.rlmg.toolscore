namespace rlmg.Tools.Core
{
    using System.Collections;
    using System.Linq;
    using UnityEngine;

    /// <summary>
    /// Be aware this will not prevent a non singleton constructor
    ///   such as `T myT = new T();`
    /// To prevent that, add `protected T () {}` to your singleton class.
    /// 
    /// As a note, this is made as MonoBehaviour because we need Coroutines.
    /// </summary>
    public class SingletonDoNotDestroy<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;

        private static object _lock = new object();

        protected static bool applicationIsQuitting = false;

        public static T Instance
        {
            get
            {
                lock (_lock)
                {
                    if (applicationIsQuitting)
                    {
                        Debug.LogWarning("[Singleton: " + typeof(T) + "] Instance '" + typeof(T) +
                            "' already destroyed on application quit." +
                            " Won't create again - returning null.");

                        return null;
                    }

                    if (_instance == null)
                    {
                        _instance = (T)FindAnyObjectByType(typeof(T));

                        if (_instance == null)
                        {
                            // No GameObject with this component in the scene; create one:

                            GameObject singleton = new GameObject();
                            _instance = singleton.AddComponent<T>();
                            singleton.name = "(singleton) " + typeof(T).ToString();

                            DontDestroyOnLoad(singleton);

                            Debug.Log($"[Singleton: {typeof(T)}] An instance of '{typeof(T)}' is needed in the scene, so '{singleton}' was created with DontDestroyOnLoad.");

                            return _instance;
                        }
                        else
                        {
                            if (_instance.transform.parent != null)
                            {
                                _instance.transform.SetParent(null);
                            }

                            if (_instance.gameObject.scene.name != "DontDestroyOnLoad")
                            {
                                DontDestroyOnLoad(_instance.gameObject);
                            }

                            Debug.Log($"[Singleton: {typeof(T)}] Setting singleton instance field. Using instance already created on gameobject: {_instance.gameObject.name}.");
                        }

                        T[] allFoundInstances;

# if UNITY_5_OR_NEWER
                        allFoundInstances = FindObjectsByType<T>(FindObjectsInactive.Include);
# else
                        allFoundInstances = FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
# endif
                        if (allFoundInstances.Length > 1)
                        {
                            System.Collections.Generic.IEnumerable<T> allFoundExceptCurrent = allFoundInstances.Where(i => i != _instance);

                            Debug.LogWarning(
                                $"[Singleton: {typeof(T)}] Multiple instances ({allFoundInstances.Length}) found. Instances on the following gameobjects will be destroyed:\n" +
                                string.Join("\n",allFoundExceptCurrent.Select(i => i.gameObject.name))
                            );

                            foreach (T i in allFoundExceptCurrent)
                                Destroy(i); // component only
                        }
                    }

                    return _instance;
                }
            }
        }

        protected virtual void Awake()
        {
            // run the getter
            _ = Instance;
        }
        
        /// <summary>
        /// When Unity quits, it destroys objects in a random order.
        /// In principle, a Singleton is only destroyed when application quits.
        /// If any script calls Instance after it have been destroyed, 
        ///   it will create a buggy ghost object that will stay on the Editor scene
        ///   even after stopping playing the Application. Really bad!
        /// So, this was made to be sure we're not creating that buggy ghost object.
        /// </summary>
        public void OnApplicationQuit()
        {
            lock (_lock)
            {
                applicationIsQuitting = true;
            }
        }
    }
}