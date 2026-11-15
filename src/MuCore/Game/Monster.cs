using System.Numerics;

namespace Mu.Core.Game;

/// <summary>
/// Un monstruo del mapa: sus números, dónde está y si sigue vivo. Al morir
/// espera un rato y vuelve a aparecer, para que el mapa nunca se quede vacío.
/// </summary>
public sealed class Monster
{
    /// <summary>Segundos que tarda en volver tras morir.</summary>
    public const float RespawnSeconds = 12f;

    private readonly Random _random;

    /// <param name="kind">Qué monstruo es.</param>
    /// <param name="spawn">Dónde aparece.</param>
    /// <param name="seed">Semilla del azar, para que salga igual en las pruebas.</param>
    public Monster(MonsterCatalog.Kind kind, Vector3 spawn, int seed)
    {
        Kind = kind;
        Spawn = spawn;
        _random = new Random(seed);
        Body = new Combatant
        {
            Name = kind.Name,
            Position = spawn,
            MaxHealth = kind.Health,
            Attack = kind.Attack,
            Defense = 1,
            ExperienceReward = kind.Experience,
            HeadingDegrees = (float)(_random.NextDouble() * 360.0),
        };
        Body.Restore();
    }

    /// <summary>De qué tipo es.</summary>
    public MonsterCatalog.Kind Kind { get; }

    /// <summary>Dónde apareció, para volver a ponerlo ahí.</summary>
    public Vector3 Spawn { get; }

    /// <summary>Sus números y su posición.</summary>
    public Combatant Body { get; }

    /// <summary>Segundos que quedan para que vuelva a aparecer (0 si está vivo).</summary>
    public float RespawnLeft { get; private set; }

    /// <summary>Cierto si está vivo y en el mapa.</summary>
    public bool IsPresent => Body.IsAlive;

    /// <summary>La acción que debe reproducir su modelo.</summary>
    public int Action => !Body.IsAlive ? (int)Mu.Core.Formats.Bmd.PlayerAction.Die1 : 0;

    /// <summary>Recibe daño y devuelve lo que le han quitado.</summary>
    public int TakeDamage(int amount)
    {
        var taken = Body.TakeDamage(amount);
        if (!Body.IsAlive && RespawnLeft <= 0f)
        {
            RespawnLeft = RespawnSeconds;
        }

        return taken;
    }

    /// <summary>Cuenta atrás para reaparecer; cuando llega a cero vuelve con toda la vida.</summary>
    public void Update(float seconds)
    {
        if (Body.IsAlive || RespawnLeft <= 0f)
        {
            return;
        }

        RespawnLeft = MathF.Max(0f, RespawnLeft - seconds);
        if (RespawnLeft <= 0f)
        {
            Body.Restore();
            Body.Position = Spawn;
        }
    }
}
