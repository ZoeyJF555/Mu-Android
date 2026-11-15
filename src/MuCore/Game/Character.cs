namespace Mu.Core.Game;

/// <summary>
/// Un personaje del jugador con sus números. Sabe subir de nivel y qué pasa al
/// recibir o hacer daño; el dibujo y el guardado viven fuera, así que estas
/// reglas se pueden probar sin abrir una ventana.
/// </summary>
public sealed class Character
{
    /// <summary>Experiencia que hace falta para pasar del primer nivel al segundo.</summary>
    public const int FirstLevelExperience = 100;

    /// <summary>Cuánto crece la experiencia necesaria con cada nivel.</summary>
    public const float ExperienceGrowth = 1.35f;

    /// <summary>Vida máxima al empezar.</summary>
    public int MaxHealth { get; private set; }

    /// <summary>Mana máxima al empezar.</summary>
    public int MaxMana { get; private set; }

    /// <summary>Fuerza de ataque base.</summary>
    public int Attack { get; private set; }

    /// <summary>Defensa base, que resta al daño recibido.</summary>
    public int Defense { get; private set; }

    /// <summary>Nombre del personaje, tal como lo escribió el jugador.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Clase elegida al crearlo.</summary>
    public CharacterClass Class { get; set; } = CharacterClass.DarkWizard;

    /// <summary>Nivel actual, desde 1.</summary>
    public int Level { get; private set; } = 1;

    /// <summary>Experiencia acumulada dentro del nivel actual.</summary>
    public int Experience { get; private set; }

    /// <summary>Vida que le queda.</summary>
    public int Health { get; private set; }

    /// <summary>Mana que le queda.</summary>
    public int Mana { get; private set; }

    /// <summary>Cierto si está vivo.</summary>
    public bool IsAlive => Health > 0;

    /// <summary>Experiencia que pide el nivel actual para subir.</summary>
    public int ExperienceForNextLevel => ExperienceNeeded(Level);

    /// <summary>Parte del nivel que se ha completado, de 0 a 1, para la barra.</summary>
    public float ExperienceFraction =>
        Math.Clamp(Experience / (float)Math.Max(1, ExperienceForNextLevel), 0f, 1f);

    /// <summary>Parte de vida que le queda, de 0 a 1, para la barra.</summary>
    public float HealthFraction => Math.Clamp(Health / (float)Math.Max(1, MaxHealth), 0f, 1f);

    /// <summary>Parte de mana que le queda, de 0 a 1, para la barra.</summary>
    public float ManaFraction => Math.Clamp(Mana / (float)Math.Max(1, MaxMana), 0f, 1f);

    /// <summary>Crea un personaje nuevo de la clase indicada, a nivel 1.</summary>
    public static Character Create(string name, CharacterClass characterClass)
    {
        var character = new Character
        {
            Name = name,
            Class = characterClass,
            Level = 1,
            Experience = 0,
        };

        character.ApplyStats();
        character.Restore();
        return character;
    }

    /// <summary>Recupera toda la vida y el mana (al entrar al mapa, por ejemplo).</summary>
    public void Restore()
    {
        Health = MaxHealth;
        Mana = MaxMana;
    }

    /// <summary>
    /// Suma la experiencia ganada y sube de nivel todas las veces que haga
    /// falta. Devuelve cuántos niveles ha subido.
    /// </summary>
    public int GainExperience(int amount)
    {
        if (amount <= 0)
        {
            return 0;
        }

        Experience += amount;
        var levels = 0;

        while (Experience >= ExperienceForNextLevel)
        {
            Experience -= ExperienceForNextLevel;
            Level++;
            levels++;
        }

        if (levels > 0)
        {
            ApplyStats();
            Restore();
        }

        return levels;
    }

    /// <summary>
    /// Recibe daño. Devuelve lo que le ha quitado de verdad, ya restada la
    /// defensa. Si llega a cero, el personaje queda muerto.
    /// </summary>
    public int TakeDamage(int amount)
    {
        if (amount <= 0)
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
        if (amount <= 0)
        {
            return 0;
        }

        var before = Health;
        Health = Math.Min(MaxHealth, Health + amount);
        return Health - before;
    }

    /// <summary>Vuelve a la vida con toda la vida y el mana, al morir.</summary>
    public void Revive() => Restore();

    /// <summary>Experiencia necesaria para pasar de un nivel al siguiente.</summary>
    public static int ExperienceNeeded(int level)
    {
        var needed = FirstLevelExperience;
        for (var i = 1; i < level; i++)
        {
            needed = (int)(needed * ExperienceGrowth);
        }

        return needed;
    }

    /// <summary>Recalcula los números a partir de la clase y el nivel.</summary>
    private void ApplyStats()
    {
        var baseStats = CharacterClasses.Base(Class);
        var perLevel = CharacterClasses.PerLevel(Class);
        var gained = Level - 1;

        MaxHealth = baseStats.MaxHealth + (perLevel.MaxHealth * gained);
        MaxMana = baseStats.MaxMana + (perLevel.MaxMana * gained);
        Attack = baseStats.Attack + (perLevel.Attack * gained);
        Defense = baseStats.Defense + (perLevel.Defense * gained);
    }

    /// <summary>Copia los números guardados, para volver a un personaje ya creado.</summary>
    public void LoadProgress(int level, int experience)
    {
        Level = Math.Max(1, level);
        Experience = Math.Max(0, experience);
        ApplyStats();
        Restore();
    }
}
