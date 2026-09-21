namespace rlmg.Tools.Core.Examples
{
    using UnityEngine;
    using UnityEngine.Networking;
    using TMPro;
    using System;

    public class ExampleRLMGConfigLoaderListener : MonoBehaviour
    {
        RLMGConfigLoader loader;

        [SerializeField]
        TMP_Text display;

        private void Awake()
        {
            loader = FindAnyObjectByType<RLMGConfigLoader>();
        }

        private void OnEnable()
        {
            if (loader != null)
                loader.AnyLoadSucceeded.AddListener(OnAnySuccess);
        }

        private void OnDisable()
        {
            if (loader != null)
                loader.AnyLoadSucceeded.RemoveListener(OnAnySuccess);
        }

        private void OnAnySuccess(UnityWebRequest unityWebRequest)
        {
            if (display != null)
                display.text = unityWebRequest.downloadHandler.text;
        }
    }

}