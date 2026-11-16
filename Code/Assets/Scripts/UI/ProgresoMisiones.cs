using UnityEngine;

namespace RoverMars.UI
{
    public sealed class ProgresoMisiones : MonoBehaviour
    {
        private const int TotalMisiones = 9;
        [Header("Nueve rellenos de izquierda a derecha")]
        [SerializeField] private UnityEngine.UI.Image[] _secciones = new UnityEngine.UI.Image[TotalMisiones];
        [SerializeField] private Color _verde = new Color32(155, 219, 99, 255);
        [SerializeField] private Color _vacio = new Color32(35, 55, 29, 255);
        public int Progreso { get; private set; }
        public event System.Action<int> OnProgresoCambiado;
        public event System.Action OnJuegoCompletado;

        private void Awake()
        {
            Progreso = 0;
            ActualizarBarra();
        }
        public void CompletarMision()
        {
            // No hay llamadas automáticas desde herramientas, mapa ni Help.
            if (Progreso >= TotalMisiones) return;
            Progreso++;
            ActualizarBarra();
            OnProgresoCambiado?.Invoke(Progreso);
            if (Progreso == TotalMisiones) OnJuegoCompletado?.Invoke();
        }
        private void ActualizarBarra()
        {
            for (int i = 0; i < _secciones.Length; i++)
                if (_secciones[i] != null) _secciones[i].color = i < Progreso ? _verde : _vacio;
        }
    }
}
