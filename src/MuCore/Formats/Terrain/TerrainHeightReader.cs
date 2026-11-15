using Mu.Core.Formats.Texture;

namespace Mu.Core.Formats.Terrain;

/// <summary>
/// El mapa de alturas de un mundo (`World{n}/TerrainHeight.OZB`). Es un BMP en
/// escala de grises de 256 × 256: cada píxel es la altura de una casilla. La
/// variante extendida usa 24 bits BGR y un mínimo distinto.
/// </summary>
public static class TerrainHeightReader
{
    private const int ExtendedPixelsOffset = 4 + 14 + 40;
    private const float ExtendedMinHeight = -500f;

    /// <summary>Lee las alturas de un fichero OZB (en memoria).</summary>
    public static float[] Read(byte[] ozb)
    {
        // La variante extendida (24 bits) se reconoce por tamaño, como el original:
        // su bloque de píxeles es tres veces mayor que el de la variante indexada.
        if (ozb.Length >= ExtendedPixelsOffset + TerrainConstants.TileCount * 3)
        {
            return ReadExtended(ozb);
        }

        return ReadPaletted(ImageDecoder.Decode(ozb, "TerrainHeight.OZB"));
    }

    /// <summary>La variante de 8 bits: la escala de grises × factor.</summary>
    private static float[] ReadPaletted(DecodedImage image)
    {
        var heights = new float[image.Width * image.Height];
        for (var i = 0; i < heights.Length; i++)
        {
            // El decodificador entrega RGBA; en gris los tres canales coinciden.
            heights[i] = image.Pixels[i * 4] * TerrainConstants.HeightFactor;
        }

        return heights;
    }

    /// <summary>La variante de 24 bits BGR, con su propio mínimo.</summary>
    private static float[] ReadExtended(byte[] ozb)
    {
        var heights = new float[TerrainConstants.TileCount];
        for (var i = 0; i < heights.Length; i++)
        {
            var at = ExtendedPixelsOffset + i * 3;
            var height = ozb[at + 2] | (ozb[at + 1] << 8) | (ozb[at] << 16);
            heights[i] = (height + ExtendedMinHeight) / 100f;
        }

        return heights;
    }
}
