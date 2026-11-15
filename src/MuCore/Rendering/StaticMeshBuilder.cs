using System.Numerics;
using Mu.Core.Formats.Bmd;

namespace Mu.Core.Rendering;

/// <summary>
/// Una malla ya colocada en su sitio: sin huesos ni animación, lista para
/// dibujar. Es lo que se usa con los objetos del mapa, que no se mueven.
/// </summary>
public sealed class StaticMesh
{
    /// <summary>Posiciones en el espacio del modelo (en centímetros).</summary>
    public required Vector3[] Positions { get; init; }

    /// <summary>Normales en el espacio del modelo.</summary>
    public required Vector3[] Normals { get; init; }

    /// <summary>Coordenadas de textura.</summary>
    public required Vector2[] TexCoords { get; init; }

    /// <summary>Índices de los triángulos.</summary>
    public required int[] Indices { get; init; }

    /// <summary>Nombre del fichero de textura del modelo.</summary>
    public required string TexturePath { get; init; }
}

/// <summary>
/// Convierte un modelo BMD en mallas estáticas aplicando la pose de reposo. Los
/// vértices de un BMD vienen en el espacio de su hueso, así que se multiplican
/// por la matriz de mundo del hueso en reposo; como en reposo casi todos los
/// huesos son la identidad, la mayoría de los vértices quedan donde están.
/// </summary>
public static class StaticMeshBuilder
{
    /// <summary>Construye las mallas de un modelo, saltando las vacías.</summary>
    public static List<StaticMesh> Build(BmdModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var pose = new BmdAnimator(model).Pose(0, 0f);
        var meshes = new List<StaticMesh>(model.Meshes.Count);

        foreach (var mesh in model.Meshes)
        {
            if (mesh.Vertices.Count == 0 || mesh.Triangles.Count == 0)
            {
                continue;
            }

            var positions = new List<Vector3>();
            var normals = new List<Vector3>();
            var texCoords = new List<Vector2>();
            var indices = new List<int>();

            foreach (var triangle in mesh.Triangles)
            {
                if (triangle.CornerCount < 3)
                {
                    continue;
                }

                AddCorner(positions, normals, texCoords, indices, mesh, triangle, pose, 0);
                AddCorner(positions, normals, texCoords, indices, mesh, triangle, pose, 1);
                AddCorner(positions, normals, texCoords, indices, mesh, triangle, pose, 2);

                if (triangle.CornerCount >= 4)
                {
                    AddCorner(positions, normals, texCoords, indices, mesh, triangle, pose, 0);
                    AddCorner(positions, normals, texCoords, indices, mesh, triangle, pose, 2);
                    AddCorner(positions, normals, texCoords, indices, mesh, triangle, pose, 3);
                }
            }

            if (indices.Count == 0)
            {
                continue;
            }

            meshes.Add(new StaticMesh
            {
                Positions = [.. positions],
                Normals = [.. normals],
                TexCoords = [.. texCoords],
                Indices = [.. indices],
                TexturePath = mesh.TexturePath,
            });
        }

        return meshes;
    }

    private static void AddCorner(
        List<Vector3> positions,
        List<Vector3> normals,
        List<Vector2> texCoords,
        List<int> indices,
        BmdMesh mesh,
        BmdTriangle triangle,
        SkeletonPose pose,
        int corner)
    {
        var vertex = mesh.Vertices[triangle.VertexIndices[corner]];
        var bone = BoneMatrix(pose, vertex.Node);

        var normal = triangle.NormalIndices[corner] < mesh.Normals.Count
            ? mesh.Normals[triangle.NormalIndices[corner]].Normal
            : Vector3.UnitY;
        var texCoord = triangle.TexCoordIndices[corner] < mesh.TexCoords.Count
            ? mesh.TexCoords[triangle.TexCoordIndices[corner]]
            : Vector2.Zero;

        indices.Add(positions.Count);
        positions.Add(Vector3.Transform(vertex.Position, bone));
        normals.Add(Vector3.TransformNormal(normal, bone));
        texCoords.Add(texCoord);
    }

    private static Matrix4x4 BoneMatrix(SkeletonPose pose, short bone) =>
        bone >= 0 && bone < pose.BoneMatrices.Length
            ? pose.BoneMatrices[bone]
            : Matrix4x4.Identity;
}
