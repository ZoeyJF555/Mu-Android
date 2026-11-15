using Mu.Core.Formats.Terrain;

namespace Mu.Core.Rendering;

/// <summary>
/// Consulta la altura del suelo en un punto cualquiera del mapa, interpolando
/// entre las cuatro casillas que lo rodean. Es lo que hace el cliente original
/// para apoyar a los personajes sobre el terreno en vez de dejarlos flotando
/// o hundidos.
/// </summary>
public static class TerrainHeightSampler
{
    /// <summary>Altura del terreno en un punto, en unidades del mundo.</summary>
    /// <param name="heights">Altura de cada casilla, como las da el lector de OZB.</param>
    /// <param name="x">Coordenada X del mundo, en casillas.</param>
    /// <param name="y">Coordenada Y (el eje Z del mundo) en casillas.</param>
    public static float Sample(float[] heights, float x, float y)
    {
        ArgumentNullException.ThrowIfNull(heights);

        var xi = (int)MathF.Floor(x);
        var yi = (int)MathF.Floor(y);
        var xd = x - xi;
        var yd = y - yi;

        var h1 = At(heights, xi, yi);
        var h2 = At(heights, xi + 1, yi);
        var h3 = At(heights, xi + 1, yi + 1);
        var h4 = At(heights, xi, yi + 1);

        return ((1f - xd) * (1f - yd) * h1)
             + (xd * (1f - yd) * h2)
             + (xd * yd * h3)
             + ((1f - xd) * yd * h4);
    }

    private static float At(float[] heights, int x, int y)
    {
        x = Math.Clamp(x, 0, TerrainConstants.Size - 1);
        y = Math.Clamp(y, 0, TerrainConstants.Size - 1);
        return heights[(y * TerrainConstants.Size) + x];
    }
}
