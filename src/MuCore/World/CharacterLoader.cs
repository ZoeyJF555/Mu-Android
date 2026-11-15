using Mu.Core.Assets;
using Mu.Core.Formats.Bmd;
using Mu.Core.Formats.Texture;
using Mu.Core.Rendering;

namespace Mu.Core.World;

/// <summary>Un personaje cargado: su modelo animado y la textura de cada malla.</summary>
public sealed class CharacterAssets
{
    /// <summary>El esqueleto, las piezas y las animaciones, ya montados.</summary>
    public required CharacterModel Model { get; init; }

    /// <summary>La textura de cada malla de <see cref="CharacterModel.Meshes"/>, en su orden.</summary>
    public required DecodedImage?[] Textures { get; init; }
}

/// <summary>
/// Carga un personaje del cliente. El esqueleto y sus animaciones vienen de
/// <c>Player/player.bmd</c>; encima se visten las piezas de la clase, que el
/// cliente guarda como <c>HelmClassNN</c>, <c>ArmorClassNN</c> y compañía, con
/// <c>NN</c> el número de clase (01 el mago oscuro, 02 el caballero...).
/// </summary>
public static class CharacterLoader
{
    /// <summary>Las cinco piezas que viste una clase, en el orden en que se leen.</summary>
    private static readonly string[] PartPrefixes =
    [
        "HelmClass",
        "ArmorClass",
        "PantClass",
        "GloveClass",
        "BootClass",
    ];

    /// <summary>Esqueleto con las animaciones de cualquier personaje.</summary>
    private const string SkeletonPath = "Player/player.bmd";

    /// <summary>Carga el personaje de una clase con su equipo básico.</summary>
    /// <param name="source">De dónde leer los ficheros.</param>
    /// <param name="classNumber">Número de clase (1 = mago oscuro, 2 = caballero...).</param>
    public static CharacterAssets Load(IAssetSource source, int classNumber) =>
        Load(source, classNumber, weaponPath: null);

    /// <summary>Carga el personaje de una clase y, si se indica, un arma en la mano.</summary>
    /// <param name="source">De dónde leer los ficheros.</param>
    /// <param name="classNumber">Número de clase (1 = mago oscuro, 2 = caballero...).</param>
    /// <param name="weaponPath">Ruta del arma, por ejemplo <c>Item/Sword01.bmd</c>.</param>
    /// <param name="weaponBone">Hueso de la mano al que se ata el arma.</param>
    public static CharacterAssets Load(
        IAssetSource source,
        int classNumber,
        string? weaponPath,
        string weaponBone = "Bip01 R Hand")
    {
        ArgumentNullException.ThrowIfNull(source);

        var skeleton = BmdReader.Read(
            Require(source, SkeletonPath), SkeletonPath);

        var suffix = classNumber.ToString("00");
        var parts = new List<BmdModel>();
        foreach (var prefix in PartPrefixes)
        {
            var path = $"Player/{prefix}{suffix}.bmd";
            if (!source.Exists(path))
            {
                continue;
            }

            try
            {
                parts.Add(BmdReader.Read(Require(source, path), path));
            }
            catch (InvalidDataException)
            {
                // Una pieza ilegible no debe impedir cargar el personaje.
            }
        }

        var model = CharacterAssembler.Assemble(skeleton, parts);

        // El cuerpo desnudo (player.bmd) no trae mallas propias: la piel la
        // aportan las piezas. Si ninguna se pudo leer, el personaje sale vacío.
        var meshes = model.Meshes.ToList();
        if (weaponPath is not null && source.Exists(weaponPath))
        {
            try
            {
                var weapon = BmdReader.Read(Require(source, weaponPath), weaponPath);
                meshes.AddRange(CharacterAssembler.Attach(skeleton, weapon, weaponBone));
            }
            catch (InvalidDataException)
            {
                // Un arma ilegible deja al personaje sin arma.
            }
        }

        var textures = new ModelTextureResolver(source, "Player", "Item", "Monster", "NPC")
            .LoadAll(meshes.Select(m => m.TexturePath));

        return new CharacterAssets
        {
            Model = new CharacterModel
            {
                Animator = model.Animator,
                Meshes = meshes,
            },
            Textures = textures,
        };
    }

    private static byte[] Require(IAssetSource source, string path) =>
        source.Read(path) ?? throw new FileNotFoundException($"Falta el recurso '{path}'.");
}
