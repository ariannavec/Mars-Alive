using System.Collections;
using UnityEngine;

namespace RoverMars.UI
{
    [DefaultExecutionOrder(-100)]
    public sealed class TutorialFlujo : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private MensajeCentral _mensajeCentral;
        [SerializeField] private VinetaManager _vinetas;
        [SerializeField] private BarrasEstado _barrasEstado;
        [SerializeField] private UnityEngine.UI.Button _botonMapa;
        [SerializeField] private CanvasGroup _bloqueador;
        [Header("Mensajes")]
        [TextArea(4, 10)]
        [SerializeField] private string bienvenida = "Welcome to Mars, Astronaut! You have just arrived at our Red Planet outpost. Your mission is to survive 30 Sols (Martian days) and collect 9 puzzle pieces to prove that life can survive on Mars. Before you start your mission, here is a quick guide to your screen controls:";
        [SerializeField] private string despedida = "Good Luck!";
        private Coroutine _flujo;
        private bool _barrasHabilitadas, _mapaInteractuable;
        public bool TutorialActivo { get; private set; }
        public event System.Action OnTutorialTerminado;
        private void Awake()
        {
            // Solo se suspende Update. BarrasEstado conserva sus valores y lógica.
            _barrasHabilitadas = _barrasEstado.enabled;
            _mapaInteractuable = _botonMapa.interactable;
            EstablecerBloqueo(true);
        }
        private void Start() => IniciarTutorial();
        public void IniciarTutorial()
        {
            if (_flujo != null) return;
            EstablecerBloqueo(true);
            _flujo = StartCoroutine(EjecutarTutorial());
        }
        private IEnumerator EjecutarTutorial()
        {
            // RoomManager y el HUD completan primero sus Start.
            yield return null;
            yield return _mensajeCentral.Mostrar(bienvenida);
            _vinetas.IniciarTutorial();
            while (_vinetas.SecuenciaEnCurso) yield return null;
            yield return _mensajeCentral.Mostrar(despedida);
            EstablecerBloqueo(false);
            _flujo = null;
            OnTutorialTerminado?.Invoke();
        }
        private void EstablecerBloqueo(bool bloquear)
        {
            TutorialActivo = bloquear;
            _vinetas.BloquearDuranteFlujo(bloquear);
            _barrasEstado.enabled = !bloquear && _barrasHabilitadas;
            _botonMapa.interactable = !bloquear && _mapaInteractuable;
            // Capa transparente: el HUD permanece visible, pero no recibe clics.
            _bloqueador.blocksRaycasts = bloquear;
            _bloqueador.interactable = bloquear;
        }
        private void OnDestroy()
        {
            if (_vinetas != null) _vinetas.BloquearDuranteFlujo(false);
            if (_barrasEstado != null) _barrasEstado.enabled = _barrasHabilitadas;
        }
    }
}
