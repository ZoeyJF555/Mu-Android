using System.Numerics;
using Mu.Core.Formats.Terrain;
using Mu.Core.Formats.Texture;

namespace Mu.Core.Rendering;

/// <summary>
/// Hornea la textura del suelo del terreno. El mapa dice, casilla a casilla,
/// qué textura le toca (hierba, tierra, piedra...). En vez de dibujar cada
/// casilla con su textura, se juntan todas en una sola imagen del tamaño del
/// mapa, muestreando cada textura a la resolución del terreno. El resultado se
/// aplica al mapa entero con una sola textura y las coordenadas de textura de
/// la malla (posición / 256) lo colocan solo.
/// </summary>
public static class TerrainTextureBaker
{
    /// <summary>Resolución de la textura horneada: una muestra por casilla.</summary>
    public const int Resolution = TerrainConstants.Size;

    /// <summary>
    /// Construye la textura del suelo. <paramref name="tiles"/> es la imagen de
    /// cada textura disponible; <paramref name="layer1"/> dice cuál usar en
    /// cada casilla; <paramref name="layer2"/> y <paramref name="alpha"/> son
    /// la capa superior y su mezcla.
    /// </summary>
    public static DecodedImage Bake(
        IReadOnlyList<DecodedImage> tiles,
        byte[] layer1,
        byte[] layer2,
        byte[] alpha)
    {
        ArgumentNullException.ThrowIfNull(tiles);
        var pixels = new byte[Resolution * Resolution * 4];

        for (var y = 0; y < Resolution; y++)
        {
            for (var x = 0; x < Resolution; x++)
            {
                var index = (y * TerrainConstants.Size) + x;
                var blend = index < alpha.Length ? alpha[index] : (byte)0;

                var bottom = Sample(tiles, index < layer1.Length ? layer1[index] : 0, x, y);
                var top = blend > 0 && index < layer2.Length
                    ? Sample(tiles, layer2[index], x, y)
                    : bottom;

                var at = index * 4;
                pixels[at] = Mix(bottom.X, top.X, blend);
                pixels[at + 1] = Mix(bottom.Y, top.Y, blend);
                pixels[at + 2] = Mix(bottom.Z, top.Z, blend);
                pixels[at + 3] = 255;
            }
        }

        return new DecodedImage
        {
            Width = Resolution,
            Height = Resolution,
            Pixels = pixels,
        };
    }

    private static byte Mix(float bottom, float top, byte blend) =>
        (byte)Math.Clamp(bottom + ((top - bottom) * (blend / 255f)), 0f, 255f);

    /// <summary>
    /// Toma un píxel de una textura. El original dibuja cada textura repetida
    /// sobre el mapa: su lado se reparte en trozos de un cuarto de su tamaño en
    /// coordenadas de textura, es decir, una textura de 256 px cubre 4 × 4
    /// casillas. La casilla (x, y) cae por tanto en el píxel
    /// <c>(x / 256) * (lado / 4) * lado</c>.
    /// </summary>
    private static Vector3 Sample(IReadOnlyList<DecodedImage> tiles, int tile, int x, int y)
    {
        if (tile < 0 || tile >= tiles.Count || tiles[tile].Width == 0 || tiles[tile].Height == 0)
        {
            return Vector3.One;
        }

        var image = tiles[tile];
        var tx = Texel(x, image.Width);
        var ty = Texel(y, image.Height);
        var at = ((ty * image.Width) + tx) * 4;

        return new Vector3(image.Pixels[at], image.Pixels[at + 1], image.Pixels[at + 2]);
    }

    private static int Texel(int tile, int size)
    {
        // La textura se repite `size / 4` veces a lo largo del mapa.
        var u = tile / (float)TerrainConstants.Size * (size / 4f);
        return (int)((u - MathF.Floor(u)) * size);
    }
}
