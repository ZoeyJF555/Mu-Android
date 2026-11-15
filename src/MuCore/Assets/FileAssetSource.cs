namespace Mu.Core.Assets;

/// <summary>
/// Un origen de recursos que lee de una carpeta del disco. Los ficheros del
/// juego usan nombres de Windows, así que la búsqueda es insensible a
/// mayúsculas también en Linux y Android.
/// </summary>
public sealed class FileAssetSource : IAssetSource
{
    private readonly AssetIndex _index;

    public FileAssetSource(string root)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        Root = Path.GetFullPath(root);
        _index = new AssetIndex(EnumerateNames());
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
    public IEnumerable<string> Enumerate(string prefix) => _index.WithPrefix(prefix);

    private string? Resolve(string relativePath)
    {
        var name = _index.Resolve(relativePath);
        return name is null ? null : Path.Combine(Root, name);
    }

    private IEnumerable<string> EnumerateNames()
    {
        if (!Directory.Exists(Root))
        {
            return [];
        }

        return Directory
            .EnumerateFiles(Root, "*", SearchOption.AllDirectories)
            .Select(file => AssetIndex.Normalize(Path.GetRelativePath(Root, file)));
    }
}
