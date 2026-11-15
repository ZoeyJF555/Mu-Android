using System.Numerics;

namespace Mu.Core.Game;

/// <summary>
/// Un ser vivo del mapa: el jugador, un bot o un monstruo comparten lo que hace
/// falta para pelear. Solo guarda números y posición; el dibujo va aparte.
/// </summary>
public sealed class Combatant
{
    /// <summary>Dónde está, en unidades del mundo (una casilla por unidad).</summary>
    public Vector3 Position { get; set; }

    /// <summary>Hacia dónde mira, en grados.</summary>
    public float HeadingDegrees { get; set; }

    /// <summary>Vida máxima.</summary>
    public int MaxHealth { get; init; } = 100;

    /// <summary>Vida que le queda.</summary>
    public int Health { get; private set; } = 100;

    /// <summary>Cuánto quita de daño por golpe.</summary>
    public int Attack { get; init; } = 5;

    /// <summary>Cuánto resta al daño que recibe.</summary>
    public int Defense { get; init; }

    /// <summary>Experiencia que da al morir.</summary>
    public int ExperienceReward { get; init; } = 10;

    /// <summary>Segundos que quedan de la animación de golpe.</summary>
    public float AttackAnimationLeft { get; set; }

    /// <summary>Segundos que quedan antes de volver a atacar.</summary>
    public float AttackCooldown { get; set; }

    /// <summary>Nombre que se enseña encima (solo bots y NPC).</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Cierto si sigue vivo.</summary>
    public bool IsAlive => Health > 0;

    /// <summary>Parte de vida que le queda, de 0 a 1.</summary>
    public float HealthFraction => Math.Clamp(Health / (float)Math.Max(1, MaxHealth), 0f, 1f);

    /// <summary>Deja la vida al máximo.</summary>
    public void Restore() => Health = MaxHealth;

    /// <summary>Recibe daño y devuelve lo que le ha quitado de verdad.</summary>
    public int TakeDamage(int amount)
    {
        if (amount <= 0 || !IsAlive)
        {
            return 0;
        }

        var taken = Math.Max(1, amount - Defense);
        var before = Health;
        Health = Math.Max(0, Health - taken);
        return before - Health;
    }

    /// <summary>Recupera vida sin pasarse del máximo.</summary>
    public int Heal(int amount)
    {
        if (amount <= 0 || !IsAlive)
        {
            return 0;
        }

        var before = Health;
        Health = Math.Min(MaxHealth, Health + amount);
        return Health - before;
    }
}

/// <summary>Los monstruos que se reparten por Lorencia, con sus números.</summary>
public static class MonsterCatalog
{
    /// <summary>Un tipo de monstruo: su fichero, lo que aguanta y lo que da.</summary>
    public readonly record struct Kind(string ModelPath, string Name, int Health, int Attack, int Experience);

    /// <summary>Los monstruos que traen los datos del cliente.</summary>
    public static readonly Kind[] All =
    [
        new("Monster/Monster01.bmd", "BULL", 120, 6, 45),
        new("Monster/Monster02.bmd", "HOUND", 90, 8, 40),
        new("Monster/Monster03.bmd", "SPIDER", 150, 7, 60),
        new("Monster/Monster04.bmd", "GOBLIN", 110, 9, 55),
        new("Monster/Monster05.bmd", "ORC", 170, 11, 75),
        new("Monster/Monster06.bmd", "STONE", 200, 12, 90),
    ];
}
