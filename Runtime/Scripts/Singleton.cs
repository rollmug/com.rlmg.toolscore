namespace rlmg.Tools.Core
{
    using UnityEngine;

    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        protected static T instance;
  
        /**
           Retuns the instance of this singleton
        */
        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = (T)FindFirstObjectByType(typeof(T));
  
                    if (instance == null)
                    {
                        Debug.Log("An instance of " + typeof(T) + " is needed in the scene, but there is none.");
                    }
                }
  
                return instance;
            }
        }
    }
}

