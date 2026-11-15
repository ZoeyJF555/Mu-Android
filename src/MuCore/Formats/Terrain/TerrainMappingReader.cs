namespace Mu.Core.Formats.Terrain;

/// <summary>
/// Las capas de textura del terreno (`World{n}/EncTerrain{n}.map`): qué textura
/// pinta cada casilla. Trae dos capas (una de base y otra encima) más un canal
/// de transparencia que mezcla las dos. El fichero va cifrado.
/// </summary>
public sealed class TerrainMapping
{
    /// <summary>Índice de la textura base por casilla (256 × 256).</summary>
    public required byte[] Layer1 { get; init; }

    /// <summary>Índice de la textura de encima por casilla (256 × 256).</summary>
    public required byte[] Layer2 { get; init; }

    /// <summary>Cobertura de la segunda capa (0 = solo la base, 255 = solo la de encima).</summary>
    public required byte[] Alpha { get; init; }
}

/// <summary>Lector de las capas de textura del terreno.</summary>
public static class TerrainMappingReader
{
    /// <summary>Lee las capas de un fichero .map (en memoria).</summary>
    public static TerrainMapping Read(byte[] map)
    {
        var plain = MapFileEncryption.Decrypt(map);

        // Cabecera de 2 bytes (versión y número de mapa) delante de las capas.
        const int header = 2;
        var expected = header + TerrainConstants.TileCount * 3;
        if (plain.Length != expected)
        {
            throw new InvalidDataException(
                $"El fichero .map tiene un tamaño inesperado ({plain.Length} bytes, se esperaban {expected}).");
        }

        return new TerrainMapping
        {
            Layer1 = plain[header..(header + TerrainConstants.TileCount)],
            Layer2 = plain[(header + TerrainConstants.TileCount)..(header + TerrainConstants.TileCount * 2)],
            Alpha = plain[(header + TerrainConstants.TileCount * 2)..(header + TerrainConstants.TileCount * 3)],
        };
    }
}
