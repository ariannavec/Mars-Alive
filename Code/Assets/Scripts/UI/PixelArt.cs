using UnityEngine;

namespace RoverMars.UI
{
    public static class PixelArt
    {
        public static Sprite CrearRegadera()
        {
            return Dibujar("PLACEHOLDER_Regadera", new[]
            {
                "................", "....####........", "...#....#.......", "...#....#.......",
                "...######.......", "..#cccccc#......", ".##chhccc#..##..", "#.#chcccc###cc#.",
                "#.#cccccc#cc###.", "#.#cccccc###....", ".##cccccc#......", "..#cccccc#......",
                "..#cccccc#......", "...######.......", "................", "................"
            });
        }

        public static Sprite CrearPala()
        {
            return Dibujar("PLACEHOLDER_Pala", new[]
            {
                ".....#####......", ".....#bbb#......", ".....#...#......", ".....#####......",
                ".......b........", ".......b........", ".......b........", ".......b........",
                ".......b........", ".....#####......", "....#mmhmm#.....", "....#mmhmm#.....",
                "....#mmmmm#.....", ".....#mmm#......", "......###.......", "................"
            });
        }

        public static Sprite CrearMarco(bool seleccionado)
        {
            Color32[] pixels = new Color32[256];
            Color32 borde = seleccionado ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 255);
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                    pixels[y * 16 + x] = x == 0 || y == 0 || x == 15 || y == 15
                        ? borde : new Color32(35, 38, 44, 255);
            return Crear(seleccionado ? "MarcoSeleccionado" : "MarcoNormal", 16, pixels, new Vector4(1, 1, 1, 1));
        }

        public static Sprite CrearParticula(Color32 color)
        {
            return Crear("ParticulaPixel", 1, new[] { color }, Vector4.zero);
        }

        public static Sprite CrearCandado()
        {
            return Dibujar("Candado8bit", new[]
            {
                "................", "................", "....KKKKKKKK....", "...KGGGGGGGGK...",
                "...KG......GK...", "...KG......GK...", "...KG......GK...", "..KKKKKKKKKKKK..",
                "..KGGGGGGGGGGK..", "..KGGGGGGGGGGK..", "..KGGGDDDDGGGK..", "..KGGGDKKDGGGK..",
                "..KGGGGDDGGGGK..", "..KGGGGDDGGGGK..", "..KDDDDDDDDDDK..", "..KKKKKKKKKKKK.."
            });
        }

        public static Sprite CrearInteriorDormitorio()
        {
            // El dormitorio ocupa una L: el hueco superior derecho no es habitación.
            var pixels = new Color32[256];
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                    pixels[y * 16 + x] = y < 9 || x < 6 ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
            return Crear("InteriorDormitorio", 16, pixels, Vector4.zero);
        }

        // Marco 9-slice: 4 px de grosor y esquinas cortadas, sin suavizado.
        public static Sprite CrearTrackBarra()
        {
            const int lado = 24;
            var pixels = new Color32[lado * lado];
            for (int y = 0; y < lado; y++)
                for (int x = 0; x < lado; x++)
                {
                    if (!DentroBarra(x, y, lado, lado, 0)) continue;
                    pixels[y * lado + x] = DentroBarra(x, y, lado, lado, 4)
                        ? new Color32(51, 51, 51, 255) : new Color32(0, 0, 0, 255);
                }
            return Crear("TrackBarra8bit", lado, pixels, new Vector4(8, 8, 8, 8));
        }

        public static Sprite CrearRellenoBarra(string nombre, Color color, int ancho, int alto)
        {
            // Filled ignora el 9-slice: usamos la medida del rectángulo para
            // mantener las franjas de 4 px y el margen transparente del marco.
            ancho = Mathf.Max(20, ancho);
            alto = Mathf.Max(20, alto);
            var pixels = new Color32[ancho * alto];
            Color brillo = Color.Lerp(color, Color.white, 0.3f);
            Color sombra = Color.Lerp(color, Color.black, 0.3f);
            for (int y = 0; y < alto; y++)
                for (int x = 0; x < ancho; x++)
                {
                    if (!DentroBarra(x, y, ancho, alto, 4)) continue;
                    pixels[y * ancho + x] = y >= alto - 8 ? brillo : y < 8 ? sombra : color;
                }
            var texture = new Texture2D(ancho, alto, TextureFormat.RGBA32, false);
            texture.name = nombre + "_Texture";
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels32(pixels);
            texture.Apply(false, false); // RGBA32 sin compresión ni mipmaps.
            var sprite = Sprite.Create(texture, new Rect(0, 0, ancho, alto),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(8, 8, 8, 8));
            sprite.name = nombre;
            return sprite;
        }

        private static bool DentroBarra(int x, int y, int ancho, int alto, int margen)
        {
            if (x < margen || x >= ancho - margen || y < margen || y >= alto - margen)
                return false;
            // Cada corte es un bloque de 4 x 4 px, no una curva.
            return !((x < margen + 4 || x >= ancho - margen - 4) &&
                     (y < margen + 4 || y >= alto - margen - 4));
        }

        private static Sprite Dibujar(string nombre, string[] filas)
{
            Color32[] pixels = new Color32[256];
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                {
                    char simbolo = x < filas[y].Length ? filas[y][x] : '.';
                    Color32 color;
                    switch (simbolo)
                    {
                        case '#': color = new Color32(0, 0, 0, 255); break;
                        case 'c': color = new Color32(66, 166, 191, 255); break;
                        case 'h': color = new Color32(218, 246, 255, 255); break;
                        case 'm': color = new Color32(148, 163, 177, 255); break;
                        case 'b': color = new Color32(154, 96, 47, 255); break;
                        case 'K': color = new Color32(0, 0, 0, 255); break;
                        case 'G': color = new Color32(154, 154, 154, 255); break;
                        case 'D': color = new Color32(94, 94, 94, 255); break;
                        default: color = new Color32(0, 0, 0, 0); break;
                    }
                    pixels[(15 - y) * 16 + x] = color;
                }
            return Crear(nombre, 16, pixels, Vector4.zero);
        }

        private static Sprite Crear(string nombre, int lado, Color32[] pixels, Vector4 borde)
        {
            Texture2D texture = new Texture2D(lado, lado, TextureFormat.RGBA32, false);
            texture.name = nombre + "_Texture";
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, lado, lado), new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect, borde);
            sprite.name = nombre;
            return sprite;
        }

        // Solo llamar para recursos creados por esta fábrica, nunca para assets del usuario.
        public static void Liberar(Sprite sprite)
        {
            if (sprite == null)
                return;
            Object.Destroy(sprite.texture);
            Object.Destroy(sprite);
        }
    }
}
