using UnityEngine;

namespace RoverMars.UI
{
    // Este componente vive en un objeto ACTIVO fuera de InvernaderoUI.
    // Su botón e imágenes sí viven dentro de InvernaderoUI: al salir solo se ocultan.
    public sealed class Planta : MonoBehaviour
    {
        public enum Estado { Brote, Creciendo, Madura }

        [Header("Sprites y crecimiento")]
        [SerializeField] private Sprite spriteBrote;
        [SerializeField] private Sprite spriteMadura;
        [SerializeField, Min(0.01f)] private float duracionCrecimiento = 600f;
        [SerializeField, Min(1)] private int escalaPixel = 4;

        [Header("Vista dentro de InvernaderoUI")]
        [SerializeField] private UnityEngine.UI.Button _boton;
        [SerializeField] private UnityEngine.UI.Image _imagen;
        [SerializeField] private GameObject _barra;
        [SerializeField] private UnityEngine.UI.Image _relleno;

        [Header("Bloqueos y recompensa")]
        [SerializeField] private TutorialFlujo _tutorial;
        [SerializeField] private VinetaManager _vinetas;
        [SerializeField] private MapaOverlay _mapa;
        [SerializeField] private BarrasEstado _barrasEstado;

        private float _tiempoCrecimiento;
        public Estado EstadoActual { get; private set; } = Estado.Brote;
        public float ProgresoCrecimiento => Mathf.Clamp01(_tiempoCrecimiento / Mathf.Max(0.01f, duracionCrecimiento));
        public bool PuedeInteractuar => _boton != null && _boton.gameObject.activeInHierarchy &&
            (_tutorial == null || !_tutorial.TutorialActivo) &&
            (_vinetas == null || !_vinetas.VinetaAbierta) && (_mapa == null || !_mapa.EstaAbierto);

        public event System.Action<Planta> OnPulsada;
        public event System.Action OnRegada;
        public event System.Action OnMadura;
        public event System.Action OnCosechada;

        private void Awake()
        {
            if (_boton == null || _imagen == null || _barra == null || _relleno == null ||
                spriteBrote == null || spriteMadura == null || _barrasEstado == null)
            {
                Debug.LogError("Asigna sprites, botón, imagen, barra y BarrasEstado en Planta.", this);
                enabled = false;
                return;
            }
            _boton.onClick.AddListener(Pulsar);
            _imagen.raycastTarget = true;
            _imagen.alphaHitTestMinimumThreshold = 0.1f;
            MostrarEstado();
        }
        private void Update()
        {
            // No consulta habitación, mapa ni viñetas: cada reloj avanza por separado.
            if (EstadoActual != Estado.Creciendo) return;
            _tiempoCrecimiento += Time.deltaTime;
            _relleno.fillAmount = ProgresoCrecimiento;
            if (ProgresoCrecimiento < 1f) return;
            EstadoActual = Estado.Madura;
            MostrarEstado();
            OnMadura?.Invoke();
        }
        private void OnDestroy()
        {
            if (_boton != null) _boton.onClick.RemoveListener(Pulsar);
        }
        private void Pulsar()
        {
            // El botón detecta el clic sobre sus píxeles opacos. Sin herramienta no hay acción.
            if (PuedeInteractuar) OnPulsada?.Invoke(this);
        }
        public bool Regar()
        {
            if (!PuedeInteractuar || EstadoActual != Estado.Brote) return false;
            EstadoActual = Estado.Creciendo;
            _tiempoCrecimiento = 0f;
            MostrarEstado();
            OnRegada?.Invoke();
            return true;
        }
        public bool Cosechar()
        {
            if (!PuedeInteractuar || EstadoActual != Estado.Madura) return false;
            EstadoActual = Estado.Brote;
            _tiempoCrecimiento = 0f;
            MostrarEstado();
            // No se modifica agua, energía ni progreso de misiones.
            _barrasEstado.CambiarSalud(10f);
            OnCosechada?.Invoke();
            return true;
        }
        private void MostrarEstado()
        {
            _imagen.sprite = EstadoActual == Estado.Madura ? spriteMadura : spriteBrote;
            // Tamaño nativo por una escala entera; el pivote conserva la base de la planta.
            _imagen.rectTransform.sizeDelta = _imagen.sprite.rect.size * escalaPixel;
            _barra.SetActive(EstadoActual == Estado.Creciendo);
            _relleno.fillAmount = ProgresoCrecimiento;
        }
    }
}
