using Mu.Core.Game;
using Xunit;

namespace Mu.Core.Tests;

/// <summary>Comprueba las reglas del personaje: niveles, daño y curación.</summary>
public class CharacterTests
{
    [Fact]
    public void EmpiezaEnNivelUnoConTodaLaVida()
    {
        var character = Character.Create("HEROE", CharacterClass.BladeKnight);

        Assert.Equal(1, character.Level);
        Assert.Equal(0, character.Experience);
        Assert.True(character.IsAlive);
        Assert.Equal(character.MaxHealth, character.Health);
        Assert.Equal(character.MaxMana, character.Mana);
    }

    [Fact]
    public void CadaClaseTieneNumerosDistintos()
    {
        var mago = Character.Create("A", CharacterClass.DarkWizard);
        var guerrero = Character.Create("B", CharacterClass.BladeKnight);
        var elfo = Character.Create("C", CharacterClass.Elf);

        Assert.True(guerrero.MaxHealth > mago.MaxHealth);
        Assert.True(mago.MaxMana > guerrero.MaxMana);
        Assert.True(elfo.MaxHealth > mago.MaxHealth && elfo.MaxHealth < guerrero.MaxHealth);
        Assert.True(guerrero.Attack > elfo.Attack && elfo.Attack > mago.Attack);
    }

    [Fact]
    public void SubeDeNivelAlLlegarALaExperiencia()
    {
        var character = Character.Create("HEROE", CharacterClass.Elf);
        var needed = character.ExperienceForNextLevel;

        var levels = character.GainExperience(needed);

        Assert.Equal(1, levels);
        Assert.Equal(2, character.Level);
        Assert.Equal(0, character.Experience);
    }

    [Fact]
    public void SubeVariosNivelesDeGolpeYRestauraLaVida()
    {
        var character = Character.Create("HEROE", CharacterClass.Elf);
        character.TakeDamage(100000);
        Assert.False(character.IsAlive);

        var levels = character.GainExperience(100000);

        Assert.True(levels >= 3);
        Assert.True(character.IsAlive);
        Assert.Equal(character.MaxHealth, character.Health);
    }

    [Fact]
    public void LaExperienciaSobranteQuedaParaElSiguienteNivel()
    {
        var character = Character.Create("HEROE", CharacterClass.Elf);
        var needed = character.ExperienceForNextLevel;

        character.GainExperience(needed + 25);

        Assert.Equal(2, character.Level);
        Assert.Equal(25, character.Experience);
    }

    [Fact]
    public void ElDanoNoBajaDeCeroYLaDefensaResta()
    {
        var character = Character.Create("HEROE", CharacterClass.BladeKnight);

        var taken = character.TakeDamage(10000);

        Assert.Equal(0, character.Health);
        Assert.False(character.IsAlive);
        Assert.Equal(character.MaxHealth, taken);
    }

    [Fact]
    public void CurarNoPasaDelMaximo()
    {
        var character = Character.Create("HEROE", CharacterClass.Elf);
        character.TakeDamage(20);

        character.Heal(10000);

        Assert.Equal(character.MaxHealth, character.Health);
    }

    [Fact]
    public void RecargarUnProgresoGuardadoDevuelveElMismoNivel()
    {
        var character = Character.Create("HEROE", CharacterClass.DarkWizard);
        character.LoadProgress(10, 55);

        Assert.Equal(10, character.Level);
        Assert.Equal(55, character.Experience);
        Assert.Equal(character.MaxHealth, character.Health);
        Assert.True(character.MaxHealth > Character.Create("X", CharacterClass.DarkWizard).MaxHealth);
    }
}
