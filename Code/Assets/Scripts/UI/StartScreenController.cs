using UnityEngine;

namespace RoverMars.UI
{
    public sealed class StartScreenController : MonoBehaviour
    {
        [Header("Navigation")]
        [SerializeField] private RoverMars.Navigation.SceneNavigator _sceneNavigator;
        [SerializeField] private UnityEngine.UI.Button _startButton;
        [SerializeField] private StartScreenAnimation _openingAnimation;
        [Tooltip("Full path of the destination scene included in the build scene list.")]
        [SerializeField] private string _nextScenePath;

        private void Awake()
        {
            if (_sceneNavigator == null || _startButton == null)
            {
                Debug.LogError("StartScreenController requires a navigator and a START button.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (_startButton != null)
                _startButton.onClick.AddListener(OnStartClicked);
        }

        private void OnDisable()
        {
            if (_startButton != null)
                _startButton.onClick.RemoveListener(OnStartClicked);
        }

        private void OnStartClicked()
        {
            if (_openingAnimation == null || !_openingAnimation.IsComplete || !_startButton.interactable)
                return;

            if (_sceneNavigator.NavigateTo(_nextScenePath))
                _startButton.interactable = false;
        }
    }
}
