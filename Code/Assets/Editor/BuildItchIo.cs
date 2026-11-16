using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RoverMars.Build
{
    // Build WebGL listo para itch.io: index.html en la raíz, Gzip con descompresión
    // de respaldo (itch.io no envía las cabeceras Content-Encoding) y lienzo 16:9.
    // Se lanza desde el menú o creando el archivo BuildWebGL.trigger en la raíz del proyecto.
    [InitializeOnLoad]
    public static class BuildItchIo
    {
        private static string Raiz => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        private static string Disparador => Path.Combine(Raiz, "BuildWebGL.trigger");
        public static string Salida => Path.Combine(Raiz, "Builds", "WebGL_itchio");
        private static string Resultado => Path.Combine(Raiz, "Builds", "build_resultado.txt");
        private static double _siguiente;

        static BuildItchIo()
        {
            EditorApplication.update += () =>
            {
                if (EditorApplication.timeSinceStartup < _siguiente) return;
                _siguiente = EditorApplication.timeSinceStartup + 1.0;
                if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling ||
                    BuildPipeline.isBuildingPlayer || !File.Exists(Disparador)) return;
                File.Delete(Disparador);
                Directory.CreateDirectory(Path.GetDirectoryName(Resultado));
                File.WriteAllText(Resultado, "trigger recibido\n");
                try { Construir(); }
                catch (System.Exception e) { File.AppendAllText(Resultado, "resultado: Failed\nERROR: " + e + "\n"); }
            };
        }

        [MenuItem("RoverMars/Build WebGL para itch.io")]
        public static void Construir()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Resultado));
            File.WriteAllText(Resultado, "construyendo\n");
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            {
                File.WriteAllText(Resultado, "resultado: Failed\nERROR: el módulo WebGL no está disponible en este Editor (reinicia Unity o instálalo desde Unity Hub)\n");
                return;
            }

            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.defaultWebScreenWidth = 1280;
            PlayerSettings.defaultWebScreenHeight = 720;
            PlayerSettings.runInBackground = true;

            string[] escenas = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (Directory.Exists(Salida)) Directory.Delete(Salida, true);

            var opciones = new BuildPlayerOptions
            {
                scenes = escenas,
                locationPathName = Salida,
                target = BuildTarget.WebGL,
                targetGroup = BuildTargetGroup.WebGL,
                options = BuildOptions.None
            };
            BuildReport reporte = BuildPipeline.BuildPlayer(opciones);
            var r = reporte.summary;
            string texto = $"resultado: {r.result}\nescenas: {string.Join(", ", escenas)}\n" +
                $"errores: {r.totalErrors}\nadvertencias: {r.totalWarnings}\n" +
                $"tamaño: {r.totalSize / (1024f * 1024f):F1} MB\nduración: {r.totalTime}\nsalida: {Salida}\n";
            foreach (var paso in reporte.steps)
                foreach (var m in paso.messages)
                    if (m.type == LogType.Error || m.type == LogType.Exception)
                        texto += "ERROR: " + m.content + "\n";
            File.WriteAllText(Resultado, texto);
            Debug.Log("[BuildItchIo] " + texto);
        }
    }
}
