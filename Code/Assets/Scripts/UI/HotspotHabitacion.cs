using UnityEngine;

namespace RoverMars.UI
{
    public enum RoomId
    {
        Laboratorio,
        Dormitorio,
        Invernadero,
        Hangar,
        CentroDeControl,
        CuartoDeEnergia,
        Bunker
    }

    public sealed class HotspotHabitacion : MonoBehaviour,
        UnityEngine.EventSystems.IPointerEnterHandler,
        UnityEngine.EventSystems.IPointerExitHandler,
        UnityEngine.EventSystems.IPointerClickHandler
    {
        [Header("Habitación y referencias")]
        [SerializeField] private RoomId _roomId;
        [SerializeField] private GameObject _borde;
        [SerializeField] private RoomManager _roomManager;
        [SerializeField] private MapaOverlay _mapaOverlay;
        [Header("Bloqueo de la habitación")]
        [SerializeField] private bool bloqueada = true;
        [SerializeField] private GameObject _oscuro;
        [SerializeField] private UnityEngine.UI.Image _candado;
        [SerializeField] private Sprite spriteCandado;
        private Sprite _candadoGenerado;
        private Sprite _interiorDormitorio;
        public bool Bloqueada => bloqueada;
        public void Desbloquear() { bloqueada = false; ActualizarApariencia(); }
        public void Bloquear() { bloqueada = true; ActualizarApariencia(); }
        private void OnValidate() { ActualizarApariencia(); }
        private void ActualizarApariencia()
        {
            if (bloqueada) OcultarBorde();
            if (_oscuro != null)
            {
                _oscuro.SetActive(bloqueada);
                if (Application.isPlaying && _roomId == RoomId.Dormitorio)
                {
                    if (_interiorDormitorio == null) _interiorDormitorio = PixelArt.CrearInteriorDormitorio();
                    _oscuro.GetComponent<UnityEngine.UI.Image>().sprite = _interiorDormitorio;
                }
            }
            if (_candado == null) return;
            if (spriteCandado != null) _candado.sprite = spriteCandado;
            else if (Application.isPlaying)
            {
                if (_candadoGenerado == null) _candadoGenerado = PixelArt.CrearCandado();
                _candado.sprite = _candadoGenerado;
            }
            _candado.raycastTarget = false;
            _candado.color = Color.white; // El dibujo ya contiene su paleta gris exacta.
            _candado.rectTransform.sizeDelta = new Vector2(64f, 64f);
            _candado.gameObject.SetActive(bloqueada);
        }
        private void OnDestroy()
        {
            PixelArt.Liberar(_candadoGenerado);
            PixelArt.Liberar(_interiorDormitorio);
        }

        private void OnEnable()
        {
            OcultarBorde();
            ActualizarApariencia();
        }

        private void OnDisable()
        {
            // Evita conservar un hover al cerrar o volver a abrir el mapa.
            OcultarBorde();
        }

        public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (!bloqueada && _borde != null && _mapaOverlay != null && _mapaOverlay.PuedeInteractuar)
                _borde.SetActive(true);
        }

        public void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData)
        {
            OcultarBorde();
        }

        public void OnPointerClick(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (eventData == null || eventData.button != UnityEngine.EventSystems.PointerEventData.InputButton.Left)
                return;
            if (bloqueada || _roomManager == null || _mapaOverlay == null || !_mapaOverlay.PuedeInteractuar)
                return;

            // Se reutilizan las funciones existentes, sin modificar RoomManager.
            switch (_roomId)
            {
                case RoomId.Laboratorio: _roomManager.MostrarLaboratorio(); break;
                case RoomId.Dormitorio: _roomManager.MostrarDormitorio(); break;
                case RoomId.Invernadero: _roomManager.MostrarInvernadero(); break;
                case RoomId.Hangar: _roomManager.MostrarHangar(); break;
                case RoomId.CentroDeControl: _roomManager.MostrarCentroDeControl(); break;
                case RoomId.CuartoDeEnergia: _roomManager.MostrarCuartoDeEnergia(); break;
                case RoomId.Bunker: _roomManager.MostrarBunker(); break;
                default: return;
            }
            _mapaOverlay.CerrarMapa();
        }

        public void OcultarBorde()
        {
            if (_borde != null)
                _borde.SetActive(false);
        }
    }
}
