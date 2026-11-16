using UnityEngine;

namespace RoverMars.UI
{
    public sealed class RelojSol : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private BarrasEstado _barrasEstado;
        [SerializeField] private RectTransform _manecilla;
        private void Awake()
        {
            // La base del dibujo coincide con el centro del reloj.
            _manecilla.anchorMin = _manecilla.anchorMax = new Vector2(0.5f, 0.5f);
            _manecilla.pivot = new Vector2(0.5f, 0f);
            _manecilla.anchoredPosition = Vector2.zero;
            _manecilla.localRotation = Quaternion.identity;
        }
        private void LateUpdate()
        {
            if (_barrasEstado == null || _manecilla == null) return;
            // Ángulo negativo: horario; al reiniciar el Sol vuelve a cero.
            _manecilla.localRotation = Quaternion.Euler(0f, 0f, -360f * _barrasEstado.ProgresoSol);
        }
    }
}
