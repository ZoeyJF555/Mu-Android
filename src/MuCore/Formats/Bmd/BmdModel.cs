using System.Numerics;

namespace Mu.Core.Formats.Bmd;

/// <summary>Modelo BMD ya decodificado, listo para el renderizador.</summary>
public sealed class BmdModel
{
    public required byte Version { get; init; }
    public required string Name { get; init; }
    public required IReadOnlyList<BmdMesh> Meshes { get; init; }
    public required IReadOnlyList<BmdBone> Bones { get; init; }
    public required IReadOnlyList<BmdAction> Actions { get; init; }
}

/// <summary>Una malla con su textura y su geometría.</summary>
public sealed class BmdMesh
{
    public required short TextureId { get; init; }
    public required string TexturePath { get; init; }
    public required IReadOnlyList<BmdVertex> Vertices { get; init; }
    public required IReadOnlyList<BmdNormal> Normals { get; init; }
    public required IReadOnlyList<Vector2> TexCoords { get; init; }
    public required IReadOnlyList<BmdTriangle> Triangles { get; init; }
}

/// <summary>Vértice con el hueso al que está ligado.</summary>
public readonly record struct BmdVertex(short Node, Vector3 Position);

/// <summary>Normal ligada a un vértice.</summary>
public readonly record struct BmdNormal(short Node, Vector3 Normal, short BindVertex);

/// <summary>Cara de la malla. Solo se usan las primeras <c>Polygon + 1</c> esquinas.</summary>
public sealed class BmdTriangle
{
    public required byte Polygon { get; init; }
    public required IReadOnlyList<short> VertexIndices { get; init; }
    public required IReadOnlyList<short> NormalIndices { get; init; }
    public required IReadOnlyList<short> TexCoordIndices { get; init; }
    public required short LightMapIndex { get; init; }

    /// <summary>Número real de esquinas de la cara (3 o 4).</summary>
    public int CornerCount => Polygon + 1;
}

/// <summary>Animación declarada en el fichero.</summary>
public sealed class BmdAction
{
    public required int AnimationKeys { get; init; }
    public required bool LockPositions { get; init; }
    public required IReadOnlyList<Vector3> Positions { get; init; }
}

/// <summary>Hueso con una pista de animación por acción.</summary>
public sealed class BmdBone
{
    public required string Name { get; init; }
    public required short Parent { get; init; }
    public required bool IsDummy { get; init; }
    public required IReadOnlyList<BmdBoneTrack> Tracks { get; init; }
}

/// <summary>Fotogramas de un hueso dentro de una acción.</summary>
public sealed class BmdBoneTrack
{
    public required IReadOnlyList<Vector3> Positions { get; init; }
    public required IReadOnlyList<Vector3> Rotations { get; init; }
}
