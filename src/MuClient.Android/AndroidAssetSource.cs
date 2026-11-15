using Android.Content.Res;
using Mu.Core.Assets;

namespace Mu.Client.Android;

/// <summary>
/// Lee los recursos que van dentro del APK. En Android no son ficheros del
/// disco, sino entradas del paquete, y solo se pueden leer de una en una, así
/// que primero se recorre el árbol y luego se abre cada una por su nombre real.
/// </summary>
public sealed class AndroidAssetSource : IAssetSource
{
    private readonly AssetManager _assets;
    private readonly string _root;
    private readonly AssetIndex _index;

    /// <param name="assets">El gestor de recursos del paquete.</param>
    /// <param name="root">Carpeta dentro de los recursos, normalmente <c>Data</c>.</param>
    public AndroidAssetSource(AssetManager assets, string root = "Data")
    {
        ArgumentNullException.ThrowIfNull(assets);
        ArgumentException.ThrowIfNullOrWhiteSpace(root);

        _assets = assets;
        _root = root.Trim('/');
        _index = new AssetIndex(AssetTree.Collect(_root, List));
    }

    /// <summary>Cuántos recursos se han encontrado.</summary>
    public int Count => _index.Keys.Count;

    /// <inheritdoc />
    public byte[]? Read(string relativePath)
    {
        var name = _index.Resolve(relativePath);
        if (name is null)
        {
            return null;
        }

        using var stream = _assets.Open($"{_root}/{name}");
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    /// <inheritdoc />
    public bool Exists(string relativePath) => _index.Resolve(relativePath) is not null;

    /// <inheritdoc />
    public IEnumerable<string> Enumerate(string prefix) => _index.WithPrefix(prefix);

    /// <summary>
    /// Lista una carpeta, o null si no se puede leer. Si la ruta es un fichero,
    /// Android puede devolver una lista vacía en vez de fallar, y de eso se
    /// encarga <see cref="AssetTree"/>.
    /// </summary>
    private string[]? List(string path)
    {
        try
        {
            return _assets.List(path);
        }
        catch (IOException)
        {
            return null;
        }
    }
}
