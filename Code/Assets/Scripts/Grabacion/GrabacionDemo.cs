#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using RoverMars.UI;
using UnityEditor;
using UnityEditor.Media;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RoverMars.Grabacion
{
    // Solo existe en el Editor. Reproduce la demo automáticamente y la graba a MP4.
    // No modifica la lógica del juego: pulsa los mismos botones y solo sube
    // Time.timeScale mientras las plantas crecen, así el reloj y las barras se aceleran igual.
    public sealed class GrabacionDemo : MonoBehaviour
    {
        public const string ClaveSesion = "RoverMars.GrabarDemo";
        public const string ClavePerfil = "RoverMars.GrabarDemo.Perfil"; // "20s" o "lectura"
        public static string Carpeta => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Grabaciones"));
        private static string RutaRegistro => Path.Combine(Carpeta, "registro.txt");

        private const int Fps = 30;
        private const float DuracionMaxima = 23.9f;  // versión corta: máximo 24 s
        private const float TrasDidYouKnow = 5.6f;   // crecimiento 2 s + cosecha + RadLab (sin la lectura final)

        // Ritmo de lectura (segundos) según el perfil.
        private struct Ritmo
        {
            public float bienvenida, vineta, suerte, didYouKnow, radLab, pausa, fpsHerramienta;
        }
        private static readonly Ritmo Rapido = new Ritmo
        { bienvenida = 1.0f, vineta = 0.42f, suerte = 0.15f, didYouKnow = 1.8f, radLab = 0.95f, pausa = 0.05f, fpsHerramienta = 60 };
        private static readonly Ritmo Lectura = new Ritmo
        { bienvenida = 7f, vineta = 2.2f, suerte = 0.9f, didYouKnow = 4.5f, radLab = 3.5f, pausa = 0.35f, fpsHerramienta = 12 };

        private Ritmo _r;
        private string _perfil;
        private string RutaVideo => Path.Combine(Carpeta, _perfil == "lectura" ? "RoverMars_demo_lectura.mp4" : "RoverMars_demo_25s.mp4");

        private MediaEncoder _encoder;
        private int _w, _h, _frames;
        private float _t0;
        private bool _grabando, _terminado;
        private string _muestraPendiente;
        private float _zoom = 1f, _zoomObjetivo = 1f, _ultimoReal;
        private Vector2 _zoomCentro = new Vector2(0.5f, 0.5f);
        private RectTransform _cursor;
        private Canvas _cursorCanvas;
        private Vector2 _pos;
        private InvernaderoTools _herramientas;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Iniciar()
        {
            if (!SessionState.GetBool(ClaveSesion, false)) return;
            SessionState.SetBool(ClaveSesion, false);
            var go = new GameObject("GrabacionDemo");
            DontDestroyOnLoad(go);
            go.AddComponent<GrabacionDemo>();
        }

        private static void Log(string texto)
        {
            Debug.Log("[GrabacionDemo] " + texto);
            try { File.AppendAllText(RutaRegistro, $"{Time.realtimeSinceStartup:F2} {texto}\n"); } catch { }
        }

        private float T => Time.realtimeSinceStartup - _t0;

        private void Start()
        {
            _perfil = SessionState.GetString(ClavePerfil, "20s");
            _r = _perfil == "lectura" ? Lectura : Rapido;
            Directory.CreateDirectory(Carpeta);
            try { File.WriteAllText(RutaRegistro, "Perfil " + _perfil + "\n"); } catch { }
            Application.runInBackground = true;
            StartCoroutine(Principal());
        }

        private IEnumerator Principal()
        {
            yield return new WaitForEndOfFrame();
            float escala = Mathf.Min(1f, 1920f / Screen.width, 1080f / Screen.height);
            _w = Mathf.RoundToInt(Screen.width * escala) & ~1;
            _h = Mathf.RoundToInt(Screen.height * escala) & ~1;
            Log($"Pantalla {Screen.width}x{Screen.height} -> video {_w}x{_h}");
            var attrs = new VideoTrackAttributes
            {
                frameRate = new MediaRational(Fps),
                width = (uint)_w,
                height = (uint)_h,
                includeAlpha = false,
                bitRateMode = VideoBitrateMode.High
            };
            if (File.Exists(RutaVideo)) File.Delete(RutaVideo);
            _encoder = new MediaEncoder(RutaVideo, attrs);
            CrearCursor();
            _t0 = Time.realtimeSinceStartup;
            _grabando = true;
            StartCoroutine(Capturar());

            IEnumerator secuencia = Secuencia();
            while (true)
            {
                bool sigue;
                try { sigue = secuencia.MoveNext(); }
                catch (Exception e) { Log("ERROR: " + e); sigue = false; }
                if (!sigue) break;
                if (T > 90f) { Log("Tiempo límite: se detiene la secuencia"); break; }
                yield return secuencia.Current;
            }

            Time.timeScale = 1f;
            _grabando = false;
            while (!_terminado) yield return null;
            Log($"Video terminado: {_frames} fotogramas ({_frames / (float)Fps:F2} s) en {RutaVideo}");
            File.WriteAllText(Path.Combine(Carpeta, "listo.txt"), "ok");
            EditorApplication.isPlaying = false;
        }

        private IEnumerator Capturar()
        {
            RenderTexture rtPantalla = null;
            var rtVideo = new RenderTexture(_w, _h, 0, RenderTextureFormat.ARGB32);
            var frame = new Texture2D(_w, _h, TextureFormat.RGBA32, false);
            while (_grabando)
            {
                yield return new WaitForEndOfFrame();
                if (!_grabando) break;
                if (rtPantalla == null || rtPantalla.width != Screen.width || rtPantalla.height != Screen.height)
                {
                    if (rtPantalla != null) rtPantalla.Release();
                    rtPantalla = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
                }
                ScreenCapture.CaptureScreenshotIntoRenderTexture(rtPantalla);
                // Zoom suave de cámara (solo en el video): se acerca a _zoomCentro.
                float ahora = Time.realtimeSinceStartup;
                float dt = _ultimoReal > 0f ? ahora - _ultimoReal : 0f;
                _ultimoReal = ahora;
                _zoom = Mathf.Lerp(_zoom, _zoomObjetivo, 1f - Mathf.Exp(-dt * 9f));
                if (Mathf.Abs(_zoom - _zoomObjetivo) < 0.002f) _zoom = _zoomObjetivo;
                float inv = 1f / _zoom;
                Vector2 c = new Vector2(Mathf.Clamp(_zoomCentro.x, 0.5f * inv, 1f - 0.5f * inv),
                    Mathf.Clamp(_zoomCentro.y, 0.5f * inv, 1f - 0.5f * inv));
                if (SystemInfo.graphicsUVStartsAtTop)
                    Graphics.Blit(rtPantalla, rtVideo, new Vector2(inv, -inv), new Vector2(c.x - 0.5f * inv, 1f - c.y + 0.5f * inv));
                else
                    Graphics.Blit(rtPantalla, rtVideo, new Vector2(inv, inv), new Vector2(c.x - 0.5f * inv, c.y - 0.5f * inv));
                var previo = RenderTexture.active;
                RenderTexture.active = rtVideo;
                frame.ReadPixels(new Rect(0, 0, _w, _h), 0, 0, false);
                frame.Apply(false);
                RenderTexture.active = previo;
                if (_muestraPendiente != null)
                {
                    File.WriteAllBytes(Path.Combine(Carpeta, $"muestra_{_perfil}_{_muestraPendiente}.png"), frame.EncodeToPNG());
                    _muestraPendiente = null;
                }
                int objetivo = Mathf.FloorToInt(T * Fps) + 1;
                while (_frames < objetivo)
                {
                    _encoder.AddFrame(frame);
                    _frames++;
                }
            }
            if (rtPantalla != null) rtPantalla.Release();
            rtVideo.Release();
            _encoder.Dispose();
            _encoder = null;
            _terminado = true;
        }

        private void Muestra(string nombre) => _muestraPendiente = nombre;

        // ---------- Secuencia de la demo ----------

        private IEnumerator Secuencia()
        {
            // 1. START
            var anim = FindAnyObjectByType<StartScreenAnimation>();
            while (anim != null && !anim.IsComplete) yield return null;
            var start = GameObject.Find("StartButton");   // oculto hasta que termina la animación
            yield return Esperar(0.1f);
            yield return Clic(start);
            while (SceneManager.GetActiveScene().name != "Tutorial_P2") yield return null;
            yield return null;
            Log($"Escena tutorial cargada t={T:F2}");

            var mensaje = FindAnyObjectByType<MensajeCentral>(FindObjectsInactive.Include);
            var vinetas = FindAnyObjectByType<VinetaManager>(FindObjectsInactive.Include);
            var flujo = FindAnyObjectByType<TutorialFlujo>(FindObjectsInactive.Include);
            _herramientas = FindAnyObjectByType<InvernaderoTools>(FindObjectsInactive.Include);
            var didYouKnow = FindAnyObjectByType<DidYouKnowUI>(FindObjectsInactive.Include);
            var radLab = FindAnyObjectByType<RadLabUI>(FindObjectsInactive.Include);
            var barras = FindAnyObjectByType<BarrasEstado>(FindObjectsInactive.Include);
            var plantas = FindObjectsByType<Planta>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Array.Sort(plantas, (a, b) => Pantalla(Campo<Button>(a, "_boton").gameObject).x
                .CompareTo(Pantalla(Campo<Button>(b, "_boton").gameObject).x));
            Asignar(_herramientas, "_fps", Mathf.RoundToInt(_r.fpsHerramienta));

            // 2. Tutorial: bienvenida, viñetas y "Good Luck!"
            var siguienteMensaje = Campo<Button>(mensaje, "_siguiente");
            var cuadroMensaje = Campo<RectTransform>(mensaje, "_cuadro").gameObject;
            while (!mensaje.EstaAbierto) yield return null;
            yield return Esperar(0.2f);
            yield return Clic(cuadroMensaje, mensaje.gameObject);   // clic en el cuadro: muestra todo el texto
            while (!siguienteMensaje.interactable) yield return null;
            yield return Esperar(_r.bienvenida);
            Muestra("1_bienvenida");
            yield return Clic(siguienteMensaje.gameObject);          // flecha: continúa

            var siguienteVineta = Campo<Button>(vinetas, "_siguiente");
            int n = 0;
            while (!vinetas.SecuenciaEnCurso) yield return null;
            while (vinetas.SecuenciaEnCurso && n < 10)
            {
                yield return Clic(siguienteVineta.gameObject); // completa el texto
                yield return Esperar(_r.vineta);
                if (n == 0) Muestra("2_vineta");
                yield return Clic(siguienteVineta.gameObject); // flecha: siguiente viñeta
                n++;
                yield return null;
            }
            Log($"Viñetas: {n} t={T:F2}");
            while (!mensaje.EstaAbierto) yield return null;
            while (!siguienteMensaje.interactable) yield return null;   // "Good Luck!" es corto
            yield return Esperar(_r.suerte);
            yield return Clic(siguienteMensaje.gameObject);
            while (flujo.TutorialActivo) yield return null;
            Log($"Tutorial terminado t={T:F2}");

            // 3-4. Mapa -> GreenHouse
            var botonMapa = Campo<Button>(flujo, "_botonMapa");
            yield return Clic(botonMapa.gameObject);
            yield return Esperar(_r.pausa);
            Muestra("3_mapa");
            yield return Clic(Hotspot(RoomId.Invernadero));
            yield return Esperar(_r.pausa);
            Log($"Invernadero t={T:F2}");

            // 5. Regadera y riego de las dos plantas
            var regadera = Campo<Image>(_herramientas, "_regaderaIcono").gameObject;
            yield return Clic(regadera);
            bool muestraRiego = false;
            foreach (var planta in plantas)
            {
                yield return UsarHerramienta(regadera, planta, Planta.Estado.Brote);
                if (!muestraRiego) { Muestra("4_riego"); muestraRiego = true; }
                while (Campo<bool>(_herramientas, "_ocupado")) yield return null;
            }
            Log($"Regadas t={T:F2} estados: {plantas[0].EstadoActual}, {plantas[plantas.Length - 1].EstadoActual}");

            // 6. "Did you know?" -> X dos veces
            var botonX = Campo<Button>(didYouKnow, "_botonX");
            float limite = T + 3f;
            // El juego espera 1 s (tiempo escalado) antes del cuadro; en la versión corta se acorta.
            if (_perfil != "lectura") Time.timeScale = 8f;
            while (!didYouKnow.EstaAbierto && T < limite) yield return null;
            Time.timeScale = 1f;
            if (didYouKnow.EstaAbierto)
            {
                // Zoom del video sobre el cuadro para que el texto se lea mejor.
                var cuadroDyk = Campo<RectTransform>(didYouKnow, "_cuadro");
                Vector3[] esquinas = new Vector3[4];
                cuadroDyk.GetWorldCorners(esquinas);   // overlay: coordenadas de pantalla
                float anchoRel = (esquinas[2].x - esquinas[0].x) / Screen.width;
                _zoomCentro = new Vector2((esquinas[0].x + esquinas[2].x) * 0.5f / Screen.width,
                    (esquinas[0].y + esquinas[2].y) * 0.5f / Screen.height);
                _zoomObjetivo = Mathf.Clamp(0.94f / Mathf.Max(0.1f, anchoRel), 1f, 1.7f);
                // En la versión corta el tiempo de lectura se ajusta al que queda para terminar
                // en DuracionMaxima (lo que sigue al segundo cuadro dura ~TrasDidYouKnow s).
                float lectura = _r.didYouKnow;
                if (_perfil != "lectura")
                    lectura = Mathf.Clamp((DuracionMaxima - TrasDidYouKnow - T - 0.7f) / 2f, 0.9f, 2.6f);
                Log($"Zoom x{_zoomObjetivo:F2} centro {_zoomCentro}; lectura {lectura:F2} s por cuadro");
                yield return Esperar(lectura);
                Muestra("5_didyouknow");
                yield return Clic(botonX.gameObject);
                yield return Esperar(lectura);
                Muestra("5b_didyouknow");
                yield return Clic(botonX.gameObject);
                _zoomObjetivo = 1f;
                yield return Esperar(0.1f);
            }
            else Log("Did you know no apareció");

            // 7. Crecimiento acelerado: 10 min de juego en 2 s reales.
            // Time.timeScale tiene un máximo de 100 (6 s), así que se suma el doble del
            // delta escalado a los mismos temporizadores de las plantas y del Sol (x300).
            float inicioCrecimiento = T;
            Log($"Antes de crecer: sol={barras.sol} salud={barras.Salud} agua={barras.Agua} energia={barras.Energia} progresoSol={barras.ProgresoSol:F2}");
            Time.timeScale = 100f;
            bool muestraCrecimiento = false;
            while (Array.Exists(plantas, p => p.EstadoActual == Planta.Estado.Creciendo))
            {
                yield return null;
                float extra = 2f * Time.deltaTime;
                foreach (var p in plantas)
                    if (p.EstadoActual == Planta.Estado.Creciendo)
                        Asignar(p, "_tiempoCrecimiento", Campo<float>(p, "_tiempoCrecimiento") + extra);
                Asignar(barras, "_tiempoSol", Campo<float>(barras, "_tiempoSol") + extra);
                if (!muestraCrecimiento && T - inicioCrecimiento > 1f) { Muestra("6_creciendo"); muestraCrecimiento = true; }
            }
            Time.timeScale = 1f;
            Log($"Crecimiento: {T - inicioCrecimiento:F2} s reales; sol={barras.sol} agua={barras.Agua} energia={barras.Energia}");
            yield return Esperar(0.12f);
            Muestra("7_maduras");

            // 8-9. Pala y cosecha
            var pala = Campo<Image>(_herramientas, "_palaIcono").gameObject;
            yield return Clic(pala);
            foreach (var planta in plantas)
            {
                yield return UsarHerramienta(pala, planta, Planta.Estado.Madura);
                while (Campo<bool>(_herramientas, "_ocupado")) yield return null;
            }
            Log($"Cosechadas t={T:F2} salud={barras.Salud}");
            yield return Esperar(_r.pausa);

            // 10-12. Mapa -> Mission Control -> RadLab
            yield return Clic(botonMapa.gameObject);
            yield return Esperar(_r.pausa);
            yield return Clic(Hotspot(RoomId.CentroDeControl));
            yield return Esperar(_r.pausa);
            yield return Clic(Campo<Button>(radLab, "_boton").gameObject);
            Log($"RadLab abierto={radLab.EstaAbierto} t={T:F2}");

            // 13. Flechas de los cuadros, si las hay; tiempo de lectura y fin.
            var raiz = Campo<GameObject>(radLab, "_raiz");
            foreach (var b in raiz.GetComponentsInChildren<Button>(false))
            {
                Log("Botón en RadLab: " + b.name);
                yield return Esperar(_r.radLab * 0.5f);
                yield return Clic(b.gameObject);
            }
            float final = _r.radLab;
            if (_perfil != "lectura") final = Mathf.Clamp(DuracionMaxima - 0.1f - T, 0.6f, 1.6f);
            yield return Esperar(final * 0.5f);
            Muestra("8_radlab");
            yield return Esperar(final * 0.5f);
            Log($"Fin t={T:F2}");
        }

        // ---------- Utilidades ----------

        // Pulsa la planta con la herramienta; si la herramienta se deseleccionó
        // (p. ej. por un cambio de foco del Editor) la vuelve a elegir y reintenta.
        private IEnumerator UsarHerramienta(GameObject herramienta, Planta planta, Planta.Estado estadoEsperado)
        {
            var boton = Campo<Button>(planta, "_boton").gameObject;
            for (int intento = 0; intento < 4 && planta.EstadoActual == estadoEsperado; intento++)
            {
                if (!_herramientas.HerramientaSeleccionada)
                {
                    Log($"Herramienta deseleccionada (foco={Campo<bool>(_herramientas, "_foco")}); se vuelve a elegir");
                    yield return Clic(herramienta);
                }
                yield return Clic(boton);
                yield return null;
                if (Campo<bool>(_herramientas, "_ocupado")) yield break;
                Log($"La planta {planta.name} no respondió (intento {intento + 1})");
            }
        }

        private static WaitForSecondsRealtime Esperar(float s) => new WaitForSecondsRealtime(s);

        private GameObject Hotspot(RoomId id)
        {
            foreach (var h in FindObjectsByType<HotspotHabitacion>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (Campo<RoomId>(h, "_roomId") == id) return h.gameObject;
            Log("No se encontró hotspot " + id);
            return null;
        }

        private static T Campo<T>(object obj, string nombre)
        {
            Type t = obj.GetType();
            while (t != null)
            {
                FieldInfo f = t.GetField(nombre, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (f != null) return (T)f.GetValue(obj);
                t = t.BaseType;
            }
            throw new MissingFieldException(obj.GetType().Name, nombre);
        }

        private static void Asignar(object obj, string nombre, object valor)
        {
            FieldInfo f = obj.GetType().GetField(nombre, BindingFlags.Instance | BindingFlags.NonPublic);
            f?.SetValue(obj, valor);
        }

        private static Vector2 Pantalla(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            var canvas = go.GetComponentInParent<Canvas>();
            Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            return RectTransformUtility.WorldToScreenPoint(cam, rt.TransformPoint(rt.rect.center));
        }

        // Mueve el cursor simulado al objeto y envía el clic al EventSystem del juego.
        private IEnumerator Clic(GameObject destino, GameObject receptorForzado = null)
        {
            if (destino == null) { Log("Clic sobre objeto nulo"); yield break; }
            GameObject go = receptorForzado != null ? receptorForzado : destino;
            Vector2 desde = _pos, hasta = Pantalla(destino);
            float distancia = Vector2.Distance(desde, hasta) / Mathf.Max(1f, Screen.height / 1080f);
            if (distancia > 4f)
            {
                float duracion = Mathf.Lerp(0.1f, 0.22f, Mathf.Clamp01(distancia / 900f));
                float t0 = Time.realtimeSinceStartup;
                while (true)
                {
                    float k = Mathf.Clamp01((Time.realtimeSinceStartup - t0) / duracion);
                    _pos = Vector2.Lerp(desde, hasta, k * k * (3f - 2f * k));
                    if (k >= 1f) break;
                    yield return null;
                }
                yield return Esperar(0.05f);
            }
            var datos = new PointerEventData(EventSystem.current)
            {
                position = hasta,
                pressPosition = hasta,
                button = PointerEventData.InputButton.Left,
                clickCount = 1,
                eligibleForClick = true
            };
            ExecuteEvents.ExecuteHierarchy(go, datos, ExecuteEvents.pointerEnterHandler);
            ExecuteEvents.ExecuteHierarchy(go, datos, ExecuteEvents.pointerDownHandler);
            if (_cursor != null) _cursor.localScale = Vector3.one * 0.82f;
            yield return Esperar(0.06f);
            if (_cursor != null) _cursor.localScale = Vector3.one;
            ExecuteEvents.ExecuteHierarchy(go, datos, ExecuteEvents.pointerUpHandler);
            GameObject receptor = ExecuteEvents.ExecuteHierarchy(go, datos, ExecuteEvents.pointerClickHandler);
            ExecuteEvents.ExecuteHierarchy(go, datos, ExecuteEvents.pointerExitHandler);
            Log($"Clic {go.name} -> {(receptor != null ? receptor.name : "nadie")} t={T:F2}");
        }

        private void LateUpdate()
        {
            if (_cursor == null) return;
            // El cursor de herramienta del invernadero sigue al cursor simulado.
            bool herramienta = false;
            if (_herramientas != null)
            {
                Asignar(_herramientas, "_puntero", _pos);
                Asignar(_herramientas, "_esRaton", true);
                Asignar(_herramientas, "_foco", true);
                herramienta = _herramientas.isActiveAndEnabled && _herramientas.HerramientaSeleccionada;
            }
            _cursorCanvas.enabled = !herramienta;
            var padre = (RectTransform)_cursor.parent;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(padre, _pos, null, out Vector2 local))
                _cursor.anchoredPosition = local;
        }

        private void CrearCursor()
        {
            var go = new GameObject("CursorDemo", typeof(Canvas), typeof(CanvasScaler));
            go.transform.SetParent(transform, false);
            _cursorCanvas = go.GetComponent<Canvas>();
            _cursorCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _cursorCanvas.sortingOrder = 32767;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            string[] dibujo =
            {
                "X...........",
                "XX..........",
                "XWX.........",
                "XWWX........",
                "XWWWX.......",
                "XWWWWX......",
                "XWWWWWX.....",
                "XWWWWWWX....",
                "XWWWWWWWX...",
                "XWWWWWWWWX..",
                "XWWWWWWWWWX.",
                "XWWWWWWXXXXX",
                "XWWWXWWX....",
                "XWWXXWWX....",
                "XWX..XWWX...",
                "XX...XWWX...",
                "X.....XWWX..",
                "......XWWX..",
                ".......XX...",
            };
            int w = dibujo[0].Length, h = dibujo.Length;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    char c = dibujo[h - 1 - y][x];
                    tex.SetPixel(x, y, c == 'X' ? Color.black : c == 'W' ? Color.white : Color.clear);
                }
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0f, 1f), 1f);
            var img = new GameObject("Flecha", typeof(RectTransform), typeof(Image));
            img.transform.SetParent(go.transform, false);
            _cursor = img.GetComponent<RectTransform>();
            // Anclado al centro: coincide con el origen de ScreenPointToLocalPointInRectangle.
            _cursor.anchorMin = _cursor.anchorMax = new Vector2(0.5f, 0.5f);
            _cursor.pivot = new Vector2(0f, 1f);
            _cursor.sizeDelta = new Vector2(w, h) * 3f;
            var image = img.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            _pos = new Vector2(Screen.width * 0.5f, Screen.height * 0.25f);
        }
    }
}
#endif
