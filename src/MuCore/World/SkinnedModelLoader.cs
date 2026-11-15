using Mu.Core.Assets;
using Mu.Core.Formats.Bmd;
using Mu.Core.Formats.Texture;
using Mu.Core.Rendering;

namespace Mu.Core.World;

/// <summary>Un modelo animado con la textura de cada una de sus mallas.</summary>
public sealed class SkinnedModelAssets
{
    /// <summary>El modelo, ya con sus mallas listas para animar.</summary>
    public required CharacterModel Model { get; init; }

    /// <summary>La textura de cada malla de <see cref="CharacterModel.Meshes"/>, en su orden.</summary>
    public required DecodedImage?[] Textures { get; init; }
}

/// <summary>
/// Carga cualquier modelo animado del cliente a partir de un solo fichero:
/// monstruos, NPC y armas. Todos comparten el esqueleto <c>Bip01</c> del
/// personaje, así que se cargan igual que él: se lee el modelo y se le buscan
/// sus texturas.
/// </summary>
public static class SkinnedModelLoader
{
    /// <summary>Carpetas donde el cliente guarda las texturas de los modelos.</summary>
    private static readonly string[] TextureFolders = ["Monster", "NPC", "Item", "Player"];

    /// <summary>Carga un modelo animado y sus texturas.</summary>
    /// <param name="source">De dónde leer los ficheros.</param>
    /// <param name="path">Ruta del modelo, por ejemplo <c>Monster/Monster01.bmd</c>.</param>
    public static SkinnedModelAssets Load(IAssetSource source, string path)
    {
        ArgumentNullException.ThrowIfNull(source);

        var model = BmdReader.Read(
            source.Read(path) ?? throw new FileNotFoundException($"Falta el recurso '{path}'."), path);

        // Un monstruo trae su geometría en el propio fichero, así que sus mallas
        // se construyen directamente, sin buscar piezas por nombre de hueso.
        var meshes = SkinnedMeshBuilder.Build(model);
        var textures = new ModelTextureResolver(source, TextureFolders)
            .LoadAll(meshes.Select(m => m.TexturePath));

        return new SkinnedModelAssets
        {
            Model = new CharacterModel
            {
                Animator = new BmdAnimator(model),
                Meshes = meshes,
            },
            Textures = textures,
        };
    }
}
