using Mu.Core.Assets;

namespace Mu.Client;

/// <summary>
/// Dónde están los ficheros de datos del cliente. En escritorio se busca una
/// carpeta `Data` junto al ejecutable o la ruta que diga la variable de entorno
/// `MU_DATA`; así el mismo juego se puede apuntar a cualquier copia de los
/// datos sin recompilar.
/// </summary>
public static class DataLocator
{
    /// <summary>Variable de entorno con la ruta a los datos.</summary>
    public const string EnvironmentVariable = "MU_DATA";

    /// <summary>Variable de entorno con la carpeta donde se guarda el perfil.</summary>
    public const string ProfileEnvironmentVariable = "MU_PROFILE";

    /// <summary>Nombre del fichero donde se guarda la cuenta y los personajes.</summary>
    public const string ProfileFileName = "profile.json";

    /// <summary>Nombre de la carpeta de datos que se busca junto al ejecutable.</summary>
    public const string FolderName = "Data";

    /// <summary>Devuelve el origen de recursos, o null si no se encuentra la carpeta.</summary>
    public static IAssetSource? TryOpen()
    {
        var root = FindRoot();
        return root is null ? null : new FileAssetSource(root);
    }

    /// <summary>La carpeta de datos, o null si no existe ninguna.</summary>
    public static string? FindRoot()
    {
        var fromEnvironment = Environment.GetEnvironmentVariable(EnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(fromEnvironment) && Directory.Exists(fromEnvironment))
        {
            return fromEnvironment;
        }

        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, FolderName),
            Path.Combine(Directory.GetCurrentDirectory(), FolderName),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", FolderName),
        };

        return candidates.Select(Path.GetFullPath).FirstOrDefault(Directory.Exists);
    }

    /// <summary>
    /// Dónde se guarda el perfil. En el móvil, Android pasa su carpeta de datos
    /// en <see cref="ProfileEnvironmentVariable"/>; en escritorio, si no se dice
    /// nada, se usa una carpeta junto al ejecutable.
    /// </summary>
    public static string ProfilePath()
    {
        var folder = Environment.GetEnvironmentVariable(ProfileEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(folder))
        {
            folder = Path.Combine(AppContext.BaseDirectory, "Profile");
        }

        return Path.Combine(folder, ProfileFileName);
    }
}
