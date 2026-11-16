using System.Collections;
using UnityEngine;

namespace RoverMars.UI
{
    // Controlador activo en TutorialCanvas; la raíz visual se guarda inactiva.
    public sealed class DidYouKnowUI : MonoBehaviour
    {
        [Header("Primer riego de cada planta")]
        [SerializeField] private Planta _lechuga;
        [SerializeField] private Planta _papa;
        [SerializeField] private GameObject _invernaderoUI;
        [SerializeField] private TutorialFlujo _tutorial;
        [SerializeField] private VinetaManager _vinetas;
        [SerializeField] private MapaOverlay _mapa;
        [SerializeField] private RadLabUI _radLab;
        private UnityEngine.InputSystem.InputAction _escape;

        [Header("Cuadro reutilizable")]
        [SerializeField] private GameObject _raiz;
        [SerializeField] private RectTransform _cuadro;
        [SerializeField] private TMPro.TMP_Text _titulo;
        [SerializeField] private TMPro.TMP_Text _texto;
        [SerializeField] private TMPro.TMP_Text _textoVineta;
        [SerializeField] private UnityEngine.UI.Button _botonX;
        [SerializeField, Min(30f)] private float margenPantalla = 40f;
        [SerializeField] private float anchoCuadro = 1100f;
        [SerializeField, Min(0f)] private float retraso = 1f;

        [Header("Mensajes (texto completo, sin escritura progresiva)")]
        [TextArea(4, 10)]
        [SerializeField] private string mensaje1 = "Growing Food in Space: Astronauts can grow leafy vegetables like lettuce aboard the International Space Station! Potatoes are also being studied because they are rich in calories and could help astronauts grow food on the Moon and Mars.";
        [TextArea(4, 10)]
        [SerializeField] private string mensaje2 = "Can Plants Grow on Mars? Not directly in Martian soil! Mars' natural soil, called regolith, contains toxic salts. Plants would need special growing beds with clean, treated soil inside a space base.";
        [SerializeField] private bool mensajesDidYouKnowMostrados;

        private bool _regadaLechuga, _regadaPapa, _bloqueoPropio;
        private int _indice;
        private Coroutine _espera;
        private Canvas _canvas;
        public bool EstaAbierto => _raiz != null && _raiz.activeSelf;
        public bool MensajesDidYouKnowMostrados => mensajesDidYouKnowMostrados;

        private void Awake()
        {
            _canvas = _cuadro.GetComponentInParent<Canvas>();
            _botonX.onClick.AddListener(CerrarMensaje);
            _raiz.SetActive(false);
            _escape = new UnityEngine.InputSystem.InputAction("CerrarDidYouKnow", UnityEngine.InputSystem.InputActionType.Button, "<Keyboard>/escape");
            _escape.performed += OnEscape;
        }
        private void OnEscape(UnityEngine.InputSystem.InputAction.CallbackContext contexto)
        {
            if (EstaAbierto) CerrarMensaje();
        }
        private void OnEnable()
        {
            _escape.Enable();
            _lechuga.OnRegada += RegistrarLechuga;
            _papa.OnRegada += RegistrarPapa;
            if (_regadaLechuga && _regadaPapa) MostrarSecuencia();
        }
        private void OnDisable()
        {
            _escape?.Disable();
            if (_lechuga != null) _lechuga.OnRegada -= RegistrarLechuga;
            if (_papa != null) _papa.OnRegada -= RegistrarPapa;
            if (_espera != null) StopCoroutine(_espera);
            _espera = null;
            if (_raiz != null) _raiz.SetActive(false);
            LiberarBloqueo();
        }
        private void OnDestroy()
        {
            if (_botonX != null) _botonX.onClick.RemoveListener(CerrarMensaje);
            if (_escape != null) { _escape.performed -= OnEscape; _escape.Dispose(); }
        }
        private void RegistrarLechuga() { _regadaLechuga = true; MostrarSecuencia(); }
        private void RegistrarPapa() { _regadaPapa = true; MostrarSecuencia(); }

        public void MostrarSecuencia()
        {
            if (!_regadaLechuga || !_regadaPapa || mensajesDidYouKnowMostrados || _espera != null || !isActiveAndEnabled) return;
            _espera = StartCoroutine(EsperarYMostrar());
        }
        private IEnumerator EsperarYMostrar()
        {
            yield return new WaitForSeconds(retraso);
            // El aviso queda pendiente si se cambia de habitación o se abre otra interfaz.
            while (!_invernaderoUI.activeInHierarchy || _tutorial.TutorialActivo ||
                _vinetas.VinetaAbierta || _mapa.EstaAbierto || (_radLab != null && _radLab.EstaAbierto))
                yield return null;
            mensajesDidYouKnowMostrados = true;
            _indice = 0;
            _bloqueoPropio = true;
            // Reutiliza el bloqueo existente: no cambia viñetas, herramientas ni plantas.
            // Tampoco deshabilita BarrasEstado ni los estados de las plantas.
            _vinetas.BloquearDuranteFlujo(true);
            _raiz.SetActive(true);
            _raiz.transform.SetAsLastSibling();
            MostrarMensaje();
            _espera = null;
        }
        private void MostrarMensaje()
        {
            _titulo.text = "Did you know?";
            _texto.text = _indice == 0 ? mensaje1 : mensaje2;
            _texto.enableAutoSizing = false;
            _texto.maxVisibleCharacters = int.MaxValue;
            _texto.textWrappingMode = TMPro.TextWrappingModes.Normal;
            if (_textoVineta != null) _texto.lineSpacing = _textoVineta.lineSpacing;
            Canvas.ForceUpdateCanvases();
            var padre = (RectTransform)_cuadro.parent;
            float margen = Mathf.Max(30f, margenPantalla) / Mathf.Max(0.01f, _canvas.scaleFactor);
            float ancho = Mathf.Min(anchoCuadro, padre.rect.width - margen * 2f);
            float alto = _texto.GetPreferredValues(_texto.text, ancho - 80f, Mathf.Infinity).y + 160f;
            _cuadro.sizeDelta = new Vector2(ancho, Mathf.Min(alto, padre.rect.height - margen * 2f));
            _cuadro.anchoredPosition = Vector2.zero;
            _texto.ForceMeshUpdate();
        }
        public void CerrarMensaje()
        {
            if (!EstaAbierto) return;
            if (_indice == 0) { _indice = 1; MostrarMensaje(); return; }
            _raiz.SetActive(false);
            LiberarBloqueo();
        }
        private void LiberarBloqueo()
        {
            if (!_bloqueoPropio) return;
            if (_vinetas != null) _vinetas.BloquearDuranteFlujo(false);
            _bloqueoPropio = false;
        }
    }
}
