namespace Mu.Core.Formats.Terrain;

/// <summary>Constantes del terreno, iguales a las del cliente original.</summary>
public static class TerrainConstants
{
    /// <summary>El terreno es siempre de 256 × 256 casillas.</summary>
    public const int Size = 256;

    /// <summary>Lado total del terreno en casillas (256 × 256).</summary>
    public const int TileCount = Size * Size;

    /// <summary>Centímetros por casilla (el `TERRAIN_SCALE` del original).</summary>
    public const float TileCentimeters = 100f;

    /// <summary>Factor de altura de la variante de 8 bits.</summary>
    public const float HeightFactor = 1.5f / 100f;
}
