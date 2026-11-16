using UnityEngine;
using UnityEngine.SceneManagement;

namespace RoverMars.UI
{
    public sealed class StartButton : MonoBehaviour
    {
        [Header("Referencias de la pantalla de inicio")]
        [SerializeField] private UnityEngine.UI.Button _button;
        [SerializeField] private StartScreenAnimation _openingAnimation;

        private const string TutorialScene = "Tutorial_P2";
        private bool _loading;

        // Conectar este método en Button > On Click desde el Inspector.
        public void OnStartPressed()
        {
            if (_loading || _button == null || !_button.interactable)
                return;

            // Conserva la secuencia de apertura que ya existe en el proyecto.
            if (_openingAnimation != null && !_openingAnimation.IsComplete)
                return;

            if (!Application.CanStreamedLevelBeLoaded(TutorialScene))
            {
                Debug.LogError("Tutorial_P2 debe estar habilitada en la lista de escenas de compilación.", this);
                return;
            }

            _loading = true;
            _button.interactable = false;
            SceneManager.LoadScene(TutorialScene, LoadSceneMode.Single);
        }
    }
}
