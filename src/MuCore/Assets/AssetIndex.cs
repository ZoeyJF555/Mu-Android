namespace Mu.Core.Assets;

/// <summary>
/// Traduce los nombres que pide el juego a los nombres reales de cada
/// plataforma. Los ficheros del cliente usan nombres de Windows y mayúsculas
/// mezcladas, y ni el disco de Linux ni los recursos dentro de un APK los
/// distinguen igual, así que la búsqueda se hace ignorando mayúsculas.
/// </summary>
public sealed class AssetIndex
{
    private readonly Dictionary<string, string> _names;

    /// <summary>Indexa los nombres disponibles, normalizados.</summary>
    public AssetIndex(IEnumerable<string> names)
    {
        ArgumentNullException.ThrowIfNull(names);

        _names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in names)
        {
            _names.TryAdd(Normalize(name), name);
        }
    }

    /// <summary>Nombres indexados, normalizados.</summary>
    public IReadOnlyCollection<string> Keys => _names.Keys;

    /// <summary>El nombre real que corresponde a uno pedido, o null si no hay.</summary>
    public string? Resolve(string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        return _names.TryGetValue(Normalize(relativePath), out var real) ? real : null;
    }

    /// <summary>Nombres indexados que empiezan por un prefijo, en orden.</summary>
    public IEnumerable<string> WithPrefix(string prefix)
    {
        var normalized = Normalize(prefix);
        return _names.Keys
            .Where(key => key.StartsWith(normalized, StringComparison.OrdinalIgnoreCase))
            .OrderBy(key => key, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Deja el nombre en la forma en que se indexa.</summary>
    public static string Normalize(string path) =>
        path.Replace('\\', '/').TrimStart('/');
}
