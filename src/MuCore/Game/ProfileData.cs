using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mu.Core.Game;

/// <summary>Un personaje guardado en el móvil, tal como se escribe en el fichero.</summary>
public sealed class SavedCharacter
{
    /// <summary>Nombre del personaje.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Clase, como número para que el fichero siga leyéndose aunque cambie el enumerado.</summary>
    public int Class { get; set; } = 1;

    /// <summary>Nivel alcanzado.</summary>
    public int Level { get; set; } = 1;

    /// <summary>Experiencia dentro del nivel.</summary>
    public int Experience { get; set; }
}

/// <summary>Todo lo que se guarda en el dispositivo: la cuenta y sus personajes.</summary>
public sealed class ProfileData
{
    /// <summary>Nombre de la cuenta local.</summary>
    public string Account { get; set; } = string.Empty;

    /// <summary>Sal con la que se calculó la huella de la contraseña.</summary>
    public string Salt { get; set; } = string.Empty;

    /// <summary>Huella de la contraseña. Nunca se guarda la contraseña.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Los tres espacios de personaje; null donde no hay ninguno.</summary>
    public List<SavedCharacter?> Characters { get; set; } = [null, null, null];
}

/// <summary>
/// Contexto de serialización generado en tiempo de compilación. Hace falta
/// porque en Android el recorte quita los metadatos que usa la serialización por
/// reflexión, y con ellos el guardado dejaría de funcionar en el móvil.
/// </summary>
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(ProfileData))]
public sealed partial class ProfileJsonContext : JsonSerializerContext;
