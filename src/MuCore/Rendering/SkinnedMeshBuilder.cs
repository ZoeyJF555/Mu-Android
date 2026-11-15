using System.Numerics;
using Mu.Core.Formats.Bmd;

namespace Mu.Core.Rendering;

/// <summary>Un vértice de un modelo BMD, ya listo para dibujar.</summary>
public struct SkinnedVertex
{
    /// <summary>Posición relativa al hueso al que está ligado (en centímetros).</summary>
    public Vector3 Position;

    /// <summary>Normal en el espacio del modelo.</summary>
    public Vector3 Normal;

    /// <summary>Coordenadas de textura.</summary>
    public Vector2 TexCoord;

    /// <summary>Hueso que mueve este vértice.</summary>
    public int BoneIndex;
}

/// <summary>Una malla del modelo, ya triangulada y con su textura.</summary>
public sealed class SkinnedMesh
{
    /// <summary>Nombre del fichero de textura (tal cual aparece en el BMD).</summary>
    public required string TexturePath { get; init; }

    /// <summary>Vértices, en el orden en que los consumen los índices.</summary>
    public required SkinnedVertex[] Vertices { get; init; }

    /// <summary>Índices de los triángulos.</summary>
    public required int[] Indices { get; init; }
}

/// <summary>
/// Convierte un modelo BMD en mallas trianguladas. Los triángulos de cuatro
/// esquinas se parten en dos, que es lo que entiende la tarjeta gráfica.
/// </summary>
public static class SkinnedMeshBuilder
{
    /// <summary>Construye las mallas de un modelo, saltando las que estén vacías.</summary>
    public static List<SkinnedMesh> Build(BmdModel model)
    {
        var meshes = new List<SkinnedMesh>(model.Meshes.Count);
        var boneCount = model.Bones.Count;

        foreach (var mesh in model.Meshes)
        {
            if (mesh.Vertices.Count == 0 || mesh.Triangles.Count == 0)
            {
                continue;
            }

            var vertices = new List<SkinnedVertex>();
            var indices = new List<int>();

            foreach (var triangle in mesh.Triangles)
            {
                var corners = triangle.CornerCount;
                if (corners < 3)
                {
                    continue;
                }

                // Un cuadrilátero se parte en dos triángulos: (0,1,2) y (0,2,3).
                AddTriangle(vertices, indices, mesh, triangle, boneCount, 0, 1, 2);
                if (corners >= 4)
                {
                    AddTriangle(vertices, indices, mesh, triangle, boneCount, 0, 2, 3);
                }
            }

            if (indices.Count == 0)
            {
                continue;
            }

            meshes.Add(new SkinnedMesh
            {
                TexturePath = mesh.TexturePath,
                Vertices = [.. vertices],
                Indices = [.. indices],
            });
        }

        return meshes;
    }

    private static void AddTriangle(
        List<SkinnedVertex> vertices,
        List<int> indices,
        BmdMesh mesh,
        BmdTriangle triangle,
        int boneCount,
        int a,
        int b,
        int c)
    {
        indices.Add(AddCorner(vertices, mesh, triangle, boneCount, a));
        indices.Add(AddCorner(vertices, mesh, triangle, boneCount, b));
        indices.Add(AddCorner(vertices, mesh, triangle, boneCount, c));
    }

    private static int AddCorner(
        List<SkinnedVertex> vertices,
        BmdMesh mesh,
        BmdTriangle triangle,
        int boneCount,
        int corner)
    {
        var vertex = mesh.Vertices[triangle.VertexIndices[corner]];
        var normal = triangle.NormalIndices[corner] < mesh.Normals.Count
            ? mesh.Normals[triangle.NormalIndices[corner]].Normal
            : Vector3.UnitY;
        var texCoord = triangle.TexCoordIndices[corner] < mesh.TexCoords.Count
            ? mesh.TexCoords[triangle.TexCoordIndices[corner]]
            : Vector2.Zero;

        vertices.Add(new SkinnedVertex
        {
            Position = vertex.Position,
            Normal = normal,
            TexCoord = texCoord,
            BoneIndex = Math.Clamp((int)vertex.Node, 0, Math.Max(0, boneCount - 1)),
        });

        return vertices.Count - 1;
    }
}
