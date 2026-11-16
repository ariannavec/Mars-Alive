using UnityEngine;
using System.Globalization;

namespace RoverMars.UI
{
    public sealed class RadLabUI : MonoBehaviour,
        UnityEngine.EventSystems.IPointerEnterHandler, UnityEngine.EventSystems.IPointerExitHandler
    {
        [Header("Botón y cuadro")]
        [SerializeField] private UnityEngine.UI.Button _boton;
        [SerializeField] private UnityEngine.UI.Image _imagenBoton;
        [SerializeField] private GameObject _borde;
        [SerializeField] private GameObject _raiz;
        [SerializeField] private RectTransform _cuadro;
        [SerializeField] private TMPro.TMP_Text _texto;
        [SerializeField] private TMPro.TMP_Text _textoVineta;
        [SerializeField, Min(30f)] private float margenPantalla = 30f;
        [SerializeField] private Vector2 tamanoCuadro = new Vector2(1100f, 560f);
        [Header("Estado del juego")]
        [SerializeField] private BarrasEstado _barras;
        [SerializeField] private TutorialFlujo _tutorial;
        [SerializeField] private VinetaManager _vinetas;
        [SerializeField] private MapaOverlay _mapa;
        [SerializeField] private DidYouKnowUI _didYouKnow;
        [Header("Dosis de los diez Soles (Gy)")]
        [SerializeField] private float[] dosis = { 5.3886237f, 6.036673f, 5.6504636f, 5.385223f,
            6.0067153f, 4.932495f, 7.0290446f, 6.698296f, 7.285437f, 5.987841f };
        private static readonly string[] TextosOriginales = { "5.3886237", "6.036673", "5.6504636", "5.385223",
            "6.0067153", "4.932495", "7.0290446", "6.698296", "7.285437", "5.987841" };
        private UnityEngine.InputSystem.InputAction _escape;
        private bool _bloqueoPropio;
        private Canvas _canvas;
        public bool EstaAbierto => _raiz != null && _raiz.activeSelf;
        private bool PuedeAbrir => isActiveAndEnabled && _boton.gameObject.activeInHierarchy &&
            !_tutorial.TutorialActivo && !_vinetas.VinetaAbierta && !_mapa.EstaAbierto && !_didYouKnow.EstaAbierto;
        private void Awake()
        {
            _canvas = _cuadro.GetComponentInParent<Canvas>();
            _imagenBoton.alphaHitTestMinimumThreshold = 0.1f;
            _boton.onClick.AddListener(Alternar);
            _borde.SetActive(false);
            _raiz.SetActive(false);
            // El fondo deja pasar únicamente el botón para poder cerrarlo con otro clic.
            var filtro = _raiz.AddComponent<FondoRadLab>();
            filtro.Inicializar(this, _cuadro);
            _escape = new UnityEngine.InputSystem.InputAction("CerrarRadLab", UnityEngine.InputSystem.InputActionType.Button, "<Keyboard>/escape");
            _escape.performed += OnEscape;
        }
        private void OnEnable()
        {
            _barras.OnSolTerminado += ActualizarTexto;
            _escape.Enable();
        }
        private void OnDisable()
        {
            if (_barras != null) _barras.OnSolTerminado -= ActualizarTexto;
            _escape?.Disable();
            Cerrar();
            if (_borde != null) _borde.SetActive(false);
        }
        private void OnDestroy()
        {
            if (_boton != null) _boton.onClick.RemoveListener(Alternar);
            if (_escape != null) { _escape.performed -= OnEscape; _escape.Dispose(); }
        }
        private void OnValidate()
        {
            if (dosis == null || dosis.Length != 10) System.Array.Resize(ref dosis, 10);
        }
        private void OnEscape(UnityEngine.InputSystem.InputAction.CallbackContext contexto)
        {
            if (EstaAbierto) Cerrar();
        }
        public float ObtenerDosisActual() => dosis[(Mathf.Max(1, _barras.sol) - 1) % 10];
        public void Alternar() { if (EstaAbierto) Cerrar(); else Abrir(); }
        public void Abrir()
        {
            if (!PuedeAbrir) return;
            _bloqueoPropio = true;
            _vinetas.BloquearDuranteFlujo(true);
            _raiz.SetActive(true);
            _raiz.transform.SetAsLastSibling();
            Canvas.ForceUpdateCanvases();
            float margen = margenPantalla / Mathf.Max(0.01f, _canvas.scaleFactor);
            var padre = (RectTransform)_cuadro.parent;
            _cuadro.sizeDelta = new Vector2(Mathf.Min(tamanoCuadro.x, padre.rect.width - margen * 2f),
                Mathf.Min(tamanoCuadro.y, padre.rect.height - margen * 2f));
            _cuadro.anchoredPosition = Vector2.zero;
            ActualizarTexto();
        }
        public void Cerrar()
        {
            if (_raiz != null) _raiz.SetActive(false);
            if (_bloqueoPropio && _vinetas != null) _vinetas.BloquearDuranteFlujo(false);
            _bloqueoPropio = false;
        }
        private void ActualizarTexto()
        {
            if (!EstaAbierto) return;
            int indice = (Mathf.Max(1, _barras.sol) - 1) % 10;
            float valor = ObtenerDosisActual();
            // float no conserva los decimales escritos. Las etiquetas sin editar quedan exactas.
            string original = TextosOriginales[indice];
            string numero = valor == float.Parse(original, CultureInfo.InvariantCulture) ? original :
                valor.ToString("R", CultureInfo.InvariantCulture);
            _texto.text = "Surface radiation:\n" + numero + " Gy\nIf greater than 12: Solar Alert";
            _texto.enableAutoSizing = false;
            _texto.maxVisibleCharacters = int.MaxValue;
            if (_textoVineta != null) _texto.lineSpacing = _textoVineta.lineSpacing;
        }
        public bool EsPuntoBoton(Vector2 punto, Camera camara)
        {
            return _boton != null && _boton.gameObject.activeInHierarchy &&
                RectTransformUtility.RectangleContainsScreenPoint(_imagenBoton.rectTransform, punto, camara) &&
                _imagenBoton.IsRaycastLocationValid(punto, camara);
        }
        public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData datos)
        {
            if (EstaAbierto || PuedeAbrir) _borde.SetActive(true);
        }
        public void OnPointerExit(UnityEngine.EventSystems.PointerEventData datos) { _borde.SetActive(false); }
    }

    // Puente del fondo modal. Se crea en Awake, no necesita archivo ni referencias manuales.
    internal sealed class FondoRadLab : MonoBehaviour, ICanvasRaycastFilter, UnityEngine.EventSystems.IPointerClickHandler
    {
        private RadLabUI _radLab;
        private RectTransform _cuadro;
        public void Inicializar(RadLabUI radLab, RectTransform cuadro) { _radLab = radLab; _cuadro = cuadro; }
        public bool IsRaycastLocationValid(Vector2 punto, Camera camara) => _radLab == null || !_radLab.EsPuntoBoton(punto, camara);
        public void OnPointerClick(UnityEngine.EventSystems.PointerEventData datos)
        {
            if (datos.button == UnityEngine.EventSystems.PointerEventData.InputButton.Left &&
                !RectTransformUtility.RectangleContainsScreenPoint(_cuadro, datos.position, datos.pressEventCamera))
                _radLab.Cerrar();
        }
    }
}
