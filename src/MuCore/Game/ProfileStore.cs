using System.Text.Json;

namespace Mu.Core.Game;

/// <summary>El resultado de intentar entrar o registrar una cuenta.</summary>
public enum LoginResult
{
    /// <summary>Se entró en una cuenta que ya existía.</summary>
    Success,

    /// <summary>Se creó la cuenta porque no había ninguna.</summary>
    Created,

    /// <summary>El nombre de cuenta no vale.</summary>
    InvalidAccount,

    /// <summary>La contraseña no vale (vacía, o no coincide al entrar).</summary>
    InvalidPassword,

    /// <summary>La contraseña es demasiado corta al crear la cuenta.</summary>
    PasswordTooShort,
}

/// <summary>
/// Guarda la cuenta y los personajes en un fichero del dispositivo. Funciona
/// igual en escritorio y en Android porque solo necesita una carpeta donde
/// escribir; el móvil le pasa la suya.
/// </summary>
public sealed class ProfileStore
{
    /// <summary>Cuántos espacios de personaje hay.</summary>
    public const int SlotCount = 3;

    /// <summary>Largo mínimo de la contraseña al registrar la cuenta.</summary>
    public const int MinimumPasswordLength = 4;

    /// <summary>Largo máximo del nombre de cuenta o personaje.</summary>
    public const int MaximumNameLength = 12;

    private readonly string _path;
    private readonly object _gate = new();

    /// <param name="path">Fichero donde se guarda todo.</param>
    public ProfileStore(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _path = path;
        Data = Load();
    }

    /// <summary>Lo que hay guardado ahora mismo.</summary>
    public ProfileData Data { get; private set; }

    /// <summary>Cierto si ya existe una cuenta en el dispositivo.</summary>
    public bool HasAccount => !string.IsNullOrEmpty(Data.Account);

    /// <summary>
    /// Entra con una cuenta, o la registra si todavía no hay ninguna. Si ya hay
    /// cuenta, el nombre tiene que coincidir y la contraseña también.
    /// </summary>
    public LoginResult LogIn(string account, string password)
    {
        account = (account ?? string.Empty).Trim();

        if (!IsValidName(account))
        {
            return LoginResult.InvalidAccount;
        }

        if (!HasAccount)
        {
            if ((password ?? string.Empty).Length < MinimumPasswordLength)
            {
                return LoginResult.PasswordTooShort;
            }

            Data.Account = account;
            Data.Salt = Password.NewSalt();
            Data.PasswordHash = Password.Hash(password!, Data.Salt);
            Save();
            return LoginResult.Created;
        }

        if (!string.Equals(Data.Account, account, StringComparison.OrdinalIgnoreCase))
        {
            return LoginResult.InvalidAccount;
        }

        if (!Password.Matches(password ?? string.Empty, Data.Salt, Data.PasswordHash))
        {
            return LoginResult.InvalidPassword;
        }

        return LoginResult.Success;
    }

    /// <summary>Los personajes guardados, siempre con un hueco por espacio.</summary>
    public SavedCharacter?[] Slots()
    {
        var slots = new SavedCharacter?[SlotCount];
        for (var i = 0; i < SlotCount && i < Data.Characters.Count; i++)
        {
            slots[i] = Data.Characters[i];
        }

        return slots;
    }

    /// <summary>Cierto si el nombre está libre (no lo usa otro personaje).</summary>
    public bool IsNameFree(string name, int slotToIgnore = -1)
    {
        for (var i = 0; i < Data.Characters.Count; i++)
        {
            if (i == slotToIgnore)
            {
                continue;
            }

            if (string.Equals(Data.Characters[i]?.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Primer espacio libre, o -1 si están los tres ocupados.</summary>
    public int FirstFreeSlot()
    {
        var slots = Slots();
        for (var i = 0; i < slots.Length; i++)
        {
            if (slots[i] is null)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>Crea un personaje en el espacio indicado.</summary>
    public bool CreateCharacter(int slot, string name, CharacterClass characterClass)
    {
        name = (name ?? string.Empty).Trim();
        if (slot < 0 || slot >= SlotCount || !IsValidName(name) || !IsNameFree(name))
        {
            return false;
        }

        while (Data.Characters.Count < SlotCount)
        {
            Data.Characters.Add(null);
        }

        Data.Characters[slot] = new SavedCharacter
        {
            Name = name,
            Class = (int)characterClass,
            Level = 1,
            Experience = 0,
        };

        Save();
        return true;
    }

    /// <summary>Borra el personaje de un espacio.</summary>
    public bool DeleteCharacter(int slot)
    {
        if (slot < 0 || slot >= SlotCount || slot >= Data.Characters.Count || Data.Characters[slot] is null)
        {
            return false;
        }

        Data.Characters[slot] = null;
        Save();
        return true;
    }

    /// <summary>Vuelve a dejar el personaje en el nivel y la experiencia que tiene.</summary>
    public void SaveProgress(int slot, Character character)
    {
        ArgumentNullException.ThrowIfNull(character);

        var slots = Slots();
        if (slot < 0 || slot >= SlotCount || slots[slot] is null)
        {
            return;
        }

        slots[slot]!.Level = character.Level;
        slots[slot]!.Experience = character.Experience;
        Data.Characters = [.. slots];
        Save();
    }

    /// <summary>Cierto si el nombre sirve (sin espacios raros y con largo razonable).</summary>
    public static bool IsValidName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > MaximumNameLength)
        {
            return false;
        }

        foreach (var character in name)
        {
            if (!char.IsLetterOrDigit(character))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Lee el fichero, o devuelve un perfil vacío si no hay o está roto.</summary>
    private ProfileData Load()
    {
        try
        {
            if (!File.Exists(_path))
            {
                return new ProfileData();
            }

            var json = File.ReadAllText(_path);
            var data = JsonSerializer.Deserialize(json, ProfileJsonContext.Default.ProfileData);
            if (data is null)
            {
                return new ProfileData();
            }

            while (data.Characters.Count < SlotCount)
            {
                data.Characters.Add(null);
            }

            return data;
        }
        catch (Exception error) when (error is IOException or JsonException or UnauthorizedAccessException)
        {
            // Un fichero ilegible no debe impedir jugar: se empieza de cero.
            return new ProfileData();
        }
    }

    /// <summary>Escribe el perfil en el dispositivo.</summary>
    private void Save()
    {
        lock (_gate)
        {
            try
            {
                var folder = Path.GetDirectoryName(_path);
                if (!string.IsNullOrEmpty(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                // Se escribe en un fichero temporal y se reemplaza, para que un
                // corte a mitad no deje el perfil a medias.
                var temporary = _path + ".tmp";
                File.WriteAllText(temporary, JsonSerializer.Serialize(Data, ProfileJsonContext.Default.ProfileData));
                File.Move(temporary, _path, overwrite: true);
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                Console.Error.WriteLine($"[Mu] no se pudo guardar el perfil: {error.Message}");
            }
        }
    }
}
