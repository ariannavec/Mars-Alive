using UnityEngine;

namespace RoverMars.UI
{
    [RequireComponent(typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster))]
    public sealed class TutorialP2Setup : MonoBehaviour
    {
        [Header("Sprites del tutorial")]
        [SerializeField] private Sprite _misionControlSprite;
        [SerializeField] private Sprite _mapaSprite;
        [SerializeField] private Sprite _simbolosSprite;
        [SerializeField] private Sprite _progresoSprite;
        [Header("HUD recortado")]
        [SerializeField] private bool _hudRecortado;
        [SerializeField, Min(30f)] private float margenPantalla = 30f;
        [SerializeField] private Vector2 _tamanoMapa = new Vector2(200f, 113f);
        [SerializeField] private Vector2 _tamanoProgreso = new Vector2(72f, 64f);
        [SerializeField] private RectTransform[] _elementosConMargen;
        private readonly Vector3[] _esquinas = new Vector3[4];
        private Canvas _canvas;
        private RectTransform _canvasRect;

        private void LateUpdate()
        {
            if (_elementosConMargen == null || _canvas == null) return;
            float margen = Mathf.Max(30f, margenPantalla) / Mathf.Max(0.01f, _canvas.scaleFactor);
            Rect limites = _canvasRect.rect;
            foreach (var elemento in _elementosConMargen)
            {
                if (elemento == null || !elemento.gameObject.activeInHierarchy) continue;
                elemento.GetWorldCorners(_esquinas);
                Vector2 min = _canvasRect.InverseTransformPoint(_esquinas[0]);
                Vector2 max = _canvasRect.InverseTransformPoint(_esquinas[2]);
                Vector2 ajuste = Vector2.zero;
                if (min.x < limites.xMin + margen) ajuste.x = limites.xMin + margen - min.x;
                else if (max.x > limites.xMax - margen) ajuste.x = limites.xMax - margen - max.x;
                if (min.y < limites.yMin + margen) ajuste.y = limites.yMin + margen - min.y;
                else if (max.y > limites.yMax - margen) ajuste.y = limites.yMax - margen - max.y;
                elemento.position += _canvasRect.TransformVector(ajuste);
            }
        }

        [Header("Desplazamientos respecto al PNG original de 1920 x 1080")]
        [SerializeField] private Vector2 _progresoOffset = new Vector2(-632f, 435f);
        [SerializeField] private Vector2 _mapaOffset = new Vector2(723f, 405f);
        [SerializeField] private Vector2 _simbolosOffset = new Vector2(-585f, -410f);

        [Header("Contenido visible del HUD en los PNG opacos (sin modificar archivos)")]
        [SerializeField] private Rect _mapaContent = new Rect(804f, 463f, 322f, 119f);
        [SerializeField] private Rect _simbolosContent = new Rect(669f, 489f, 553f, 98f);
        [SerializeField] private Rect _progresoContent = new Rect(703f, 462f, 520f, 120f);

        [Header("Jerarquía generada (se reutiliza, no se destruye)")]
        [SerializeField] private UnityEngine.UI.Image _misionControl;
        [SerializeField] private RectTransform _hudPersistente;
        [SerializeField] private UnityEngine.UI.Image _mapa;
        [SerializeField] private UnityEngine.UI.Image _simbolos;
        [SerializeField] private UnityEngine.UI.Image _progreso;

        private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            _canvasRect = GetComponent<RectTransform>();
            BuildOrValidate();
        }

        // En el editor, aplica cambios de posición con el menú contextual
        // «Construir o validar tutorial». En ejecución se aplican desde Awake.

        [ContextMenu("Construir o validar tutorial")]
        public void BuildOrValidate()
        {
            if (_misionControlSprite == null || _mapaSprite == null || _simbolosSprite == null || _progresoSprite == null)
            {
                Debug.LogError("Asigna los cuatro sprites en TutorialP2Setup antes de construir la interfaz.", this);
                return;
            }

            Canvas canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            UnityEngine.UI.CanvasScaler scaler = GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            _misionControl = EnsureImage(transform, "Fondo", _misionControl);
            if (_hudPersistente == null)
                _hudPersistente = EnsureRect(transform, "HUD");
            _mapa = EnsureImage(_hudPersistente, "Mapa", _mapa);
            _simbolos = EnsureImage(_hudPersistente, "Simbolos", _simbolos);
            _progreso = EnsureImage(_hudPersistente, "Progreso", _progreso);
            EnsureClip(_mapa, _hudPersistente);
            EnsureClip(_simbolos, _hudPersistente);
            EnsureClip(_progreso, _hudPersistente);
            ApplyLayout();
        }

        private void ApplyLayout()
        {
            _misionControl.transform.SetSiblingIndex(0);
            Stretch(_misionControl.rectTransform);
            // RoomManager es el único responsable de cambiar el sprite de Fondo.
            _misionControl.preserveAspect = true;
            _misionControl.color = Color.white;
            _misionControl.raycastTarget = false;

            Stretch(_hudPersistente);
            int indiceHud = 1;
            foreach (Transform child in transform)
                if (child.name == "InvernaderoUI")
                {
                    child.SetSiblingIndex(1);
                    indiceHud = 2;
                    break;
                }
            _hudPersistente.SetSiblingIndex(indiceHud);
            foreach (Transform child in transform)
                if (child.name == "MapaOverlay")
                {
                    child.SetSiblingIndex(indiceHud);
                    _hudPersistente.SetSiblingIndex(indiceHud + 1);
                    break;
                }
            _hudPersistente.gameObject.SetActive(true);
            if (_hudRecortado)
            {
                ConfigurarRecortado(_mapa, _mapaSprite, new Vector2(1f, 1f), _tamanoMapa);
                ConfigurarRecortado(_progreso, _progresoSprite, new Vector2(0f, 1f), _tamanoProgreso);
            }
            else
            {
                ConfigureHudImage(_mapa, _mapaSprite, _mapaOffset, _mapaContent);
                ConfigureHudImage(_progreso, _progresoSprite, _progresoOffset, _progresoContent);
            }
            if (_hudRecortado)
                _simbolos.gameObject.SetActive(false);
            else
                ConfigureHudImage(_simbolos, _simbolosSprite, _simbolosOffset, _simbolosContent);
        }

        private void ConfigurarRecortado(UnityEngine.UI.Image image, Sprite sprite, Vector2 esquina, Vector2 tamano)
        {
            var clip = (RectTransform)image.transform.parent;
            float margen = Mathf.Max(30f, margenPantalla) / Mathf.Max(0.01f, GetComponent<Canvas>().scaleFactor);
            clip.anchorMin = clip.anchorMax = esquina;
            clip.pivot = esquina;
            clip.sizeDelta = tamano;
            clip.anchoredPosition = new Vector2(esquina.x == 1f ? -margen : margen, -margen);
            clip.GetComponent<UnityEngine.UI.RectMask2D>().padding = Vector4.zero;
            Stretch(image.rectTransform);
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = image.GetComponent<UnityEngine.UI.Button>() != null;
            image.color = Color.white;
        }

        private static void ConfigureHudImage(UnityEngine.UI.Image image, Sprite sprite, Vector2 offset, Rect content)
        {
            if (sprite == null)
                return;
            image.gameObject.SetActive(true);
            image.sprite = sprite;
            image.type = UnityEngine.UI.Image.Type.Simple;
            image.preserveAspect = true;
            // Las imágenes informativas no bloquean clics; Mapa ahora sí es botón.
            image.raycastTarget = image.GetComponent<UnityEngine.UI.Button>() != null;
            image.color = Color.white;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localScale = Vector3.one;
            rect.sizeDelta = ReferenceResolution;
            rect.anchoredPosition = offset;
            // Los PNG actuales son opacos. La máscara evita que sus márgenes
            // blancos cubran el fondo, pero cada Image conserva 1920 x 1080.
            UnityEngine.UI.RectMask2D mask = rect.parent.GetComponent<UnityEngine.UI.RectMask2D>();
            if (mask != null)
            {
                mask.padding = new Vector4(
                    content.xMin + offset.x,
                    content.yMin + offset.y,
                    ReferenceResolution.x - content.xMax - offset.x,
                    ReferenceResolution.y - content.yMax - offset.y);
            }
        }

        private static void EnsureClip(UnityEngine.UI.Image image, RectTransform hud)
        {
            RectTransform clip = EnsureRect(hud, image.name + "Recorte");
            clip.anchorMin = clip.anchorMax = new Vector2(0.5f, 0.5f);
            clip.pivot = new Vector2(0.5f, 0.5f);
            clip.sizeDelta = ReferenceResolution;
            clip.anchoredPosition = Vector2.zero;
            if (clip.GetComponent<UnityEngine.UI.RectMask2D>() == null)
                clip.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            image.rectTransform.SetParent(clip, false);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static RectTransform EnsureRect(Transform parent, string objectName)
        {
            foreach (Transform child in parent)
            {
                if (child.name == objectName && child is RectTransform rect)
                    return rect;
            }
            GameObject childObject = new GameObject(objectName, typeof(RectTransform));
            childObject.layer = parent.gameObject.layer;
            RectTransform newRect = childObject.GetComponent<RectTransform>();
            newRect.SetParent(parent, false);
            return newRect;
        }

        private static UnityEngine.UI.Image EnsureImage(Transform parent, string objectName, UnityEngine.UI.Image existing)
        {
            if (existing != null)
                return existing;
            RectTransform rect = EnsureRect(parent, objectName);
            UnityEngine.UI.Image image = rect.GetComponent<UnityEngine.UI.Image>();
            return image != null ? image : rect.gameObject.AddComponent<UnityEngine.UI.Image>();
        }
    }
}
