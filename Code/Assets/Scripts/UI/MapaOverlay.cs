using UnityEngine;

namespace RoverMars.UI
{
    public sealed class MapaOverlay : MonoBehaviour, UnityEngine.ICanvasRaycastFilter
    {
        [Header("Referencias")]
        [SerializeField] private RoomManager _roomManager;
        [SerializeField] private GameObject _panel;
        [SerializeField] private GameObject _panelVineta;
        [SerializeField] private VinetaManager _vinetaManager;
        [SerializeField] private UnityEngine.UI.RectMask2D _recorteBotonMapa;
        [Header("Capas y escenario")]
        [SerializeField] private Transform[] _capasCanvas;
        [SerializeField] private CanvasGroup _centroControlGrupo;
        [SerializeField] private CanvasGroup _invernaderoGrupo;
        [SerializeField] private UnityEngine.UI.Image _fondoHabitacion;
        [SerializeField] private Sprite _fondoCentroControl;
        [SerializeField] private UnityEngine.UI.Image _candadoHall;
        private Sprite _candadoHallGenerado;
        private bool _escenarioBloqueado;

        private void Awake()
        {
            OrdenarCanvas();
            if (_candadoHall != null)
            {
                _candadoHallGenerado = PixelArt.CrearCandado();
                _candadoHall.sprite = _candadoHallGenerado;
                _candadoHall.raycastTarget = false;
                _candadoHall.rectTransform.sizeDelta = new Vector2(64f, 64f);
                _candadoHall.gameObject.SetActive(true);
            }
        }
        private void OrdenarCanvas()
        {
            if (_capasCanvas == null) return;
            int indice = 0;
            foreach (Transform capa in _capasCanvas)
                if (capa != null && capa.parent == transform.parent) capa.SetSiblingIndex(indice++);
        }
        private void BloquearEscenario(bool bloquear)
        {
            _escenarioBloqueado = bloquear;
            if (_invernaderoGrupo != null)
                _invernaderoGrupo.interactable = _invernaderoGrupo.blocksRaycasts = !bloquear;
            if (_centroControlGrupo == null) return;
            _centroControlGrupo.interactable = _centroControlGrupo.blocksRaycasts = !bloquear;
            // RoomManager sigue controlando el fondo. Se restaura solo la interfaz de ese cuarto.
            bool esCentro = _fondoHabitacion != null && _fondoHabitacion.sprite == _fondoCentroControl;
            _centroControlGrupo.gameObject.SetActive(!bloquear && esCentro);
        }

        [Header("Bloqueo durante las explicaciones")]
        [SerializeField] private bool _bloquearConVineta = true;
        [Header("Plano con fondo")]
        [SerializeField] private UnityEngine.UI.Image _imagenMapa;
        [SerializeField, Range(0f, 0.4f)] private float margenMapa = 0.08f;

        private void AjustarMapa()
        {
            if (_imagenMapa == null || _imagenMapa.sprite == null) return;
            OrdenarCanvas();
            Canvas.ForceUpdateCanvases();
            var padre = (RectTransform)_imagenMapa.transform.parent;
            Vector2 disponible = padre.rect.size * (1f - 2f * margenMapa);
            Vector2 dibujo = _imagenMapa.sprite.rect.size;
            float escala = Mathf.Min(disponible.x / dibujo.x, disponible.y / dibujo.y);
            var rect = _imagenMapa.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = dibujo * escala;
        }

        private UnityEngine.InputSystem.InputAction _escapeAction;
        private bool _escapeSolicitado;

        public bool EstaAbierto => _panel != null && _panel.activeSelf;
        public bool PuedeInteractuar => EstaAbierto && !VinetaAbierta();
        public event System.Action OnMapaAbierto;
        private void OnEnable()
        {
            AjustarMapa();
            BloquearEscenario(true);
            if (_escapeAction == null)
            {
                _escapeAction = new UnityEngine.InputSystem.InputAction(
                    "CerrarMapa", UnityEngine.InputSystem.InputActionType.Button, "<Keyboard>/escape");
                _escapeAction.performed += OnEscape;
            }
            _escapeAction.Enable();
        }

        private void OnDisable()
        {
            _escapeAction?.Disable();
            _escapeSolicitado = false;
            if (_escenarioBloqueado) BloquearEscenario(false);
        }

        private void OnDestroy()
        {
            PixelArt.Liberar(_candadoHallGenerado);
            if (_escapeAction != null)
            {
                _escapeAction.performed -= OnEscape;
                _escapeAction.Dispose();
            }
        }

        private void OnEscape(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            _escapeSolicitado = true;
        }

        private void Update()
        {
            // La acción del nuevo Input System solicita el cierre con Esc.
            if (_escapeSolicitado)
            {
                _escapeSolicitado = false;
                CerrarMapa();
            }
        }

        public void AbrirMapa()
        {
            if (_panel == null || _roomManager == null)
            {
                Debug.LogError("Asigna el panel y RoomManager en MapaOverlay.", this);
                return;
            }
            if (VinetaAbierta() || EstaAbierto)
                return;
            OrdenarCanvas();
            _panel.SetActive(true);
            BloquearEscenario(true);
            OnMapaAbierto?.Invoke();
        }

        public void CerrarMapa()
        {
            if (_panel == null)
                return;
            foreach (HotspotHabitacion hotspot in _panel.GetComponentsInChildren<HotspotHabitacion>(true))
                hotspot.OcultarBorde();
            _panel.SetActive(false);
        }

        public void AlternarMapa()
        {
            if (EstaAbierto)
                CerrarMapa();
            else
                AbrirMapa();
        }

        // La selección de habitación ahora se realiza desde HotspotHabitacion.

private bool VinetaAbierta()
{
    return _bloquearConVineta &&
        ((_vinetaManager != null && _vinetaManager.VinetaAbierta) ||
         (_panelVineta != null && _panelVineta.activeInHierarchy));
}

        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            // Se aplica al fondo oscuro y a sus hijos. Deja pasar los clics
            // en el botón Mapa del HUD, aunque el overlay esté por encima.
            if (_recorteBotonMapa == null)
                return true;
            RectTransform rectTransform = _recorteBotonMapa.rectTransform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform, screenPoint, eventCamera, out Vector2 point))
                return true;
            Rect rect = rectTransform.rect;
            Vector4 padding = _recorteBotonMapa.padding;
            Rect visible = Rect.MinMaxRect(rect.xMin + padding.x, rect.yMin + padding.y,
                rect.xMax - padding.z, rect.yMax - padding.w);
            return !visible.Contains(point);
        }
    }
}
