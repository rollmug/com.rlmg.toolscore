namespace rlmg.Tools.Core
{
    using Codice.CM.SEIDInfo;
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

        public static T Instance
        {
            get
            {
                if (applicationIsQuitting)
                {
                    Debug.LogWarning("[Singleton: " + typeof(T) + "] Instance '" + typeof(T) +
                        "' already destroyed on application quit." +
                        " Won't create again - returning null.");
                    return null;
                }

                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = (T)FindAnyObjectByType(typeof(T));

                        if (FindObjectsByType<T>().Length > 1)
                        {
                            Debug.LogError("[Singleton: " + typeof(T) + "] Something went really wrong " +
                                " - there should never be more than 1 singleton!" +
                                " Reopening the scene might fix it.");
                            return _instance;
                        }

                        if (_instance == null)
                        {
                            GameObject singleton = new GameObject();
                            _instance = singleton.AddComponent<T>();
                            singleton.name = "(singleton) " + typeof(T).ToString();

                            DontDestroyOnLoad(singleton);

                            Debug.Log("[Singleton: " + typeof(T) + "] An instance of " + typeof(T) +
                                " is needed in the scene, so '" + singleton +
                                "' was created with DontDestroyOnLoad.");
                        }
                        else
                        {
                            if (_instance.gameObject.scene.name != "DontDestroyOnLoad")
                            {
                                DontDestroyOnLoad(_instance.gameObject);
                            }

                            Debug.Log("[Singleton: " + typeof(T) + "] Setting singleton instance field. Using instance already created on gameobject: " +
                                _instance.gameObject.name);
                        }
                    }

                    return _instance;
                }
            }
        }

        protected static bool applicationIsQuitting = false;

        protected virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this); // component only
                return;
            }
        }
        
        /// <summary>
        /// When Unity quits, it destroys objects in a random order.
        /// In principle, a Singleton is only destroyed when application quits.
        /// If any script calls Instance after it have been destroyed, 
        ///   it will create a buggy ghost object that will stay on the Editor scene
        ///   even after stopping playing the Application. Really bad!
        /// So, this was made to be sure we're not creating that buggy ghost object.
        /// </summary>
        public void OnDestroy()
        {
            if (Instance == null || Instance == this)
                applicationIsQuitting = true;
        }
    }
}