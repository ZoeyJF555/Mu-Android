using Mu.Core.Game;
using Xunit;

namespace Mu.Core.Tests;

/// <summary>Comprueba el guardado local: cuenta, personajes y progreso.</summary>
public class ProfileStoreTests : IDisposable
{
    private readonly string _folder;
    private readonly string _path;

    public ProfileStoreTests()
    {
        _folder = Path.Combine(Path.GetTempPath(), "mu-tests-" + Guid.NewGuid().ToString("N"));
        _path = Path.Combine(_folder, "profile.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    [Fact]
    public void RegistrarCuentaLaGuardaYCreaElFichero()
    {
        var store = new ProfileStore(_path);

        var result = store.LogIn("JUGADOR", "secreta");

        Assert.Equal(LoginResult.Created, result);
        Assert.True(store.HasAccount);
        Assert.True(File.Exists(_path));
    }

    [Fact]
    public void EntrarDeNuevoConLaMismaContrasenaFunciona()
    {
        var store = new ProfileStore(_path);
        store.LogIn("JUGADOR", "secreta");

        var again = new ProfileStore(_path);
        var result = again.LogIn("JUGADOR", "secreta");

        Assert.Equal(LoginResult.Success, result);
    }

    [Fact]
    public void UnaContrasenaDistintaNoEntra()
    {
        var store = new ProfileStore(_path);
        store.LogIn("JUGADOR", "secreta");

        var again = new ProfileStore(_path);

        Assert.Equal(LoginResult.InvalidPassword, again.LogIn("JUGADOR", "otra"));
    }

    [Fact]
    public void OtraCuentaNoEntra()
    {
        var store = new ProfileStore(_path);
        store.LogIn("JUGADOR", "secreta");

        var again = new ProfileStore(_path);

        Assert.Equal(LoginResult.InvalidAccount, again.LogIn("INTRUSO", "secreta"));
    }

    [Fact]
    public void LaContrasenaCortaNoValeAlRegistrar()
    {
        var store = new ProfileStore(_path);

        Assert.Equal(LoginResult.PasswordTooShort, store.LogIn("JUGADOR", "ab"));
        Assert.False(store.HasAccount);
    }

    [Fact]
    public void LaContrasenaNoSeGuardaEnClaro()
    {
        var store = new ProfileStore(_path);
        store.LogIn("JUGADOR", "secreta");

        var text = File.ReadAllText(_path);

        Assert.DoesNotContain("secreta", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CreaTresPersonajesYNoDejaRepetirNombre()
    {
        var store = new ProfileStore(_path);
        store.LogIn("JUGADOR", "secreta");

        Assert.True(store.CreateCharacter(0, "UNO", CharacterClass.DarkWizard));
        Assert.True(store.CreateCharacter(1, "DOS", CharacterClass.BladeKnight));
        Assert.True(store.CreateCharacter(2, "TRES", CharacterClass.Elf));

        Assert.False(store.CreateCharacter(0, "DOS", CharacterClass.Elf));
        Assert.Equal(-1, store.FirstFreeSlot());
    }

    [Fact]
    public void ElProgresoDelPersonajeSobreviveAlCerrarLaApp()
    {
        var store = new ProfileStore(_path);
        store.LogIn("JUGADOR", "secreta");
        store.CreateCharacter(0, "HEROE", CharacterClass.Elf);

        var character = Character.Create("HEROE", CharacterClass.Elf);
        character.GainExperience(500);
        store.SaveProgress(0, character);

        // Otra sesión: se vuelve a abrir el fichero desde cero.
        var again = new ProfileStore(_path);
        var slot = again.Slots()[0];

        Assert.NotNull(slot);
        Assert.Equal("HEROE", slot!.Name);
        Assert.Equal(character.Level, slot.Level);
        Assert.Equal(character.Experience, slot.Experience);
    }

    [Fact]
    public void BorrarUnPersonajeDejaElHuecoLibre()
    {
        var store = new ProfileStore(_path);
        store.LogIn("JUGADOR", "secreta");
        store.CreateCharacter(1, "HEROE", CharacterClass.Elf);

        Assert.True(store.DeleteCharacter(1));
        Assert.Null(store.Slots()[1]);
        Assert.Equal(0, store.FirstFreeSlot());
        Assert.True(store.CreateCharacter(0, "OTRO", CharacterClass.Elf));
    }

    [Fact]
    public void UnFicheroRotoNoImpideJugar()
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllText(_path, "{ esto no es json");

        var store = new ProfileStore(_path);

        Assert.False(store.HasAccount);
        Assert.Equal(LoginResult.Created, store.LogIn("JUGADOR", "secreta"));
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("A B", false)]
    [InlineData("NOMBRE_DEMASIADO_LARGO", false)]
    [InlineData("HEROE", true)]
    [InlineData("Bot1", true)]
    public void ValidaLosNombres(string name, bool expected) =>
        Assert.Equal(expected, ProfileStore.IsValidName(name));
}
