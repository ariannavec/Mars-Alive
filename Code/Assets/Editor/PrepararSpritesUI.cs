using System.IO;
using UnityEditor;
using UnityEngine;

namespace RoverMars.UI.Editor
{
    // Se ejecuta explícitamente: no modifica otros PNG ni usa AssetPostprocessor.
    public static class PrepararSpritesUI
    {
        public static void Preparar(string origen, string destino, bool quitarBlanco,
            bool recortar, RectInt region = default)
        {
            var textura = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!ImageConversion.LoadImage(textura, File.ReadAllBytes(origen)))
                    throw new System.InvalidOperationException("No se pudo leer el PNG: " + origen);
                Color32[] pixeles = textura.GetPixels32();
                int ancho = textura.width;
                int alto = textura.height;
                int minX = ancho, minY = alto, maxX = -1, maxY = -1;
                for (int y = 0; y < alto; y++)
                for (int x = 0; x < ancho; x++)
                {
                    int i = y * ancho + x;
                    Color32 p = pixeles[i];
                    // La región opcional de HELP conserva las letras blancas del botón.
                    bool dentro = region.width == 0 || region.Contains(new Vector2Int(x, y));
                    if (!dentro || (quitarBlanco && p.r > 240 && p.g > 240 && p.b > 240))
                        p.a = 0;
                    pixeles[i] = p;
                    if (p.a == 0) continue;
                    minX = Mathf.Min(minX, x); minY = Mathf.Min(minY, y);
                    maxX = Mathf.Max(maxX, x); maxY = Mathf.Max(maxY, y);
                }
                if (maxX < minX) throw new System.InvalidOperationException("El dibujo está vacío.");
                if (!recortar) { minX = 0; minY = 0; maxX = ancho - 1; maxY = alto - 1; }
                int w = maxX - minX + 1, h = maxY - minY + 1;
                var salida = new Texture2D(w, h, TextureFormat.RGBA32, false);
                try
                {
                    var recorte = new Color32[w * h];
                    for (int y = 0; y < h; y++)
                        System.Array.Copy(pixeles, (minY + y) * ancho + minX, recorte, y * w, w);
                    salida.SetPixels32(recorte); salida.Apply();
                    Directory.CreateDirectory(Path.GetDirectoryName(destino));
                    File.WriteAllBytes(destino, salida.EncodeToPNG());
                }
                finally { Object.DestroyImmediate(salida); }
                Importar(destino, true);
            }
            finally { Object.DestroyImmediate(textura); }
        }

        public static void Importar(string ruta, bool lectura)
        {
            AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(ruta) as TextureImporter;
            if (importer == null) throw new System.InvalidOperationException("No es un PNG importable.");
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            var ajustes = new TextureImporterSettings();
            importer.ReadTextureSettings(ajustes);
            ajustes.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(ajustes);
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.isReadable = lectura;
            importer.maxTextureSize = 2048;
            // Evita que una configuración de plataforma vuelva a comprimir estos PNG.
            importer.ClearPlatformTextureSettings("Standalone");
            importer.SaveAndReimport();
        }

        public static void CrearCapsula(string destino)
        {
            const int ancho = 1248, alto = 115;
            const float radio = alto / 2f;
            var textura = new Texture2D(ancho, alto, TextureFormat.RGBA32, false);
            try
            {
                var pixeles = new Color32[ancho * alto];
                for (int y = 0; y < alto; y++)
                for (int x = 0; x < ancho; x++)
                {
                    float centroX = Mathf.Clamp(x + 0.5f, radio, ancho - radio);
                    float dx = x + 0.5f - centroX, dy = y + 0.5f - radio;
                    pixeles[y * ancho + x] = dx * dx + dy * dy <= radio * radio
                        ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
                }
                textura.SetPixels32(pixeles); textura.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(destino));
                File.WriteAllBytes(destino, textura.EncodeToPNG());
            }
            finally { Object.DestroyImmediate(textura); }
            Importar(destino, true);
            var importer = (TextureImporter)AssetImporter.GetAtPath(destino);
            var factory = new UnityEditor.U2D.Sprites.SpriteDataProviderFactories();
            factory.Init();
            var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            if (provider == null) throw new System.InvalidOperationException("No existe editor de sprites para este importador.");
            provider.InitSpriteEditorDataProvider();
            var capacidad = provider.GetDataProvider<UnityEditor.U2D.Sprites.ISpriteFrameEditCapability>();
            if (capacidad == null || !capacidad.GetEditCapability().HasCapability(UnityEditor.U2D.Sprites.EEditCapability.EditBorder))
                throw new System.InvalidOperationException("El importador no admite editar los bordes del sprite.");
            var rectangulos = provider.GetSpriteRects();
            foreach (var rectangulo in rectangulos)
                rectangulo.border = new Vector4(57, 57, 57, 57);
            provider.SetSpriteRects(rectangulos);
            provider.Apply();
            importer.SaveAndReimport();
        }
    }
}
