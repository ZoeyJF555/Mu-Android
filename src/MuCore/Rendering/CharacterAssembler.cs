using Mu.Core.Formats.Bmd;

namespace Mu.Core.Rendering;

/// <summary>
/// Un personaje completo: un esqueleto con sus animaciones y las piezas
/// (cuerpo, armadura, casco...) que cuelgan de él. Cada pieza se ata al
/// esqueleto por el nombre de sus huesos, así que todas se mueven juntas.
/// </summary>
public sealed class CharacterModel
{
    /// <summary>El esqueleto que anima todas las piezas.</summary>
    public required BmdAnimator Animator { get; init; }

    /// <summary>Las mallas de todas las piezas, ya atadas al esqueleto.</summary>
    public required IReadOnlyList<SkinnedMesh> Meshes { get; init; }
}

/// <summary>
/// Junta un esqueleto y varias piezas en un solo personaje. El esqueleto
/// (por ejemplo <c>Player/player.bmd</c>) aporta las animaciones; cada pieza
/// (armadura, casco, botas...) aporta su geometría. La unión se hace por
/// nombre de hueso, que es como el cliente original reconoce las piezas.
/// </summary>
public static class CharacterAssembler
{
    /// <summary>Monta un personaje a partir del esqueleto y sus piezas.</summary>
    public static CharacterModel Assemble(BmdModel skeleton, IEnumerable<BmdModel> parts)
    {
        ArgumentNullException.ThrowIfNull(skeleton);
        ArgumentNullException.ThrowIfNull(parts);

        var skeletonIndex = BuildBoneIndex(skeleton);
        var meshes = new List<SkinnedMesh>();

        foreach (var part in parts)
        {
            var remap = BuildRemap(part, skeletonIndex);

            foreach (var mesh in SkinnedMeshBuilder.Build(part))
            {
                RemapBones(mesh, remap);
                meshes.Add(mesh);
            }
        }

        return new CharacterModel
        {
            Animator = new BmdAnimator(skeleton),
            Meshes = meshes,
        };
    }

    /// <summary>
    /// Cuelga un objeto (un arma, un escudo) de un hueso del esqueleto. Los
    /// vértices de un arma vienen en el espacio de su propio hueso, así que se
    /// atan al hueso de la mano: al animar el brazo, el arma lo sigue.
    /// </summary>
    /// <param name="skeleton">El esqueleto del personaje.</param>
    /// <param name="item">El modelo que se cuelga.</param>
    /// <param name="boneName">El hueso al que se ata, por ejemplo <c>Bip01 R Hand</c>.</param>
    public static IReadOnlyList<SkinnedMesh> Attach(BmdModel skeleton, BmdModel item, string boneName)
    {
        ArgumentNullException.ThrowIfNull(skeleton);
        ArgumentNullException.ThrowIfNull(item);

        var bone = FindBone(skeleton, boneName);
        if (bone < 0)
        {
            return [];
        }

        var meshes = SkinnedMeshBuilder.Build(item);
        foreach (var mesh in meshes)
        {
            for (var i = 0; i < mesh.Vertices.Length; i++)
            {
                mesh.Vertices[i].BoneIndex = bone;
            }
        }

        return meshes;
    }

    /// <summary>Índice de un hueso por su nombre, o -1 si no está.</summary>
    public static int FindBone(BmdModel skeleton, string name)
    {
        for (var i = 0; i < skeleton.Bones.Count; i++)
        {
            if (string.Equals(skeleton.Bones[i].Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private static Dictionary<string, int> BuildBoneIndex(BmdModel skeleton)
    {
        var index = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < skeleton.Bones.Count; i++)
        {
            index.TryAdd(skeleton.Bones[i].Name, i);
        }

        return index;
    }

    private static int[] BuildRemap(BmdModel part, Dictionary<string, int> skeletonIndex)
    {
        var remap = new int[part.Bones.Count];
        for (var i = 0; i < remap.Length; i++)
        {
            // Un hueso sin pareja se ata a la raíz para que al menos no se pierda.
            remap[i] = skeletonIndex.TryGetValue(part.Bones[i].Name, out var target) ? target : 0;
        }

        return remap;
    }

    private static void RemapBones(SkinnedMesh mesh, int[] remap)
    {
        if (remap.Length == 0)
        {
            return;
        }

        for (var i = 0; i < mesh.Vertices.Length; i++)
        {
            var bone = mesh.Vertices[i].BoneIndex;
            mesh.Vertices[i].BoneIndex = remap[Math.Clamp(bone, 0, remap.Length - 1)];
        }
    }
}
