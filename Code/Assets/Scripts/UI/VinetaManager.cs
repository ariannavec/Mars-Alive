using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RoverMars.UI
{
    // El tutorial espera un fotograma para que RoomManager seleccione primero
    // Centro de control, sin cambiar sus funciones ni el orden del proyecto.
    public sealed class VinetaManager : MonoBehaviour
    {
        [System.Serializable]
        private class Vineta
        {
            public string nombre;
            public Vector2 posicion;
            public Vector2 tamano = new Vector2(850f, 300f);
            [TextArea(3, 6)] public List<string> dialogos = new List<string>();
            public RectTransform punta;
            public RectTransform objetivo;
        }

        [Header("Cuadro reutilizable")]
        [SerializeField] private GameObject _panel;
        [SerializeField] private TMPro.TMP_Text _texto;
        [SerializeField] private UnityEngine.UI.Button _siguiente;
        [SerializeField] private UnityEngine.UI.Image _flecha;
        [SerializeField] private TMPro.TMP_Text _okTexto;
        [SerializeField] private MapaOverlay _mapaOverlay;

        [Header("Ritmo del texto y de la flecha")]
        [SerializeField, Min(0.001f)] private float _segundosPorLetra = 0.03f;
        [SerializeField, Min(0.05f)] private float _intervaloParpadeo = 0.45f;

        [Header("Viñetas en orden: Health, Water, Energy, Progress, Map")]
        [SerializeField] private List<Vineta> _vinetas = new List<Vineta>
        {
            new Vineta { nombre = "Health", dialogos = new List<string> { "The heart shows your health. Keep it full to stay healthy!" } },
            new Vineta { nombre = "Water", dialogos = new List<string> { "The water drop shows how much water you have left. Stay hydrated!" } },
            new Vineta { nombre = "Energy", dialogos = new List<string> { "The lightning bolt shows your energy. Keep it up to continue your adventure!" } },
            new Vineta { nombre = "Progress", dialogos = new List<string> { "Check your progress and see how far you have come!" } },
            new Vineta { nombre = "Map", dialogos = new List<string> { "Open the map to see where you are and explore new places!" } }
        };

        private RectTransform _panelRect;
        private Coroutine _escritura;
        private UnityEngine.InputSystem.InputAction _avanzar;
        private bool _avanceSolicitado;
        private bool _escribiendo;
        private int _indiceVineta;
        private int _indiceDialogo;

        private bool _vinetaAbierta;
        private bool _bloqueoFlujo;
        // Los sistemas existentes consultan esta propiedad; no hay que modificarlos.
        public bool VinetaAbierta { get => _vinetaAbierta || _bloqueoFlujo; private set => _vinetaAbierta = value; }
        public bool SecuenciaEnCurso => _vinetaAbierta;
        public void BloquearDuranteFlujo(bool bloquear) => _bloqueoFlujo = bloquear;
        [Header("Márgenes y texto")]
        [SerializeField, Min(30f)] private float margenPantalla = 30f;
        [SerializeField] private float interlineado = 20f;

        private void Awake()
        {
            if (_panel != null)
                _panelRect = _panel.GetComponent<RectTransform>();
            if (_texto != null)
                _texto.maxVisibleCharacters = 0;
        }

        private void OnEnable()
        {
            if (_avanzar == null)
            {
                _avanzar = new UnityEngine.InputSystem.InputAction(
                    "AvanzarTutorial", UnityEngine.InputSystem.InputActionType.Button);
                UnityEngine.InputSystem.InputActionSetupExtensions.AddBinding(_avanzar, "<Keyboard>/space");
                UnityEngine.InputSystem.InputActionSetupExtensions.AddBinding(_avanzar, "<Keyboard>/enter");
                UnityEngine.InputSystem.InputActionSetupExtensions.AddBinding(_avanzar, "<Keyboard>/numpadEnter");
                _avanzar.performed += OnAvanzar;
            }
            _avanzar.Enable();
        }

        private void Start()
        {
            if (!_vinetaAbierta && _panel != null) _panel.SetActive(false);
        }

        private void Update()
        {
            if (_avanceSolicitado)
            {
                _avanceSolicitado = false;
                SiguienteDialogo();
            }
            if (VinetaAbierta && _flecha != null && _flecha.gameObject.activeSelf)
            {
                bool visible = _escribiendo || Mathf.FloorToInt(Time.unscaledTime / _intervaloParpadeo) % 2 == 0;
                _flecha.color = new Color(1f, 1f, 1f, visible ? 1f : 0f);
            }
        }

        private void OnDisable()
        {
            DetenerEscritura();
            _avanzar?.Disable();
            _avanceSolicitado = false;
        }

        private void OnDestroy()
        {
            if (_avanzar != null)
            {
                _avanzar.performed -= OnAvanzar;
                _avanzar.Dispose();
            }
        }

        private void OnAvanzar(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            _avanceSolicitado = true;
        }

        public void IniciarTutorial()
        {
            if (_panelRect == null && _panel != null)
                _panelRect = _panel.GetComponent<RectTransform>();
            if (_panelRect == null || _texto == null || _siguiente == null || _flecha == null || _okTexto == null)
            {
                Debug.LogError("Asigna panel, texto, botón, flecha y OK en VinetaManager.", this);
                return;
            }
            if (_vinetas == null || _vinetas.Count == 0)
                return;
            DetenerEscritura();
            _indiceVineta = 0;
            _indiceDialogo = 0;
            _avanceSolicitado = false;
            VinetaAbierta = true;
            _mapaOverlay?.CerrarMapa();
            _panel.SetActive(true);
            _avanzar?.Enable();
            MostrarDialogo();
        }

        public void SiguienteDialogo()
        {
            if (!_vinetaAbierta)
                return;
            // El primer clic completa el texto; el siguiente avanza.
            if (_escribiendo)
            {
                CompletarTexto();
                return;
            }
            Vineta actual = _vinetas[_indiceVineta];
            if (_indiceDialogo + 1 < actual.dialogos.Count)
            {
                _indiceDialogo++;
                MostrarDialogo();
            }
            else
                CerrarVineta();
        }

        public void CerrarVineta()
        {
            if (!_vinetaAbierta)
                return;
            if (_escribiendo)
            {
                CompletarTexto();
                return;
            }
            // No se permite saltar diálogos intermedios.
            if (_indiceDialogo + 1 < _vinetas[_indiceVineta].dialogos.Count)
                return;
            OcultarPuntas();
            _indiceVineta++;
            if (_indiceVineta >= _vinetas.Count)
            {
                VinetaAbierta = false;
                _panel.SetActive(false);
                _avanzar?.Disable();
                _avanceSolicitado = false;
                return;
            }
            _indiceDialogo = 0;
            MostrarDialogo();
        }

        private void MostrarDialogo()
        {
            Vineta actual = _vinetas[_indiceVineta];
            if (actual.dialogos == null || actual.dialogos.Count == 0)
            {
                Debug.LogError("Cada viñeta necesita al menos un diálogo.", this);
                return;
            }
            OcultarPuntas();
            _texto.enableAutoSizing = false;
            _texto.lineSpacing = interlineado;
            _texto.text = actual.dialogos[_indiceDialogo];
            var padre = (RectTransform)_panelRect.parent;
            float escala = _panelRect.GetComponentInParent<Canvas>().scaleFactor;
            float margen = Mathf.Max(30f, margenPantalla) / Mathf.Max(0.01f, escala);
            float ancho = Mathf.Min(actual.tamano.x, padre.rect.width - 2f * (margen + 32f));
            float alto = _texto.GetPreferredValues(_texto.text, ancho - 76f, Mathf.Infinity).y + 100f;
            alto = Mathf.Min(alto, padre.rect.height - 2f * (margen + 32f));
            _panelRect.sizeDelta = new Vector2(ancho, alto);
            Vector2 posicion = actual.posicion;
            Vector2 destino = posicion;
            if (actual.objetivo != null)
            {
                destino = padre.InverseTransformPoint(actual.objetivo.TransformPoint(actual.objetivo.rect.center));
                // Posición es un desplazamiento respecto al icono si tiene objetivo.
                posicion = destino + (actual.posicion != Vector2.zero ? actual.posicion :
                    new Vector2(0f, destino.y > 0f ? -alto / 2f - 90f : alto / 2f + 90f));
            }
            posicion.x = Mathf.Clamp(posicion.x, padre.rect.xMin + margen + ancho / 2f + 32f, padre.rect.xMax - margen - ancho / 2f - 32f);
            posicion.y = Mathf.Clamp(posicion.y, padre.rect.yMin + margen + alto / 2f + 32f, padre.rect.yMax - margen - alto / 2f - 32f);
            _panelRect.anchoredPosition = posicion;
            if (actual.punta != null)
            {
                actual.punta.gameObject.SetActive(true);
                Vector2 direccion = (destino - posicion).normalized;
                float distancia = Mathf.Min(ancho / 2f / Mathf.Max(0.001f, Mathf.Abs(direccion.x)), alto / 2f / Mathf.Max(0.001f, Mathf.Abs(direccion.y)));
                actual.punta.anchorMin = actual.punta.anchorMax = new Vector2(0.5f, 0.5f);
                actual.punta.anchoredPosition = direccion * (distancia + 14f);
                actual.punta.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg);
            }
            _flecha.gameObject.SetActive(false);
            _flecha.color = Color.white;
            _okTexto.gameObject.SetActive(false);
            _siguiente.interactable = true;
            _texto.maxVisibleCharacters = 0;
            _texto.ForceMeshUpdate();
            DetenerEscritura();
            _escribiendo = true;
            _escritura = StartCoroutine(EscribirTexto());
        }

        private IEnumerator EscribirTexto()
        {
            int total = _texto.textInfo.characterCount;
            WaitForSecondsRealtime espera = new WaitForSecondsRealtime(_segundosPorLetra);
            for (int i = 1; i <= total; i++)
            {
                _texto.maxVisibleCharacters = i;
                yield return espera;
            }
            _escribiendo = false;
            _escritura = null;
            _flecha.gameObject.SetActive(true);
        }

        public void CompletarTexto()
        {
            if (!_vinetaAbierta) return;
            DetenerEscritura();
            _texto.maxVisibleCharacters = int.MaxValue;
            _flecha.gameObject.SetActive(true);
        }

        private void DetenerEscritura()
        {
            if (_escritura != null)
                StopCoroutine(_escritura);
            _escritura = null;
            _escribiendo = false;
        }

        private void OcultarPuntas()
        {
            foreach (Vineta vineta in _vinetas)
                if (vineta.punta != null)
                    vineta.punta.gameObject.SetActive(false);
        }
    }
}
