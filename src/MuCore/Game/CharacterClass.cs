namespace Mu.Core.Game;

/// <summary>Las clases de personaje que se pueden elegir al crearlo.</summary>
public enum CharacterClass
{
    /// <summary>Mago oscuro: poca vida y mucha mana.</summary>
    DarkWizard = 1,

    /// <summary>Caballero: mucha vida y fuerza.</summary>
    BladeKnight = 2,

    /// <summary>Elfo: equilibrado.</summary>
    Elf = 3,
}

/// <summary>Los números base de una clase, antes de subir de nivel.</summary>
public readonly record struct ClassStats(int MaxHealth, int MaxMana, int Attack, int Defense);

/// <summary>Nombres y números de las clases.</summary>
public static class CharacterClasses
{
    /// <summary>Todas las clases, en el orden en que se ofrecen.</summary>
    public static readonly CharacterClass[] All =
    [
        CharacterClass.DarkWizard,
        CharacterClass.BladeKnight,
        CharacterClass.Elf,
    ];

    /// <summary>El nombre que se enseña en pantalla.</summary>
    public static string Name(CharacterClass value) => value switch
    {
        CharacterClass.DarkWizard => "Mago",
        CharacterClass.BladeKnight => "Guerrero",
        CharacterClass.Elf => "Elfo",
        _ => "?",
    };

    /// <summary>
    /// El número de clase que se usa para vestir el cuerpo. Los datos solo traen
    /// las piezas de la clase 01, así que las tres clases comparten cuerpo y se
    /// diferencian por sus números.
    /// </summary>
    public static int ModelNumber(CharacterClass value) => 1;

    /// <summary>Los números con los que empieza un personaje de la clase.</summary>
    public static ClassStats Base(CharacterClass value) => value switch
    {
        CharacterClass.DarkWizard => new ClassStats(60, 130, 11, 7),
        CharacterClass.BladeKnight => new ClassStats(130, 45, 19, 14),
        CharacterClass.Elf => new ClassStats(95, 85, 14, 10),
        _ => new ClassStats(90, 80, 13, 10),
    };

    /// <summary>Lo que sube cada número al ganar un nivel.</summary>
    public static ClassStats PerLevel(CharacterClass value) => value switch
    {
        CharacterClass.DarkWizard => new ClassStats(12, 22, 2, 1),
        CharacterClass.BladeKnight => new ClassStats(26, 8, 3, 2),
        CharacterClass.Elf => new ClassStats(18, 14, 2, 2),
        _ => new ClassStats(18, 14, 2, 2),
    };
}
