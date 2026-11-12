using System.Collections.Concurrent;

namespace Mu.Core.Assets;

/// <summary>
/// Resuelve rutas dentro del directorio de datos del cliente. Los ficheros del
/// juego se referencian con nombres de Windows, así que la búsqueda es
/// insensible a mayúsculas también en Linux y Android.
/// </summary>
public sealed class AssetStore
{
    private readonly ConcurrentDictionary<string, string> _resolved = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lazy<Dictionary<string, string>> _index;

    public AssetStore(string root)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        Root = Path.GetFullPath(root);
        _index = new Lazy<Dictionary<string, string>>(BuildIndex, isThreadSafe: true);
    }

    /// <summary>Directorio raíz de los datos del cliente.</summary>
    public string Root { get; }

    /// <summary>Devuelve la ruta real de un recurso o lanza si no existe.</summary>
    public string GetRequired(string relativePath)
    {
        if (TryResolve(relativePath, out var fullPath))
        {
            return fullPath;
        }

        throw new FileNotFoundException(
            $"No se encontró el recurso '{relativePath}' dentro de '{Root}'.", relativePath);
    }

    /// <summary>Intenta resolver un recurso sin lanzar excepción.</summary>
    public bool TryResolve(string relativePath, out string fullPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

        var normalized = Normalize(relativePath);
        if (_resolved.TryGetValue(normalized, out var cached))
        {
            fullPath = cached;
            return true;
        }

        var candidate = Path.Combine(Root, normalized);
        if (File.Exists(candidate))
        {
            fullPath = candidate;
            _resolved[normalized] = candidate;
            return true;
        }

        return _index.Value.TryGetValue(normalized, out fullPath!);
    }

    /// <summary>Abre un recurso para lectura.</summary>
    public Stream OpenRead(string relativePath) => File.OpenRead(GetRequired(relativePath));

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
