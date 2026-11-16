using System.IO;
using UnityEngine;
using UnityEditor;

namespace RoverMars.UI.Editor
{
    // Ejecución explícita: siempre crea una copia, nunca altera los originales.
    public static class PrepararPlantas
    {
        public static RectInt Procesar(string origen, string destino)
        {
            if (Path.GetFullPath(origen) == Path.GetFullPath(destino))
                throw new System.ArgumentException("El destino debe ser distinto del original.");
            var textura = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Texture2D salida = null;
            RectInt region;
            try
            {
                if (!ImageConversion.LoadImage(textura, File.ReadAllBytes(origen)))
                    throw new System.InvalidOperationException("No se puede leer " + origen);
                Color32[] pixeles = textura.GetPixels32();
                int ancho = textura.width, alto = textura.height;
                int minX = ancho, minY = alto, maxX = -1, maxY = -1;
                for (int y = 0; y < alto; y++)
                for (int x = 0; x < ancho; x++)
                {
                    int i = y * ancho + x;
                    Color32 p = pixeles[i];
                    // También elimina blancos interiores, tal como pide esta ilustración.
                    if (p.r > 240 && p.g > 240 && p.b > 240) p.a = 0;
                    pixeles[i] = p;
                    if (p.a == 0) continue;
                    minX = Mathf.Min(minX, x); minY = Mathf.Min(minY, y);
                    maxX = Mathf.Max(maxX, x); maxY = Mathf.Max(maxY, y);
                }
                if (maxX < minX) throw new System.InvalidOperationException("El dibujo está vacío.");
                int w = maxX - minX + 1, h = maxY - minY + 1;
                region = new RectInt(minX, minY, w, h);
                var recorte = new Color32[w * h];
                for (int y = 0; y < h; y++)
                    System.Array.Copy(pixeles, (minY + y) * ancho + minX, recorte, y * w, w);
                salida = new Texture2D(w, h, TextureFormat.RGBA32, false);
                salida.SetPixels32(recorte); salida.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(destino));
                File.WriteAllBytes(destino, salida.EncodeToPNG());
            }
            finally
            {
                Object.DestroyImmediate(textura);
                if (salida != null) Object.DestroyImmediate(salida);
            }
            Importar(destino);
            var importer = (TextureImporter)AssetImporter.GetAtPath(destino);
            var fabrica = new UnityEditor.U2D.Sprites.SpriteDataProviderFactories();
            fabrica.Init();
            var proveedor = fabrica.GetSpriteEditorDataProviderFromObject(importer);
            if (proveedor == null) throw new System.InvalidOperationException("Importador sin editor de sprites.");
            proveedor.InitSpriteEditorDataProvider();
            var capacidad = proveedor.GetDataProvider<UnityEditor.U2D.Sprites.ISpriteFrameEditCapability>();
            if (capacidad == null || !capacidad.GetEditCapability().HasCapability(UnityEditor.U2D.Sprites.EEditCapability.EditPivot))
                throw new System.InvalidOperationException("El importador no admite editar pivotes.");
            var rectangulos = proveedor.GetSpriteRects();
            foreach (var rectangulo in rectangulos)
            {
                rectangulo.alignment = SpriteAlignment.Custom;
                rectangulo.pivot = new Vector2(0.5f, 0f);
            }
            proveedor.SetSpriteRects(rectangulos); proveedor.Apply(); importer.SaveAndReimport();
            return region;
        }
        public static void Importar(string ruta)
        {
            AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(ruta) as TextureImporter;
            if (importer == null) throw new System.InvalidOperationException("No se puede importar " + ruta);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.isReadable = true; // Necesario para Alpha Hit Test.
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 2048;
            var ajustes = new TextureImporterSettings(); importer.ReadTextureSettings(ajustes);
            ajustes.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(ajustes);
            importer.ClearPlatformTextureSettings("Standalone");
            importer.SaveAndReimport();
        }
    }
}
