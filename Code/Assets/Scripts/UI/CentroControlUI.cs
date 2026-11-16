using UnityEngine;

namespace RoverMars.UI
{
    public sealed class CentroControlUI : MonoBehaviour, ICanvasRaycastFilter
    {
        [Header("Referencias de la interfaz")]
        [SerializeField] private UnityEngine.UI.Button _botonHelp;
        [SerializeField] private VinetaManager _vinetaManager;
        [SerializeField] private MapaOverlay _mapaOverlay;
        [SerializeField] private RectTransform _hud;
        [SerializeField] private RadLabUI _radLab;

        private bool PuedeInteractuar =>
            (_vinetaManager == null || !_vinetaManager.VinetaAbierta) &&
            (_mapaOverlay == null || !_mapaOverlay.EstaAbierto);

        private void OnEnable()
        {
            // MapaOverlay es quien garantiza el orden de las capas del Canvas.
            ActualizarBloqueo();
        }

        private void LateUpdate()
        {
            // No cambiamos el mapa ni las viñetas: solo consultamos su estado.
            ActualizarBloqueo();
        }

        private void ActualizarBloqueo()
        {
            if (_botonHelp != null)
                _botonHelp.interactable = PuedeInteractuar;
        }

        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            // RadLab sigue recibiendo otro clic para cerrar; Help conserva su bloqueo.
            if (_radLab != null && _radLab.EstaAbierto && _radLab.EsPuntoBoton(screenPoint, eventCamera))
                return true;
            return PuedeInteractuar;
        }

        public void OnHelpPresionado()
        {
            // TODO: acción del botón Help (pendiente)
        }
    }
}
