using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RoverMars.Grabacion
{
    // Lanza la grabación desde el menú RoverMars o cuando aparece el archivo
    // GrabarDemo.trigger en la raíz del proyecto (contenido: "20s" o "lectura").
    // Inicia Play desde StartScreen sin cambiar la escena abierta.
    [InitializeOnLoad]
    public static class GrabacionDemoLanzador
    {
        private static double _siguienteRevision;
        private static string Disparador => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "GrabarDemo.trigger"));

        static GrabacionDemoLanzador()
        {
            EditorApplication.update += Revisar;
            EditorApplication.playModeStateChanged += estado =>
            {
                if (estado == PlayModeStateChange.EnteredEditMode) EditorSceneManager.playModeStartScene = null;
            };
        }

        private static void Revisar()
        {
            if (EditorApplication.timeSinceStartup < _siguienteRevision) return;
            _siguienteRevision = EditorApplication.timeSinceStartup + 0.5;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            if (!File.Exists(Disparador)) return;
            string perfil = File.ReadAllText(Disparador).Trim();
            File.Delete(Disparador);
            Grabar(perfil == "lectura" ? "lectura" : "20s");
        }

        [MenuItem("RoverMars/Grabar demo (25 s)")]
        public static void GrabarRapida() => Grabar("20s");

        [MenuItem("RoverMars/Grabar demo (lectura cómoda)")]
        public static void GrabarLectura() => Grabar("lectura");

        private static void Grabar(string perfil)
        {
            var escena = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/StartScreen.unity");
            EditorSceneManager.playModeStartScene = escena;
            SessionState.SetBool(GrabacionDemo.ClaveSesion, true);
            SessionState.SetString(GrabacionDemo.ClavePerfil, perfil);
            Directory.CreateDirectory(GrabacionDemo.Carpeta);
            File.Delete(Path.Combine(GrabacionDemo.Carpeta, "listo.txt"));
            EditorApplication.isPlaying = true;
        }
    }
}
