namespace rlmg.Tools.MediaPlayers.Examples
{
    using UnityEngine;
    using UnityEngine.UI;

    [RequireComponent(typeof(Button))]
    public class ExampleBehindAttractButton : MonoBehaviour
    {
        Button button;
        private void Awake()
        {
            button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            button.onClick.AddListener(OnClick);
        }

        private void OnDisable()
        {
            button.onClick.RemoveListener(OnClick);
        }

        private void OnClick()
        {
            Debug.Log("Main content interacted with. Attract video should block this when visible.");
        }
    }

}