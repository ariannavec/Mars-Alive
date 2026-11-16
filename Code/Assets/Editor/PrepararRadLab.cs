using System.IO;
using UnityEngine;
using UnityEditor;

namespace RoverMars.UI.Editor
{
    public static class PrepararRadLab
    {
        // El procesador existente elimina RGB > 240, recorta y activa Read/Write.
        public static RectInt Procesar(string origen, string destino) => PrepararPlantas.Procesar(origen, destino);

        public static void CrearMarco(string destino, Color32 color, int grosor, bool fondoNegro)
        {
            const int lado = 64;
            var textura = new Texture2D(lado, lado, TextureFormat.RGBA32, false);
            try
            {
                var pixeles = new Color32[lado * lado];
                for (int y = 0; y < lado; y++)
                    for (int x = 0; x < lado; x++)
                    {
                        bool esquina = (x < 4 || x >= lado - 4) && (y < 4 || y >= lado - 4);
                        bool borde = x < grosor || y < grosor || x >= lado - grosor || y >= lado - grosor;
                        pixeles[y * lado + x] = esquina ? new Color32(0, 0, 0, 0) :
                            borde ? color : new Color32(0, 0, 0, fondoNegro ? (byte)255 : (byte)0);
                    }
                textura.SetPixels32(pixeles); textura.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(destino));
                File.WriteAllBytes(destino, textura.EncodeToPNG());
            }
            finally { Object.DestroyImmediate(textura); }
            PrepararPlantas.Importar(destino);
            var importer = (TextureImporter)AssetImporter.GetAtPath(destino);
            var fabrica = new UnityEditor.U2D.Sprites.SpriteDataProviderFactories(); fabrica.Init();
            var proveedor = fabrica.GetSpriteEditorDataProviderFromObject(importer);
            if (proveedor == null) throw new System.InvalidOperationException("No existe proveedor de sprites.");
            proveedor.InitSpriteEditorDataProvider();
            var capacidad = proveedor.GetDataProvider<UnityEditor.U2D.Sprites.ISpriteFrameEditCapability>();
            if (capacidad == null || !capacidad.GetEditCapability().HasCapability(UnityEditor.U2D.Sprites.EEditCapability.EditBorder))
                throw new System.InvalidOperationException("El importador no admite editar bordes.");
            var rectangulos = proveedor.GetSpriteRects();
            foreach (var rectangulo in rectangulos) rectangulo.border = Vector4.one * (grosor + 4);
            proveedor.SetSpriteRects(rectangulos); proveedor.Apply(); importer.SaveAndReimport();
        }
    }
}
