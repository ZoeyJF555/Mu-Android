using System.Numerics;
using Mu.Core.Formats.Terrain;
using Mu.Core.Formats.Texture;

namespace Mu.Core.Rendering;

/// <summary>
/// Hornea la luz del terreno. El cliente guarda una imagen de luz
/// (`TerrainLight.OZJ`) que hay que combinar con la inclinación del terreno:
/// las zonas llanas reciben la luz entera y las cuestas menos. El resultado es
/// un color por casilla que se aplica a los vértices.
/// </summary>
public static class TerrainLightBaker
{
    /// <summary>Dirección de la luz, como en el cliente original (eje Z arriba).</summary>
    private static readonly Vector3 LightDirection = Vector3.Normalize(new Vector3(0.5f, -0.5f, 0.5f));

    /// <summary>Luz ambiente mínima, como en el cliente original (cero).</summary>
    public const float Ambient = 0f;

    /// <summary>
    /// Calcula el color de luz de cada casilla. <paramref name="light"/> es la
    /// imagen de luz ya decodificada y <paramref name="heights"/> las alturas.
    /// </summary>
    public static Vector3[] Bake(DecodedImage light, float[] heights)
    {
        var size = TerrainConstants.Size;
        var normals = ComputeNormals(heights);
        var colors = new Vector3[size * size];

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var index = (y * size) + x;
                var normal = normals[index];
                var luminosity = Math.Clamp(
                    (normal.X * LightDirection.X) +
                    (normal.Y * LightDirection.Y) +
                    (normal.Z * LightDirection.Z) + 0.5f,
                    0f,
                    1f);

                var lx = Math.Min(x, light.Width - 1);
                var ly = Math.Min(y, light.Height - 1);
                var at = ((ly * light.Width) + lx) * 4;

                colors[index] = new Vector3(
                    Math.Clamp(light.Pixels[at] / 255f, 0f, 1f) * luminosity,
                    Math.Clamp(light.Pixels[at + 1] / 255f, 0f, 1f) * luminosity,
                    Math.Clamp(light.Pixels[at + 2] / 255f, 0f, 1f) * luminosity);
            }
        }

        return colors;
    }

    /// <summary>
    /// La normal de cada casilla, sumando las dos caras del cuadrilátero que
    /// la tiene como esquina inferior. Es la misma cuenta que hace el cliente.
    /// </summary>
    private static Vector3[] ComputeNormals(float[] heights)
    {
        var size = TerrainConstants.Size;
        var normals = new Vector3[size * size];

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                // El cuadrilátero cuya esquina inferior izquierda es (x, y).
                var v1 = new Vector3(x + 1, y, HeightAt(heights, x + 1, y));
                var v2 = new Vector3(x + 1, y + 1, HeightAt(heights, x + 1, y + 1));
                var v3 = new Vector3(x, y + 1, HeightAt(heights, x, y + 1));
                var v4 = new Vector3(x, y, HeightAt(heights, x, y));

                normals[(y * size) + x] =
                    FaceNormal(v1, v2, v3) + FaceNormal(v3, v4, v1);
            }
        }

        return normals;
    }

    private static Vector3 FaceNormal(Vector3 a, Vector3 b, Vector3 c)
    {
        var normal = Vector3.Cross(b - a, c - a);
        return normal.LengthSquared() < 1e-12f ? Vector3.UnitZ : Vector3.Normalize(normal);
    }

    private static float HeightAt(float[] heights, int x, int y) =>
        heights[(Math.Min(y, TerrainConstants.Size - 1) * TerrainConstants.Size) +
                Math.Min(x, TerrainConstants.Size - 1)];
}
