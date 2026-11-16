using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace RoverMars.UI.Editor
{
    // Llamar desde un script de editor. Nunca procesa automáticamente otros assets.
    public static class QuitarFondoBlanco
    {
        public static void Procesar(string origen, string destino, bool recortar = true, byte umbral = 240)
        {
            if (origen == destino) throw new System.ArgumentException("Usa un destino distinto para conservar el original.");
            var textura = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Texture2D salida = null;
            try
            {
                if (!ImageConversion.LoadImage(textura, File.ReadAllBytes(origen)))
                    throw new System.InvalidOperationException("No se puede leer " + origen);
                int ancho = textura.width, alto = textura.height;
                Color32[] pixeles = textura.GetPixels32();
                var visitados = new bool[pixeles.Length];
                var cola = new Queue<int>();
                // Relleno desde los bordes: conserva los blancos encerrados en el dibujo.
                for (int x = 0; x < ancho; x++) { cola.Enqueue(x); cola.Enqueue((alto - 1) * ancho + x); }
                for (int y = 0; y < alto; y++) { cola.Enqueue(y * ancho); cola.Enqueue(y * ancho + ancho - 1); }
                while (cola.Count > 0)
                {
                    int i = cola.Dequeue();
                    if (visitados[i]) continue;
                    visitados[i] = true;
                    Color32 p = pixeles[i];
                    if (p.a != 0 && (p.r < umbral || p.g < umbral || p.b < umbral)) continue;
                    p.a = 0; pixeles[i] = p;
                    int x = i % ancho, y = i / ancho;
                    if (x > 0) cola.Enqueue(i - 1);
                    if (x + 1 < ancho) cola.Enqueue(i + 1);
                    if (y > 0) cola.Enqueue(i - ancho);
                    if (y + 1 < alto) cola.Enqueue(i + ancho);
                }
                int minX = ancho, minY = alto, maxX = -1, maxY = -1;
                for (int y = 0; y < alto; y++) for (int x = 0; x < ancho; x++)
                {
                    if (pixeles[y * ancho + x].a == 0) continue;
                    minX = Mathf.Min(minX, x); minY = Mathf.Min(minY, y);
                    maxX = Mathf.Max(maxX, x); maxY = Mathf.Max(maxY, y);
                }
                if (maxX < minX) throw new System.InvalidOperationException("Imagen vacía.");
                if (!recortar) { minX = minY = 0; maxX = ancho - 1; maxY = alto - 1; }
                int w = maxX - minX + 1, h = maxY - minY + 1;
                salida = new Texture2D(w, h, TextureFormat.RGBA32, false);
                var recorte = new Color32[w * h];
                for (int y = 0; y < h; y++) System.Array.Copy(pixeles, (y + minY) * ancho + minX, recorte, y * w, w);
                salida.SetPixels32(recorte); salida.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(destino));
                File.WriteAllBytes(destino, salida.EncodeToPNG());
                PrepararSpritesUI.Importar(destino, false);
            }
            finally
            {
                Object.DestroyImmediate(textura);
                if (salida != null) Object.DestroyImmediate(salida);
            }
        }
    }
}
