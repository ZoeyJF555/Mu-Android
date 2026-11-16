namespace Mu.Core.Formats.Terrain;

/// <summary>
/// Los atributos del terreno (`World{n}/EncTerrain{n}.att`). Un valor por
/// casilla que dice si se puede andar, si hay agua, etc. El fichero va
/// cifrado y trae una cabecera de 4 bytes: versión, número de mapa, ancho y
/// alto.
/// </summary>
public static class TerrainAttributeReader
{
    /// <summary>Bandera de "no se puede andar" sobre el terreno.</summary>
    public const ushort FlagNoMove = 0x0004;

    /// <summary>Bandera de "no hay suelo" (agujeros del mapa).</summary>
    public const ushort FlagNoGround = 0x0008;

    /// <summary>
    /// Cierto si una casilla se puede pisar. No se puede si está marcada como
    /// "no se puede andar" o si es un agujero. Es lo que consultan los bots para
    /// rodear un obstáculo en vez de atravesarlo.
    /// </summary>
    /// <param name="attributes">Los atributos del mapa.</param>
    /// <param name="x">Casilla en X.</param>
    /// <param name="y">Casilla en Y.</param>
    public static bool IsWalkable(ushort[] attributes, int x, int y)
    {
        ArgumentNullException.ThrowIfNull(attributes);

        if (x < 0 || y < 0 || x >= TerrainConstants.Size || y >= TerrainConstants.Size)
        {
            return false;
        }

        var value = attributes[(y * TerrainConstants.Size) + x];
        return (value & (FlagNoMove | FlagNoGround)) == 0;
    }

    /// <summary>Lee los atributos de un fichero .att (en memoria).</summary>
    public static ushort[] Read(byte[] att)
    {
        var plain = MapFileEncryption.Decrypt(att);
        MapFileEncryption.ApplyBux(plain);

        var bytesPerTile = plain.Length switch
        {
            TerrainConstants.TileCount + 4 => 1,
            TerrainConstants.TileCount * 2 + 4 => 2,
            _ => throw new InvalidDataException(
                $"El fichero .att tiene un tamaño inesperado ({plain.Length} bytes)."),
        };

        if (plain[0] != 0 || plain[2] != 255 || plain[3] != 255)
        {
            throw new InvalidDataException("La cabecera del .att no es la esperada.");
        }

        var attributes = new ushort[TerrainConstants.TileCount];
        var payload = plain.AsSpan(4);

        if (bytesPerTile == 1)
        {
            for (var i = 0; i < attributes.Length; i++)
            {
                attributes[i] = payload[i];
            }
        }
        else
        {
            for (var i = 0; i < attributes.Length; i++)
            {
                attributes[i] = BitConverter.ToUInt16(payload.Slice(i * 2, 2));
            }
        }

        return attributes;
    }
}
