namespace Mu.Core.Formats.Terrain;

/// <summary>
/// El cifrado que llevan los ficheros de mapa del cliente. Todos los `.att` y
/// `.map` van cifrados con el mismo esquema: un XOR contra una clave de 16
/// bytes combinado con una clave que rueda byte a byte. Además, los primeros
/// bytes de la cabecera pasan por el "Bux" (XOR con una clave de 3 bytes).
/// </summary>
public static class MapFileEncryption
{
    private static readonly byte[] XorKey =
    [
        0xD1, 0x73, 0x52, 0xF6, 0xD2, 0x9A, 0xCB, 0x27,
        0x3E, 0xAF, 0x59, 0x31, 0x37, 0xB3, 0xE7, 0xA2,
    ];

    private static readonly byte[] BuxKey = [0xFC, 0xCF, 0xAB];

    /// <summary>Descifra <paramref name="source"/> y devuelve una copia en claro.</summary>
    public static byte[] Decrypt(ReadOnlySpan<byte> source)
    {
        var destination = new byte[source.Length];
        Decrypt(source, destination);
        return destination;
    }

    /// <summary>Descifra <paramref name="source"/> en <paramref name="destination"/>.</summary>
    public static void Decrypt(ReadOnlySpan<byte> source, Span<byte> destination)
    {
        if (destination.Length < source.Length)
        {
            throw new ArgumentException("El destino es más pequeño que el origen.", nameof(destination));
        }

        int rolling = 0x5E;
        for (var i = 0; i < source.Length; i++)
        {
            var cipher = source[i];
            destination[i] = (byte)(((cipher ^ XorKey[i % XorKey.Length]) - rolling) & 0xFF);
            rolling = (cipher + 0x3D) & 0xFF;
        }
    }

    /// <summary>Aplica el "Bux": XOR con la clave de 3 bytes, en sitio.</summary>
    public static void ApplyBux(Span<byte> buffer)
    {
        for (var i = 0; i < buffer.Length; i++)
        {
            buffer[i] ^= BuxKey[i % BuxKey.Length];
        }
    }

    /// <summary>
    /// Descifra y aplica el Bux sobre los primeros bytes, que es como se leen
    /// las cabeceras de los ficheros de mapa.
    /// </summary>
    public static byte[] DecryptAndBux(ReadOnlySpan<byte> source)
    {
        var plain = Decrypt(source);
        ApplyBux(plain);
        return plain;
    }
}
