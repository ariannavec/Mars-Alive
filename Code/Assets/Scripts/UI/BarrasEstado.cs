using UnityEngine;

namespace RoverMars.UI
{
    public sealed class BarrasEstado : MonoBehaviour
    {
        [Header("Valores actuales (0 a 100)")]
        [SerializeField, Range(0f, 100f)] private float salud = 100f;
        [SerializeField, Range(0f, 100f)] private float agua = 100f;
        [SerializeField, Range(0f, 100f)] private float energia = 100f;

        [Header("Rellenos de las barras")]
        [SerializeField] private UnityEngine.UI.Image _rellenoSalud;
        [SerializeField] private UnityEngine.UI.Image _rellenoAgua;
        [SerializeField] private UnityEngine.UI.Image _rellenoEnergia;

        [Header("Sprites 8-bit (reemplazables)")]
        [SerializeField] private Sprite _track8bit;
        [SerializeField] private Sprite _spriteSalud;
        [SerializeField] private Sprite _spriteAgua;
        [SerializeField] private Sprite _spriteEnergia;

        [Header("Integración con el HUD existente")]
        [Tooltip("Imagen antigua que TutorialP2Setup conserva como referencia.")]
        [SerializeField] private GameObject _simbolosAnterior;

        [Header("Paso del Sol")]
        [Min(0.01f)] public float duracionSol = 600f;
        public int sol = 1;
        public event System.Action OnSolTerminado;
        private float _tiempoSol;

        public float Salud => salud;
        public float Agua => agua;
        public float Energia => energia;
        // El reloj comparte este temporizador; no mantiene uno independiente.
        public float ProgresoSol => Mathf.Clamp01(_tiempoSol / Mathf.Max(0.01f, duracionSol));

        private void Awake()
        {
            salud = agua = energia = 100f;
            sol = 1;
            _tiempoSol = 0f;
            AplicarCapsula(_rellenoSalud);
            AplicarCapsula(_rellenoAgua);
            AplicarCapsula(_rellenoEnergia);
            ActualizarRellenos();
        }

        private void Start()
        {
            // Todos los Awake han terminado: ocultamos solo el rótulo anterior.
            if (_simbolosAnterior != null) _simbolosAnterior.SetActive(false);
        }

        private void Update()
        {
            // Corre también con mapa o viñetas abiertas; estos no pausan el tiempo.
            _tiempoSol += Time.deltaTime;
            float duracion = Mathf.Max(0.01f, duracionSol);
            while (_tiempoSol >= duracion)
            {
                // Conservamos el sobrante para no perder tiempo entre fotogramas.
                _tiempoSol -= duracion;
                CambiarAgua(-10f);
                CambiarEnergia(-10f);
                sol++;
                OnSolTerminado?.Invoke();
            }
        }

        public void CambiarSalud(float cantidad)
        {
            salud = Mathf.Clamp(salud + cantidad, 0f, 100f);
            ActualizarRellenos();
        }

        public void CambiarAgua(float cantidad)
        {
            agua = Mathf.Clamp(agua + cantidad, 0f, 100f);
            ActualizarRellenos();
        }

        public void CambiarEnergia(float cantidad)
        {
            energia = Mathf.Clamp(energia + cantidad, 0f, 100f);
            ActualizarRellenos();
        }

        // Solo liberamos sprites generados en ejecución, nunca los del Inspector.
        private readonly System.Collections.Generic.List<Sprite> _spritesGenerados =
            new System.Collections.Generic.List<Sprite>();

        private void AplicarCapsula(UnityEngine.UI.Image relleno)
        {
            if (relleno == null) return;
            if (_track8bit == null)
            {
                _track8bit = PixelArt.CrearTrackBarra();
                _spritesGenerados.Add(_track8bit);
            }
            Sprite sprite = relleno == _rellenoSalud ? _spriteSalud :
                relleno == _rellenoAgua ? _spriteAgua : _spriteEnergia;
            if (sprite == null)
            {
                Color color = relleno == _rellenoSalud ? new Color32(255, 87, 87, 255) :
                    relleno == _rellenoAgua ? new Color32(14, 192, 224, 255) : new Color32(252, 186, 87, 255);
                sprite = PixelArt.CrearRellenoBarra("RellenoBarra8bit", color,
                    Mathf.RoundToInt(relleno.rectTransform.rect.width),
                    Mathf.RoundToInt(relleno.rectTransform.rect.height));
                _spritesGenerados.Add(sprite);
            }
            relleno.sprite = sprite;
            // Los colores están dibujados en el sprite: blanco evita doble tinte.
            relleno.color = Color.white;
            relleno.type = UnityEngine.UI.Image.Type.Filled;
            relleno.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            relleno.fillOrigin = (int)UnityEngine.UI.Image.OriginHorizontal.Left;
            if (relleno.transform.parent != null &&
                relleno.transform.parent.TryGetComponent<UnityEngine.UI.Image>(out var track))
            {
                track.sprite = _track8bit;
                track.color = Color.white;
                track.type = UnityEngine.UI.Image.Type.Sliced;
                track.pixelsPerUnitMultiplier = _track8bit.pixelsPerUnit / track.canvas.referencePixelsPerUnit;
            }
        }

        private void ActualizarRellenos()
        {
            ActualizarRelleno(_rellenoSalud, salud);
            ActualizarRelleno(_rellenoAgua, agua);
            ActualizarRelleno(_rellenoEnergia, energia);
        }

        private static void ActualizarRelleno(UnityEngine.UI.Image relleno, float valor)
        {
            if (relleno == null) return;
            // Solo cambia la representación; los valores y temporizadores no se redondean.
            // Si el ancho no es múltiplo de 4, el último paso es más corto.
            float pasos = Mathf.Max(1f, relleno.rectTransform.rect.width / 4f);
            relleno.fillAmount = Mathf.Clamp01(Mathf.Round(valor / 100f * pasos) / pasos);
        }

        private void OnDestroy()
        {
            foreach (var sprite in _spritesGenerados) PixelArt.Liberar(sprite);
        }
}
}
