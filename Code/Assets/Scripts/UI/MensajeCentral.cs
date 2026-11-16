using System.Collections;
using UnityEngine;

namespace RoverMars.UI
{
    public sealed class MensajeCentral : MonoBehaviour, UnityEngine.EventSystems.IPointerClickHandler
    {
        [Header("Referencias del cuadro y fondo bloqueador")]
        [SerializeField] private GameObject _raiz;
        [SerializeField] private RectTransform _cuadro;
        [SerializeField] private TMPro.TMP_Text _texto;
        [SerializeField] private UnityEngine.UI.Button _siguiente;
        [SerializeField] private CanvasGroup _flecha;
        [Header("Presentación")]
        [SerializeField, Min(0.001f)] private float segundosPorLetra = 0.04f;
        [SerializeField, Min(0.05f)] private float intervaloParpadeo = 0.45f;
        [SerializeField, Min(30f)] private float margenPantalla = 30f;
        [SerializeField] private float interlineado = 20f;
        [SerializeField] private float anchoCuadro = 1100f;
        private bool _escribiendo, _continuar;
        public bool EstaAbierto => _raiz != null && _raiz.activeSelf;

        private void Awake()
        {
            // La raíz se guarda inactiva en la escena y se habilita desde Mostrar.
            _siguiente.onClick.AddListener(Avanzar);
        }
        private void OnDestroy()
        {
            if (_siguiente != null) _siguiente.onClick.RemoveListener(Avanzar);
        }
        public IEnumerator Mostrar(string mensaje)
        {
            _continuar = false;
            _escribiendo = true;
            _raiz.SetActive(true);
            _raiz.transform.SetAsLastSibling();
            _texto.enableAutoSizing = false;
            _texto.lineSpacing = interlineado;
            _texto.text = mensaje;
            _texto.maxVisibleCharacters = 0;
            AjustarCuadro();
            _texto.ForceMeshUpdate();
            int total = _texto.textInfo.characterCount;
            _siguiente.interactable = false;
            _flecha.alpha = 0f;
            var espera = new WaitForSecondsRealtime(segundosPorLetra);
            for (int i = 1; i <= total && _escribiendo; i++)
            {
                _texto.maxVisibleCharacters = i;
                yield return espera;
            }
            CompletarTexto();
            while (!_continuar)
            {
                _flecha.alpha = Mathf.FloorToInt(Time.unscaledTime / intervaloParpadeo) % 2 == 0 ? 1f : 0f;
                yield return null;
            }
            _raiz.SetActive(false);
        }
        private void AjustarCuadro()
        {
            // Se mide el texto completo ANTES de escribir: no cambia de tamaño por letra.
            var padre = (RectTransform)_cuadro.parent;
            var canvas = _cuadro.GetComponentInParent<Canvas>();
            float margen = margenPantalla / Mathf.Max(0.01f, canvas.scaleFactor);
            float ancho = Mathf.Min(anchoCuadro, padre.rect.width - margen * 2f);
            float alto = _texto.GetPreferredValues(_texto.text, ancho - 80f, Mathf.Infinity).y + 120f;
            _cuadro.sizeDelta = new Vector2(ancho, Mathf.Min(alto, padre.rect.height - margen * 2f));
            _cuadro.anchoredPosition = Vector2.zero;
        }
        public void OnPointerClick(UnityEngine.EventSystems.PointerEventData datos)
        {
            if (datos.button == UnityEngine.EventSystems.PointerEventData.InputButton.Left && _escribiendo)
                CompletarTexto();
        }
        private void CompletarTexto()
        {
            _escribiendo = false;
            _texto.maxVisibleCharacters = int.MaxValue;
            _siguiente.interactable = true;
            _flecha.alpha = 1f;
        }
        public void Avanzar()
        {
            if (!EstaAbierto) return;
            // Un clic durante la escritura solo completa; no salta el mensaje.
            if (_escribiendo) CompletarTexto();
            else _continuar = true;
        }
    }
}
