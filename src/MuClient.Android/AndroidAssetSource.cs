using Android.Content.Res;
using Mu.Core.Assets;

namespace Mu.Client.Android;

/// <summary>
/// Lee los recursos que van dentro del APK. En Android no son ficheros del
/// disco, sino entradas del paquete, y solo se pueden leer de una en una, así
/// que primero se pide la lista y luego se abre cada una por su nombre real.
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
        _index = new AssetIndex(EnumerateNames(_root));
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

        using var stream = _assets.Open(Combine(_root, name));
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    /// <inheritdoc />
    public bool Exists(string relativePath) => _index.Resolve(relativePath) is not null;

    /// <inheritdoc />
    public IEnumerable<string> Enumerate(string prefix) => _index.WithPrefix(prefix);

    /// <summary>Recorre los recursos de una carpeta, entrando en las subcarpetas.</summary>
    private IEnumerable<string> EnumerateNames(string folder)
    {
        var names = new List<string>();

        void Walk(string path, string relative)
        {
            var children = List(path);
            if (children is null)
            {
                return;
            }

            foreach (var child in children)
            {
                var childPath = Combine(path, child);
                var childRelative = relative.Length == 0 ? child : $"{relative}/{child}";
                if (IsFolder(path, child))
                {
                    Walk(childPath, childRelative);
                }
                else
                {
                    names.Add(childRelative);
                }
            }
        }

        Walk(folder, string.Empty);
        return names;
    }

    /// <summary>Lista una carpeta, o null si no existe o no se puede leer.</summary>
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

    /// <summary>
    /// Android no dice si una entrada es carpeta, así que se prueba a listarla:
    /// si responde, es carpeta, y si falla, es fichero.
    /// </summary>
    private bool IsFolder(string parent, string child) => List(Combine(parent, child)) is not null;

    private static string Combine(string left, string right) =>
        left.Length == 0 ? right : $"{left}/{right}";
}
