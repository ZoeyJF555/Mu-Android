using StbImageSharp;

namespace Mu.Core.Formats.Texture;

/// <summary>Imagen decodificada en memoria: píxeles RGBA de 8 bits, fila 0 arriba.</summary>
public sealed class DecodedImage
{
    public required int Width { get; init; }
    public required int Height { get; init; }

    /// <summary>Píxeles en RGBA (4 bytes por píxel, 4 × ancho × alto).</summary>
    public required byte[] Pixels { get; init; }
}

/// <summary>
/// Decodifica cualquier imagen que use el cliente. Los OZJ/OZT/OZB se abren
/// primero con <see cref="OzDecoder"/> y la imagen embebida la lee StbImageSharp,
/// que entiende JPEG, TGA, BMP y PNG y no depende de la plataforma.
/// </summary>
public static class ImageDecoder
{
    /// <summary>Decodifica un fichero del cliente (OZJ, OZT, OZB o imagen normal).</summary>
    public static DecodedImage DecodeFile(string path) => Decode(File.ReadAllBytes(path), path);

    /// <summary>Decodifica unos bytes ya leídos, usando el nombre para saber el contenedor.</summary>
    public static DecodedImage Decode(byte[] data, string path) =>
        DecodeEmbedded(OzDecoder.Unwrap(data, path), path);

    /// <summary>Decodifica una imagen ya sin cabecera de contenedor.</summary>
    public static DecodedImage DecodeEmbedded(ReadOnlyMemory<byte> image, string path)
    {
        try
        {
            using var stream = new MemoryStream(image.ToArray(), writable: false);
            var result = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
            return new DecodedImage
            {
                Width = result.Width,
                Height = result.Height,
                Pixels = result.Data,
            };
        }
        catch (Exception error) when (error is not InvalidDataException)
        {
            throw new InvalidDataException($"No se pudo decodificar la imagen '{path}'.", error);
        }
    }
}
