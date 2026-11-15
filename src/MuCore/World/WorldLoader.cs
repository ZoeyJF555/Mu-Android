using Mu.Core.Assets;
using Mu.Core.Formats.Bmd;
using Mu.Core.Formats.Terrain;
using Mu.Core.Formats.Texture;
using Mu.Core.Rendering;

namespace Mu.Core.World;

/// <summary>Un modelo de objeto con sus texturas ya cargadas.</summary>
public sealed class WorldModel
{
    /// <summary>La malla del objeto, con huesos y animaciones si las tiene.</summary>
    public required BmdModel Model { get; init; }

    /// <summary>La textura de cada ranura del modelo; null si no se encontró.</summary>
    public required DecodedImage?[] Textures { get; init; }
}

/// <summary>Todo lo que hace falta para dibujar un mundo.</summary>
public sealed class WorldAssets
{
    /// <summary>Número de mundo (1 = Lorencia).</summary>
    public required int World { get; init; }

    /// <summary>La geometría del terreno.</summary>
    public required TerrainMesh Terrain { get; init; }

    /// <summary>Altura del suelo de cada casilla, para apoyar a los personajes.</summary>
    public required float[] Heights { get; init; }

    /// <summary>La textura del suelo ya horneada.</summary>
    public required DecodedImage GroundTexture { get; init; }

    /// <summary>Los objetos colocados sobre el mapa.</summary>
    public required IReadOnlyList<TerrainObject> Objects { get; init; }

    /// <summary>El modelo de cada tipo de objeto, indexado por su tipo.</summary>
    public required IReadOnlyDictionary<int, WorldModel> ObjectModels { get; init; }
}

/// <summary>
/// Carga un mundo completo: el terreno, su textura de suelo, los objetos que
/// hay sobre él y el modelo de cada tipo de objeto. Todo se lee a través del
/// origen de recursos, así que funciona igual desde el disco que desde el APK.
/// </summary>
public static class WorldLoader
{
    /// <summary>Texturas de suelo que usa un mundo, en el orden de sus casillas.</summary>
    private static readonly string[] DefaultTiles =
    [
        "TileGrass01",
        "TileGrass02",
        "TileGround01",
        "TileGround02",
        "TileGround03",
        "TileWater01",
        "TileWood01",
        "TileRock01",
        "TileRock02",
    ];

    /// <summary>Carga el mundo indicado.</summary>
    /// <param name="source">De dónde leer los ficheros.</param>
    /// <param name="world">Número de mundo (1 = Lorencia).</param>
    public static WorldAssets Load(IAssetSource source, int world)
    {
        ArgumentNullException.ThrowIfNull(source);

        var folder = $"World{world}";

        var heights = TerrainHeightReader.Read(Require(source, $"{folder}/TerrainHeight.OZB"));
        var attributes = TerrainAttributeReader.Read(Require(source, $"{folder}/EncTerrain{world}.att"));
        var mapping = TerrainMappingReader.Read(Require(source, $"{folder}/EncTerrain{world}.map"));

        var lightImage = ImageDecoder.Decode(Require(source, $"{folder}/TerrainLight.OZJ"), "TerrainLight.OZJ");
        var light = TerrainLightBaker.Bake(lightImage, heights);

        var tiles = LoadTiles(source, folder);
        var ground = TerrainTextureBaker.Bake(tiles, mapping.Layer1, mapping.Layer2, mapping.Alpha);

        var objects = TerrainObjectReader.Read(Require(source, $"{folder}/EncTerrain{world}.obj"));
        var models = LoadObjectModels(source, world, objects);

        return new WorldAssets
        {
            World = world,
            Terrain = TerrainMeshBuilder.Build(heights, attributes, light),
            Heights = heights,
            GroundTexture = ground,
            Objects = objects,
            ObjectModels = models,
        };
    }

    /// <summary>Una textura de suelo se guarda como OZJ (JPEG) o, si no, OZT (TGA).</summary>
    private static List<DecodedImage> LoadTiles(IAssetSource source, string folder)
    {
        var tiles = new List<DecodedImage>(DefaultTiles.Length);
        foreach (var name in DefaultTiles)
        {
            var path = Pick(source, $"{folder}/{name}.OZJ", $"{folder}/{name}.OZT");
            tiles.Add(path is null ? Blank() : ImageDecoder.Decode(Require(source, path), path));
        }

        return tiles;
    }

    /// <summary>
    /// Carga el modelo de cada tipo de objeto distinto que dibuja algo, con la
    /// textura de cada una de sus ranuras.
    /// </summary>
    private static Dictionary<int, WorldModel> LoadObjectModels(
        IAssetSource source,
        int world,
        IReadOnlyList<TerrainObject> objects)
    {
        var textures = new ModelTextureResolver(source, $"Object{world}", "Item", "Player", $"World{world}");
        var models = new Dictionary<int, WorldModel>();

        foreach (var type in objects.Select(o => (int)o.Type).Distinct())
        {
            var name = ObjectModels.FileName(world, (short)type);
            if (name is null)
            {
                continue;
            }

            var path = $"Object{world}/{name}.bmd";
            if (!source.Exists(path))
            {
                continue;
            }

            try
            {
                var model = BmdReader.Read(Require(source, path), path);
                models[type] = new WorldModel
                {
                    Model = model,
                    Textures = textures.LoadAll(model.Meshes.Select(m => m.TexturePath)),
                };
            }
            catch (InvalidDataException)
            {
                // Un objeto que no se puede leer no debe impedir cargar el mapa.
            }
        }

        return models;
    }

    private static string? Pick(IAssetSource source, params string[] candidates) =>
        candidates.FirstOrDefault(source.Exists);

    private static byte[] Require(IAssetSource source, string path) =>
        source.Read(path) ?? throw new FileNotFoundException($"Falta el recurso '{path}'.");

    private static DecodedImage Blank()
    {
        var pixels = new byte[4];
        Array.Fill(pixels, (byte)255);
        return new DecodedImage { Width = 1, Height = 1, Pixels = pixels };
    }
}
