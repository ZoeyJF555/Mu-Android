using Mu.Core.Formats.Texture;

namespace Mu.Core.Assets;

/// <summary>
/// Busca y decodifica las texturas que nombran los modelos. Un modelo BMD guarda
/// el nombre de su textura sin carpeta y sin extensión, así que hay que probar
/// las carpetas donde el cliente las guarda y los dos formatos que usa: OZJ
/// (JPEG) y OZT (TGA).
/// </summary>
public sealed class ModelTextureResolver
{
    private readonly IAssetSource _source;
    private readonly string[] _folders;

    /// <summary>Crea un buscador que mira en las carpetas indicadas, en orden.</summary>
    public ModelTextureResolver(IAssetSource source, params string[] folders)
    {
        ArgumentNullException.ThrowIfNull(source);
        _source = source;
        _folders = folders.Length > 0 ? folders : ["Item"];
    }

    /// <summary>Devuelve la ruta del fichero de una textura, o null si no está.</summary>
    public string? Find(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var baseName = Path.GetFileNameWithoutExtension(name);
        foreach (var folder in _folders)
        {
            foreach (var extension in (string[])["OZJ", "OZT", "JPG", "PNG"])
            {
                var path = $"{folder}/{baseName}.{extension}";
                if (_source.Exists(path))
                {
                    return path;
                }
            }
        }

        return null;
    }

    /// <summary>Decodifica una textura, o devuelve null si no se encuentra o no se puede leer.</summary>
    public DecodedImage? Load(string name)
    {
        var path = Find(name);
        if (path is null)
        {
            return null;
        }

        try
        {
            return ImageDecoder.Decode(_source.Read(path)!, path);
        }
        catch (InvalidDataException)
        {
            // Una textura ilegible deja la ranura sin textura.
            return null;
        }
    }

    /// <summary>Decodifica la textura de cada malla de un modelo, en su orden.</summary>
    public DecodedImage?[] LoadAll(IEnumerable<string> names) =>
        names.Select(Load).ToArray();
}
