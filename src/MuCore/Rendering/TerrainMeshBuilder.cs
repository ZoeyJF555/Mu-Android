using System.Numerics;
using Mu.Core.Formats.Terrain;

namespace Mu.Core.Rendering;

/// <summary>
/// La geometría del terreno, ya en el formato que espera el renderizador: una
/// rejilla de casillas con su altura, su normal, su color de luz y las
/// coordenadas de textura. Es lógica pura (sin GPU), así que se puede probar
/// sin pantalla.
/// </summary>
public sealed class TerrainMesh
{
    /// <summary>Un vértice por esquina de casilla.</summary>
    public required Vector3[] Positions { get; init; }

    /// <summary>La normal de cada vértice, para la iluminación.</summary>
    public required Vector3[] Normals { get; init; }

    /// <summary>El color de luz horneado de cada vértice.</summary>
    public required Vector3[] Colors { get; init; }

    /// <summary>Coordenadas de textura de cada vértice.</summary>
    public required Vector2[] TexCoords { get; init; }

    /// <summary>Índices de los triángulos (dos por casilla).</summary>
    public required int[] Indices { get; init; }
}

/// <summary>
/// Convierte el mapa de alturas, los atributos y la luz horneada del terreno
/// en una malla lista para dibujar. Las coordenadas de textura son la posición
/// dividida por el tamaño del terreno, de modo que una textura que cubra todo
/// el mapa encaja sola.
/// </summary>
public static class TerrainMeshBuilder
{
    /// <summary>Altura a la que se hunden las casillas sin suelo, para no verlas.</summary>
    private const float NoGroundHeight = -10000f;

    /// <summary>Construye la malla del terreno.</summary>
    /// <param name="heights">Altura por casilla.</param>
    /// <param name="attributes">Atributos por casilla (suelo, agua, ...).</param>
    /// <param name="light">Color de luz por casilla; si es null, se usa luz plena.</param>
    public static TerrainMesh Build(float[] heights, ushort[] attributes, Vector3[]? light = null)
    {
        var size = TerrainConstants.Size;
        var tiles = TerrainConstants.TileCount;
        var vertices = tiles * 4;

        var positions = new Vector3[vertices];
        var normals = new Vector3[vertices];
        var colors = new Vector3[vertices];
        var texCoords = new Vector2[vertices];
        var indices = new int[tiles * 6];

        var v = 0;
        var i = 0;

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var idx1 = Index(x, y);
                var idx2 = Index(x + 1, y);
                var idx3 = Index(x + 1, y + 1);
                var idx4 = Index(x, y + 1);

                var noGround = (attributes[idx1] & TerrainAttributeReader.FlagNoGround) != 0;
                positions[v] = new Vector3(x, noGround ? NoGroundHeight : heights[idx1], y);
                positions[v + 1] = new Vector3(x + 1, noGround ? NoGroundHeight : heights[idx2], y);
                positions[v + 2] = new Vector3(x + 1, noGround ? NoGroundHeight : heights[idx3], y + 1);
                positions[v + 3] = new Vector3(x, noGround ? NoGroundHeight : heights[idx4], y + 1);

                var normal = QuadNormal(positions[v], positions[v + 1], positions[v + 2], positions[v + 3]);
                normals[v] = normal;
                normals[v + 1] = normal;
                normals[v + 2] = normal;
                normals[v + 3] = normal;

                colors[v] = Light(light, idx1);
                colors[v + 1] = Light(light, idx2);
                colors[v + 2] = Light(light, idx3);
                colors[v + 3] = Light(light, idx4);

                texCoords[v] = new Vector2((float)x / size, (float)y / size);
                texCoords[v + 1] = new Vector2((float)(x + 1) / size, (float)y / size);
                texCoords[v + 2] = new Vector2((float)(x + 1) / size, (float)(y + 1) / size);
                texCoords[v + 3] = new Vector2((float)x / size, (float)(y + 1) / size);

                indices[i] = v;
                indices[i + 1] = v + 1;
                indices[i + 2] = v + 2;
                indices[i + 3] = v;
                indices[i + 4] = v + 2;
                indices[i + 5] = v + 3;

                v += 4;
                i += 6;
            }
        }

        return new TerrainMesh
        {
            Positions = positions,
            Normals = normals,
            Colors = colors,
            TexCoords = texCoords,
            Indices = indices,
        };
    }

    private static Vector3 Light(Vector3[]? light, int index) =>
        light is not null && index < light.Length ? light[index] : Vector3.One;

    private static int Index(int x, int y)
    {
        if (x >= TerrainConstants.Size)
        {
            x = TerrainConstants.Size - 1;
        }

        if (y >= TerrainConstants.Size)
        {
            y = TerrainConstants.Size - 1;
        }

        return (y * TerrainConstants.Size) + x;
    }

    private static Vector3 QuadNormal(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        var normal = Vector3.Cross(b - a, d - a) + Vector3.Cross(c - b, d - b);
        return normal.LengthSquared() < 1e-12f ? Vector3.UnitY : Vector3.Normalize(normal);
    }
}
