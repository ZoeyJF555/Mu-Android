using System.Security.Cryptography;
using System.Text;

namespace Mu.Core.Game;

/// <summary>
/// Comprueba la contraseña de la cuenta local. No se guarda la contraseña, sino
/// su huella con una sal, que es lo mínimo para no dejarla escrita en claro en
/// el móvil.
/// </summary>
public static class Password
{
    /// <summary>Calcula la huella de una contraseña con la sal indicada.</summary>
    public static string Hash(string password, string salt)
    {
        ArgumentNullException.ThrowIfNull(password);
        ArgumentNullException.ThrowIfNull(salt);

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(salt + ":" + password));
        return Convert.ToHexString(bytes);
    }

    /// <summary>Crea una sal nueva al azar.</summary>
    public static string NewSalt()
    {
        Span<byte> salt = stackalloc byte[16];
        RandomNumberGenerator.Fill(salt);
        return Convert.ToHexString(salt);
    }

    /// <summary>Cierto si la contraseña produce la huella guardada.</summary>
    public static bool Matches(string password, string salt, string expectedHash) =>
        string.Equals(Hash(password, salt), expectedHash, StringComparison.OrdinalIgnoreCase);
}
