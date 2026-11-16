using UnityEngine;

namespace RoverMars.UI
{
    public sealed class RoomManager : MonoBehaviour
    {
        [Header("Única imagen de fondo")]
        [SerializeField] private UnityEngine.UI.Image _fondo;

        [Header("Fondos de las siete habitaciones")]
        [SerializeField] private Sprite _laboratorio;
        [SerializeField] private Sprite _dormitorio;
        [SerializeField] private Sprite _invernadero;
        [SerializeField] private Sprite _hangar;
        [SerializeField] private Sprite _centroDeControl;
        [SerializeField] private Sprite _cuartoDeEnergia;
        [SerializeField] private Sprite _bunker;

        [Header("Herramientas exclusivas del Invernadero")]
        [SerializeField] private GameObject _invernaderoUI;
        [SerializeField] private InvernaderoTools _invernaderoTools;

        [Header("Interfaz exclusiva del Centro de control")]
        [SerializeField] private GameObject _centroControlUI;

        private void Start()
        {
            // Es la única habitación que se selecciona automáticamente.
            MostrarCentroDeControl();
        }

        // Estas funciones quedan disponibles para el futuro mapa.
        public void MostrarLaboratorio() { CambiarFondo(_laboratorio); if (_centroControlUI != null) _centroControlUI.SetActive(false); }
        public void MostrarDormitorio() { CambiarFondo(_dormitorio); if (_centroControlUI != null) _centroControlUI.SetActive(false); }
        public void MostrarInvernadero() { CambiarFondo(_invernadero); if (_centroControlUI != null) _centroControlUI.SetActive(false); }
        public void MostrarHangar() { CambiarFondo(_hangar); if (_centroControlUI != null) _centroControlUI.SetActive(false); }
        public void MostrarCentroDeControl() { CambiarFondo(_centroDeControl); if (_centroControlUI != null) _centroControlUI.SetActive(true); }
        public void MostrarCuartoDeEnergia() { CambiarFondo(_cuartoDeEnergia); if (_centroControlUI != null) _centroControlUI.SetActive(false); }
        public void MostrarBunker() { CambiarFondo(_bunker); if (_centroControlUI != null) _centroControlUI.SetActive(false); }

        private void CambiarFondo(Sprite sprite)
        {
            if (_fondo == null || sprite == null)
            {
                Debug.LogError("Asigna Fondo y el sprite de la habitación en RoomManager.", this);
                return;
            }
            // Solo cambian el fondo y las herramientas de esta habitación; el HUD permanece intacto.
            if (_invernaderoTools != null) _invernaderoTools.Deseleccionar();
            _fondo.sprite = sprite;
            if (_invernaderoUI != null) _invernaderoUI.SetActive(sprite == _invernadero);
        }
    }
}
