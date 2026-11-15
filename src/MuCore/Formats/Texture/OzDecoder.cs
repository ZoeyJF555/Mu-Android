namespace Mu.Core.Formats.Texture;

/// <summary>
/// Los formatos de imagen del cliente (.OZJ, .OZT, .OZB) son contenedores: una
/// cabecera de pocos bytes delante de una imagen normal (JPEG, TGA o BMP).
/// Este decodificador quita la cabecera y devuelve la imagen embebida, que ya
/// sabe leer cualquier decodificador de imágenes estándar.
/// </summary>
public static class OzDecoder
{
    /// <summary>Cabecera de un OZJ (JPEG).</summary>
    public const int OzjHeaderSize = 24;

    /// <summary>Cabecera de un OZT (TGA).</summary>
    public const int OztHeaderSize = 4;

    /// <summary>Cabecera de un OZB (BMP).</summary>
    public const int OzbHeaderSize = 4;

    /// <summary>Quita la cabecera de un OZJ y devuelve el JPEG embebido.</summary>
    public static ReadOnlyMemory<byte> UnwrapJpeg(ReadOnlyMemory<byte> ozj)
    {
        var payload = Strip(ozj, OzjHeaderSize, "OZJ");
        if (payload.Length < 2 || payload.Span[0] != 0xFF || payload.Span[1] != 0xD8)
        {
            throw new InvalidDataException("El OZJ no contiene un JPEG válido tras la cabecera.");
        }

        return payload;
    }

    /// <summary>Quita la cabecera de un OZT y devuelve el TGA embebido.</summary>
    public static ReadOnlyMemory<byte> UnwrapTga(ReadOnlyMemory<byte> ozt) => Strip(ozt, OztHeaderSize, "OZT");

    /// <summary>Quita la cabecera de un OZB y devuelve el BMP embebido.</summary>
    public static ReadOnlyMemory<byte> UnwrapBmp(ReadOnlyMemory<byte> ozb)
    {
        var payload = Strip(ozb, OzbHeaderSize, "OZB");
        if (payload.Length < 2 || payload.Span[0] != (byte)'B' || payload.Span[1] != (byte)'M')
        {
            throw new InvalidDataException("El OZB no contiene un BMP válido tras la cabecera.");
        }

        return payload;
    }

    /// <summary>
    /// Quita la cabecera según la extensión del fichero. Es la puerta que usa
    /// el cliente: la extensión decide el formato, como en el original. Si el
    /// nombre no la trae, se mira el contenido.
    /// </summary>
    public static ReadOnlyMemory<byte> Unwrap(ReadOnlyMemory<byte> data, string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        return extension switch
        {
            ".ozj" => UnwrapJpeg(data),
            ".ozt" => UnwrapTga(data),
            ".ozb" => UnwrapBmp(data),
            ".jpg" or ".jpeg" or ".tga" or ".bmp" => data,
            "" => Detect(data),
            _ => throw new NotSupportedException($"Extensión de imagen no soportada: '{extension}'."),
        };
    }

    /// <summary>
    /// Adivina el contenedor mirando los primeros bytes: un JPEG empieza por
    /// FF D8 y un BMP por "BM"; cualquier otra cosa se trata como TGA.
    /// </summary>
    private static ReadOnlyMemory<byte> Detect(ReadOnlyMemory<byte> data)
    {
        var bytes = data.Span;
        for (var header = 4; header <= OzjHeaderSize; header += 4)
        {
            if (header <= bytes.Length &&
                bytes[header] == 0xFF &&
                bytes[header + 1] == 0xD8)
            {
                return UnwrapJpeg(data);
            }
        }

        for (var header = 4; header <= OzbHeaderSize; header += 4)
        {
            if (header <= bytes.Length &&
                bytes[header] == (byte)'B' &&
                bytes[header + 1] == (byte)'M')
            {
                return UnwrapBmp(data);
            }
        }

        return UnwrapTga(data);
    }

    private static ReadOnlyMemory<byte> Strip(ReadOnlyMemory<byte> data, int headerSize, string kind)
    {
        if (data.Length < headerSize)
        {
            throw new InvalidDataException($"El fichero {kind} es demasiado pequeño ({data.Length} bytes).");
        }

        return data[headerSize..];
    }
}
