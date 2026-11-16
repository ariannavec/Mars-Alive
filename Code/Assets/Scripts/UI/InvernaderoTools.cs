using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RoverMars.UI
{
    public sealed class InvernaderoTools : MonoBehaviour,
        UnityEngine.EventSystems.IPointerClickHandler, ICanvasRaycastFilter
    {
        private enum Herramienta { Ninguna, Regadera, Pala }

        [Header("Sprites reemplazables (vacíos = PixelArt provisional)")]
        [SerializeField] private Sprite _regaderaSprite;
        [SerializeField] private Sprite _palaSprite;
        [SerializeField] private Sprite _marcoNormalSprite;
        [SerializeField] private Sprite _marcoSeleccionadoSprite;

        [Header("Interfaz")]
        [SerializeField] private UnityEngine.UI.Image _regaderaIcono;
        [SerializeField] private UnityEngine.UI.Image _palaIcono;
        [SerializeField] private UnityEngine.UI.Image _regaderaMarco;
        [SerializeField] private UnityEngine.UI.Image _palaMarco;
        [SerializeField] private RectTransform _palaBoton;
        [SerializeField] private RectTransform _columna;
        [SerializeField] private RectTransform _efectos;
        [SerializeField] private UnityEngine.UI.Image _cursor;
        [SerializeField] private TMPro.TMP_Text _aviso;
        [SerializeField] private UnityEngine.UI.RectMask2D[] _hudRecortes;
        [SerializeField] private MapaOverlay _mapa;
        [SerializeField] private VinetaManager _vinetas;
        [Header("Plantas independientes")]
        [SerializeField] private Planta[] _plantas;
        private Planta _plantaObjetivo;
        public bool hayPlantaMadura
        {
            get
            {
                if (_plantas != null)
                    foreach (Planta planta in _plantas)
                        if (planta != null && planta.EstadoActual == Planta.Estado.Madura) return true;
                return false;
            }
        }

        [Header("Animación por pasos")]
        [SerializeField, Min(1)] private int _fps = 12;
        [SerializeField, Min(0.1f)] private float _duracionAviso = 1.5f;
        [SerializeField, Min(1)] private int _cantidadGotas = 10;
        [SerializeField, Min(1)] private int _cantidadTierra = 8;

        private RectTransform _area;
        private Vector2 _palaPosicion;
        private Vector2 _puntero;
        private bool _esRaton;
        private bool _foco = true;
        private bool _ocupado;
        private bool _inicializado;
        private bool _cursorPropio;
        private bool _cursorOriginalVisible;
        private Herramienta _seleccion;
        private Sprite _regaderaActual, _palaActual, _normalActual, _seleccionadoActual;
        private Sprite _gotaSprite, _tierraSprite;
        private UnityEngine.InputSystem.InputAction _posicionAction;
        private Coroutine _seguimiento, _uso, _advertencia;
        private readonly List<Sprite> _generados = new List<Sprite>();
        private readonly List<GameObject> _temporales = new List<GameObject>();

        public bool haRegado { get; private set; }
        public event Action<Vector2> OnRegado;
        public event Action<Vector2> OnExcavar;
        public bool HerramientaSeleccionada => _seleccion != Herramienta.Ninguna;

        private bool Permitido => isActiveAndEnabled && gameObject.activeInHierarchy && _foco &&
            (_mapa == null || !_mapa.EstaAbierto) && (_vinetas == null || !_vinetas.VinetaAbierta);
        private float Paso => 1f / Mathf.Max(1, _fps);

        private void Awake()
        {
            _area = GetComponent<RectTransform>();
            if (_area == null || _regaderaIcono == null || _palaIcono == null || _cursor == null ||
                _palaBoton == null || _efectos == null || _aviso == null)
            {
                Debug.LogError("Asigna las referencias de InvernaderoTools en AreaJuego.", this);
                enabled = false;
                return;
            }
            _palaPosicion = _palaBoton.anchoredPosition;
            _regaderaActual = _regaderaSprite != null ? _regaderaSprite : Guardar(PixelArt.CrearRegadera());
            _palaActual = _palaSprite != null ? _palaSprite : Guardar(PixelArt.CrearPala());
            _normalActual = _marcoNormalSprite != null ? _marcoNormalSprite : Guardar(PixelArt.CrearMarco(false));
            _seleccionadoActual = _marcoSeleccionadoSprite != null ? _marcoSeleccionadoSprite : Guardar(PixelArt.CrearMarco(true));
            _gotaSprite = Guardar(PixelArt.CrearParticula(new Color32(50, 166, 255, 255)));
            _tierraSprite = Guardar(PixelArt.CrearParticula(new Color32(131, 80, 37, 255)));
            _regaderaIcono.sprite = _regaderaActual;
            _palaIcono.sprite = _palaActual;
            _cursor.raycastTarget = false;
            _aviso.gameObject.SetActive(false);
            _inicializado = true;
            if (_plantas != null)
                foreach (Planta planta in _plantas)
                {
                    if (planta == null) continue;
                    planta.OnPulsada += UsarEnPlanta;
                    planta.OnMadura += ActualizarDisponibilidadPala;
                    planta.OnCosechada += ActualizarDisponibilidadPala;
                }
            Refrescar();
        }

        private void ActualizarDisponibilidadPala()
        {
            // Se mantiene la suscripción al salir para actualizar la madurez fuera del cuarto.
            if (_seleccion == Herramienta.Pala && !hayPlantaMadura) Elegir(Herramienta.Ninguna);
            Refrescar();
        }

        private void UsarEnPlanta(Planta planta)
        {
            if (!Permitido || _ocupado || _seleccion == Herramienta.Ninguna || !planta.PuedeInteractuar) return;
            if (!IsRaycastLocationValid(_puntero, null)) return;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_area, _puntero, null, out Vector2 posicion))
            {
                _plantaObjetivo = planta;
                _uso = StartCoroutine(EjecutarUso(Redondear(posicion)));
            }
        }

        private Sprite Guardar(Sprite sprite)
        {
            _generados.Add(sprite);
            return sprite;
        }

        private void OnEnable()
        {
            if (_posicionAction == null)
            {
                _posicionAction = new UnityEngine.InputSystem.InputAction("PunteroInvernadero",
                    UnityEngine.InputSystem.InputActionType.PassThrough, "<Pointer>/position");
                _posicionAction.performed += OnPosicion;
            }
            _posicionAction.Enable();
            _puntero = _posicionAction.ReadValue<Vector2>();
            _esRaton = UnityEngine.InputSystem.Mouse.current != null;
            if (_mapa != null) _mapa.OnMapaAbierto += Deseleccionar;
            _seguimiento = StartCoroutine(SeguirCursor());
        }

        private void OnDisable()
        {
            if (_mapa != null) _mapa.OnMapaAbierto -= Deseleccionar;
            Deseleccionar();
            if (_seguimiento != null) StopCoroutine(_seguimiento);
            _seguimiento = null;
            _posicionAction?.Disable();
        }

        private void OnDestroy()
        {
            if (_plantas != null)
                foreach (Planta planta in _plantas)
                {
                    if (planta == null) continue;
                    planta.OnPulsada -= UsarEnPlanta;
                    planta.OnMadura -= ActualizarDisponibilidadPala;
                    planta.OnCosechada -= ActualizarDisponibilidadPala;
                }
            if (_posicionAction != null)
            {
                _posicionAction.performed -= OnPosicion;
                _posicionAction.Dispose();
            }
            foreach (Sprite sprite in _generados) PixelArt.Liberar(sprite);
        }

        private void OnApplicationFocus(bool tieneFoco)
        {
            _foco = tieneFoco;
            if (!tieneFoco) Deseleccionar();
        }

        private void OnPosicion(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            _puntero = context.ReadValue<Vector2>();
            _esRaton = context.control.device is UnityEngine.InputSystem.Mouse ||
                context.control.device is UnityEngine.InputSystem.Pen;
        }

        public void SeleccionarRegadera()
        {
            if (!Permitido || _ocupado) return;
            Elegir(_seleccion == Herramienta.Regadera ? Herramienta.Ninguna : Herramienta.Regadera);
        }

        public void SeleccionarPala()
        {
            if (!Permitido || _ocupado) return;
            if (!hayPlantaMadura)
            {
                if (_advertencia != null) StopCoroutine(_advertencia);
                _palaBoton.anchoredPosition = _palaPosicion;
                _advertencia = StartCoroutine(AvisarAgua());
                return;
            }
            Elegir(_seleccion == Herramienta.Pala ? Herramienta.Ninguna : Herramienta.Pala);
        }

        private void Elegir(Herramienta herramienta)
        {
            _seleccion = herramienta;
            Refrescar();
            if (herramienta == Herramienta.Ninguna) RestaurarCursor();
        }

        public void Deseleccionar()
        {
            if (!_inicializado) return;
            if (_uso != null) StopCoroutine(_uso);
            if (_advertencia != null) StopCoroutine(_advertencia);
            _uso = _advertencia = null;
            _plantaObjetivo = null;
            _ocupado = false;
            LimpiarEfectos();
            _seleccion = Herramienta.Ninguna;
            if (_palaBoton != null) _palaBoton.anchoredPosition = _palaPosicion;
            if (_aviso != null) _aviso.gameObject.SetActive(false);
            Refrescar();
            RestaurarCursor();
        }

public void OnPointerClick(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (eventData == null || eventData.button != UnityEngine.EventSystems.PointerEventData.InputButton.Left ||
                !Permitido || _ocupado || _seleccion == Herramienta.Ninguna) return;
            if (!IsRaycastLocationValid(eventData.position, eventData.pressEventCamera)) return;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_area, eventData.position,
                eventData.pressEventCamera, out Vector2 posicion))
            {
                // Los eventos entregan coordenadas locales de AreaJuego, no coordenadas de mundo.
                posicion = Redondear(posicion);
                _plantaObjetivo = null;
                _uso = StartCoroutine(EjecutarUso(posicion));
            }
        }

        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera camera)
        {
            if (_columna != null && RectTransformUtility.RectangleContainsScreenPoint(_columna, screenPoint, camera))
                return false;
            if (_hudRecortes != null)
                foreach (UnityEngine.UI.RectMask2D mask in _hudRecortes)
                {
                    if (mask == null) continue;
                    if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(mask.rectTransform, screenPoint,
                        camera, out Vector2 local)) continue;
                    Rect r = mask.rectTransform.rect;
                    Vector4 p = mask.padding;
                    if (Rect.MinMaxRect(r.xMin + p.x, r.yMin + p.y, r.xMax - p.z, r.yMax - p.w).Contains(local))
                        return false;
                }
            return true;
        }

        private void Refrescar()
        {
            if (_regaderaIcono != null) _regaderaIcono.rectTransform.localScale = Vector3.one * (_seleccion == Herramienta.Regadera ? 5 : 4);
            if (_palaIcono != null)
            {
                _palaIcono.rectTransform.localScale = Vector3.one * (_seleccion == Herramienta.Pala ? 5 : 4);
                _palaIcono.color = hayPlantaMadura ? Color.white : new Color(1, 1, 1, 0.35f);
            }
            if (_regaderaMarco != null) _regaderaMarco.sprite = _seleccion == Herramienta.Regadera ? _seleccionadoActual : _normalActual;
            if (_palaMarco != null) _palaMarco.sprite = _seleccion == Herramienta.Pala ? _seleccionadoActual : _normalActual;
        }

        private IEnumerator SeguirCursor()
        {
            while (true)
            {
                if (HerramientaSeleccionada && !Permitido) Deseleccionar();
                bool mostrar = HerramientaSeleccionada && Permitido && _esRaton &&
                    RectTransformUtility.RectangleContainsScreenPoint(_area, _puntero, null) &&
                    IsRaycastLocationValid(_puntero, null);
                if (mostrar)
                {
                    if (!_cursorPropio)
                    {
                        _cursorOriginalVisible = Cursor.visible;
                        _cursorPropio = true;
                        Cursor.visible = false;
                    }
                    _cursor.gameObject.SetActive(!_ocupado);
                    _cursor.sprite = _seleccion == Herramienta.Regadera ? _regaderaActual : _palaActual;
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_area, _puntero, null, out Vector2 local))
                        _cursor.rectTransform.anchoredPosition = Redondear(local);
                }
                else RestaurarCursor();
                yield return new WaitForSecondsRealtime(Paso);
            }
        }

        private void RestaurarCursor()
        {
            if (_cursor != null) _cursor.gameObject.SetActive(false);
            if (_cursorPropio) Cursor.visible = _cursorOriginalVisible;
            _cursorPropio = false;
        }

        private IEnumerator AvisarAgua()
        {
            _aviso.text = "WATER FIRST!";
            _aviso.gameObject.SetActive(true);
            float inicio = Time.unscaledTime;
            int paso = 0;
            while (Time.unscaledTime - inicio < _duracionAviso)
            {
                _palaBoton.anchoredPosition = _palaPosicion + Vector2.right * (paso < 6 ? (paso % 2 == 0 ? -4 : 4) : 0);
                yield return new WaitForSecondsRealtime(Paso);
                paso++;
            }
            _palaBoton.anchoredPosition = _palaPosicion;
            _aviso.gameObject.SetActive(false);
            _advertencia = null;
        }

        private IEnumerator EjecutarUso(Vector2 posicion)
        {
            Herramienta herramienta = _seleccion;
            _ocupado = true;
            bool regadera = herramienta == Herramienta.Regadera;
            UnityEngine.UI.Image herramientaVisual = CrearImagen("UsoHerramienta",
                regadera ? _regaderaActual : _palaActual, posicion, new Vector2(64, 64));
            int cantidad = regadera ? _cantidadGotas : _cantidadTierra;
            RectTransform[] particulas = new RectTransform[cantidad];
            Vector2[] velocidades = new Vector2[cantidad];
            float[] angulos = { 0f, -15f, -30f, 0f };
            int[] alturas = { 0, -8, -16, -8, 0 };
            const int pasosParticulas = 12;
            WaitForSecondsRealtime espera = new WaitForSecondsRealtime(Paso);
            for (int paso = 0; paso < pasosParticulas + 2; paso++)
            {
                if (!Permitido)
                {
                    _uso = null;
                    Deseleccionar();
                    yield break;
                }
                if (regadera)
                    herramientaVisual.rectTransform.localRotation = Quaternion.Euler(0, 0, angulos[Mathf.Min(paso, angulos.Length - 1)]);
                else
                    herramientaVisual.rectTransform.anchoredPosition = posicion + Vector2.up * alturas[Mathf.Min(paso, alturas.Length - 1)];
                if (paso == 2)
                    for (int i = 0; i < cantidad; i++)
                    {
                        Vector2 origen = posicion + (regadera ? new Vector2(28, -12) : new Vector2(0, -24));
                        origen += new Vector2(UnityEngine.Random.Range(-8, 9), UnityEngine.Random.Range(-4, 5));
                        particulas[i] = CrearImagen(regadera ? "GotaPixel" : "TierraPixel",
                            regadera ? _gotaSprite : _tierraSprite, origen, new Vector2(4, 4)).rectTransform;
                        velocidades[i] = regadera ? new Vector2(0, -UnityEngine.Random.Range(2, 5)) :
                            new Vector2(UnityEngine.Random.Range(-4, 5), UnityEngine.Random.Range(5, 10));
                    }
                if (paso >= 2)
                    for (int i = 0; i < cantidad; i++)
                    {
                        velocidades[i].y -= regadera ? 1 : 2;
                        particulas[i].anchoredPosition = Redondear(particulas[i].anchoredPosition + velocidades[i]);
                    }
                yield return espera;
            }
            LimpiarEfectos();
            _ocupado = false;
            _uso = null;
            Planta objetivo = _plantaObjetivo;
            _plantaObjetivo = null;
            if (regadera) haRegado = true;
            // Cancelar la animación no cambia el cultivo ni concede salud.
            // El objetivo valida su estado: regar madura o cavar brote no hace nada.
            if (objetivo != null)
            {
                if (regadera) objetivo.Regar();
                else objetivo.Cosechar();
            }
            Refrescar();
            // Un único evento al completar la acción; cancelar no concede un riego nuevo.
            if (regadera) OnRegado?.Invoke(posicion);
            else OnExcavar?.Invoke(posicion);
        }

        private UnityEngine.UI.Image CrearImagen(string nombre, Sprite sprite, Vector2 posicion, Vector2 tamano)
        {
            GameObject obj = new GameObject(nombre, typeof(RectTransform), typeof(UnityEngine.UI.Image));
            obj.layer = _efectos.gameObject.layer;
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.SetParent(_efectos, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = tamano;
            rect.anchoredPosition = Redondear(posicion);
            UnityEngine.UI.Image image = obj.GetComponent<UnityEngine.UI.Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            _temporales.Add(obj);
            return image;
        }

        private static Vector2 Redondear(Vector2 valor)
        {
            return new Vector2(Mathf.Round(valor.x), Mathf.Round(valor.y));
        }

        private void LimpiarEfectos()
        {
            foreach (GameObject obj in _temporales)
                if (obj != null) { obj.SetActive(false); Destroy(obj); }
            _temporales.Clear();
        }
    }
}
