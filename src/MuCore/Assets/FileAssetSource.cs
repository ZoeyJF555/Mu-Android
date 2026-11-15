using System.Collections.Concurrent;

namespace Mu.Core.Assets;

/// <summary>
/// Un origen de recursos que lee de una carpeta del disco. Los ficheros del
/// juego usan nombres de Windows, así que la búsqueda es insensible a
/// mayúsculas también en Linux y Android.
/// </summary>
public sealed class FileAssetSource : IAssetSource
{
    private readonly ConcurrentDictionary<string, string> _resolved = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lazy<Dictionary<string, string>> _index;

    public FileAssetSource(string root)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        Root = Path.GetFullPath(root);
        _index = new Lazy<Dictionary<string, string>>(BuildIndex, isThreadSafe: true);
    }

    /// <summary>Directorio raíz de los datos del cliente.</summary>
    public string Root { get; }

    /// <inheritdoc />
    public byte[]? Read(string relativePath)
    {
        var path = Resolve(relativePath);
        return path is null ? null : File.ReadAllBytes(path);
    }

    /// <inheritdoc />
    public bool Exists(string relativePath) => Resolve(relativePath) is not null;

    /// <inheritdoc />
    public IEnumerable<string> Enumerate(string prefix)
    {
        var normalized = Normalize(prefix);
        return _index.Value.Keys
            .Where(key => key.StartsWith(normalized, StringComparison.OrdinalIgnoreCase))
            .OrderBy(key => key, StringComparer.OrdinalIgnoreCase);
    }

    private string? Resolve(string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

        var normalized = Normalize(relativePath);
        if (_resolved.TryGetValue(normalized, out var cached))
        {
            return cached;
        }

        var candidate = Path.Combine(Root, normalized);
        if (File.Exists(candidate))
        {
            _resolved[normalized] = candidate;
            return candidate;
        }

        return _index.Value.TryGetValue(normalized, out var indexed) ? indexed : null;
    }

    private Dictionary<string, string> BuildIndex()
    {
        var index = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!Directory.Exists(Root))
        {
            return index;
        }

        foreach (var file in Directory.EnumerateFiles(Root, "*", SearchOption.AllDirectories))
        {
            index.TryAdd(Normalize(Path.GetRelativePath(Root, file)), file);
        }

        return index;
    }

    private static string Normalize(string path) =>
        path.Replace('\\', '/').TrimStart('/');
}
