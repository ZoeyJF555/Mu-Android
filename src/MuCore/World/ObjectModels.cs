namespace Mu.Core.World;

/// <summary>
/// Qué modelo usa cada tipo de objeto en cada mundo. En el cliente original
/// esta tabla se arma al cargar el mapa; aquí se recoge la de Lorencia, la
/// primera que reproduce el cliente de referencia. La mayoría de tipos van en
/// tramos ("los trece árboles son Tree01..Tree13") y unos pocos son únicos.
/// </summary>
public static class ObjectModels
{
    private readonly record struct Range(short First, short Count, string Prefix);

    private static readonly Range[] LorenciaRanges =
    [
        new(0, 13, "Tree"),
        new(20, 8, "Grass"),
        new(30, 5, "Stone"),
        new(40, 3, "StoneStatue"),
        new(44, 3, "Tomb"),
        new(50, 2, "FireLight"),
        new(56, 2, "MerchantAnimal"),
        new(65, 3, "SteelWall"),
        new(69, 6, "StoneWall"),
        new(75, 4, "StoneMuWall"),
        new(81, 4, "Fence"),
        new(91, 3, "Cannon"),
        new(96, 2, "Sign"),
        new(98, 4, "Carriage"),
        new(102, 2, "Straw"),
        new(106, 4, "Well"),
        new(115, 5, "House"),
        new(121, 6, "HouseWall"),
        new(127, 3, "HouseEtc"),
        new(140, 7, "Furniture"),
        new(151, 3, "Beer"),
    ];

    private static readonly Dictionary<short, string> LorenciaSingles = new()
    {
        [43] = "SteelStatue01",
        [52] = "Bonfire01",
        [55] = "DoungeonGate01",
        [58] = "TreasureDrum01",
        [59] = "TreasureChest01",
        [60] = "Ship01",
        [68] = "SteelDoor01",
        [80] = "Bridge01",
        [85] = "BridgeStone01",
        [90] = "StreetLight01",
        [95] = "Curtain01",
        [105] = "Waterspout01",
        [110] = "Hanging01",
        [111] = "Stair01",
        [120] = "Tent01",
        [133] = "PoseBox01",
        [150] = "Candle01",
    };

    /// <summary>
    /// El fichero de modelo de un tipo de objeto (sin carpeta ni extensión), o
    /// null si ese tipo no dibuja nada.
    /// </summary>
    /// <remarks>
    /// Lorencia es la excepción: sus objetos tienen nombre propio. En el resto
    /// de mundos el fichero se llama <c>Object&lt;tipo+1&gt;</c>, que es la
    /// convención que sigue el cliente original.
    /// </remarks>
    public static string? FileName(int world, short type) =>
        world == 1 ? LorenciaName(type) : GenericName(type);

    private static string? GenericName(short type) =>
        type >= 0 ? $"Object{type + 1:00}" : null;

    private static string? LorenciaName(short type)
    {
        if (LorenciaSingles.TryGetValue(type, out var single))
        {
            return single;
        }

        foreach (var range in LorenciaRanges)
        {
            if (type >= range.First && type < range.First + range.Count)
            {
                return $"{range.Prefix}{type - range.First + 1:00}";
            }
        }

        return null;
    }
}
