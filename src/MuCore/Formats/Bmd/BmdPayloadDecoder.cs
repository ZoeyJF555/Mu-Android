namespace Mu.Core.Formats.Bmd;

/// <summary>
/// Descifra el bloque de datos de los BMD que vienen cifrados. La cabecera
/// (4 bytes) siempre va en claro: "BMD" + byte de versión.
/// </summary>
internal static class BmdPayloadDecoder
{
    private static readonly byte[] RotationKey =
    [
        0xD1, 0x73, 0x52, 0xF6, 0xD2, 0x9A, 0xCB, 0x27,
        0x3E, 0xAF, 0x59, 0x31, 0x37, 0xB3, 0xE7, 0xA2,
    ];

    /// <summary>Versiones cuyo cuerpo viaja cifrado.</summary>
    public static bool IsEncrypted(byte version) => version is 12 or 15;

    /// <summary>
    /// Descifra <paramref name="payload"/> en sitio. El bloque descifrado se
    /// escribe a partir del byte 4 del buffer, justo donde estaba la longitud.
    /// </summary>
    public static void Decode(Span<byte> payload, byte version)
    {
        switch (version)
        {
            case 12:
                DecodeRolling(payload);
                break;
            case 15:
                throw new NotSupportedException(
                    "Los BMD v15 usan cifrado LEA y todavía no está implementado. " +
                    "Pendiente: módulo Mu.Core/Formats/Bmd/LeaCipher.cs.");
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(version), version, "Esta versión no usa cuerpo cifrado.");
        }
    }

    /// <summary>
    /// Esquema clásico: XOR contra una clave de 16 bytes y resta de una clave
    /// que se desplaza con cada byte cifrado.
    /// </summary>
    private static void DecodeRolling(Span<byte> payload)
    {
        ushort rolling = 0x5E;
        for (var i = 0; i < payload.Length; i++)
        {
            var cipher = payload[i];
            payload[i] = (byte)((cipher ^ RotationKey[i % RotationKey.Length]) - rolling);
            rolling = (ushort)((cipher + 0x3D) & 0xFF);
        }
    }
}
