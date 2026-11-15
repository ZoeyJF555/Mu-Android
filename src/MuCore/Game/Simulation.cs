using System.Numerics;

namespace Mu.Core.Game;

/// <summary>
/// El mundo jugable sin dibujarlo: el jugador, los bots, los monstruos y los
/// NPC, con sus reglas. Aquí no hay ni una línea de gráficos, así que se puede
/// probar entero sin abrir una ventana y es el mismo código en escritorio y en
/// el móvil. La vida del jugador vive en su <see cref="Character"/>, y la de
/// bots y monstruos en sus <see cref="Combatant"/>: cada uno tiene un solo
/// sitio donde guardar sus números.
/// </summary>
public sealed class Simulation
{
    /// <summary>Cuántos bots pueblan el mapa.</summary>
    public const int BotCount = 10;

    /// <summary>Cuántos monstruos hay repartidos.</summary>
    public const int MonsterCount = 18;

    /// <summary>Alcance del golpe del jugador, en unidades.</summary>
    public const float PlayerAttackRange = 2.6f;

    /// <summary>Segundos entre golpes del jugador.</summary>
    public const float PlayerAttackInterval = 0.7f;

    /// <summary>Segundos que se ve la animación de golpe del jugador.</summary>
    public const float PlayerAttackAnimation = 0.55f;

    /// <summary>Velocidad del jugador, en unidades por segundo.</summary>
    public const float PlayerSpeed = 7f;

    /// <summary>Vida que recupera el jugador cada segundo, poco a poco.</summary>
    public const float PlayerRegeneration = 4f;

    /// <summary>Distancia a la que un monstruo ya pega al jugador.</summary>
    public const float MonsterAttackRange = 2.2f;

    /// <summary>Frases que sueltan los bots de vez en cuando, como en un chat real.</summary>
    private static readonly string[] BotChatter =
    [
        "ALGUIEN TIENE POCIONES?",
        "VOY A SUBIR DE NIVEL",
        "CUIDADO CON LOS BICHOS",
        "DONDE ESTA EL JEFE?",
        "BUENA CAZA",
        "ME VOY A LORENCIA",
        "NECESITO UN ARMA MEJOR",
        "ALGUIEN PARA GRUPO?",
    ];

    /// <summary>Nombres de los bots, al estilo de un servidor real.</summary>
    private static readonly string[] BotNames =
    [
        "DARKLORD", "MAGICA", "ELFINA", "BLADE", "XDRAGON", "SHADOW",
        "KRATOS", "LUNA", "THOR", "ARWEN", "MORGANA", "SEPHIROTH",
    ];

    private readonly Func<float, float, float> _ground;
    private readonly Random _random;
    private readonly List<Bot> _bots = [];
    private readonly List<Monster> _monsters = [];
    private readonly List<Npc> _npcs = [];

    private float _playerAttackCooldown;
    private float _playerAttackAnimationLeft;
    private float _playerDeathLeft;
    private float _botChatterCooldown;
    private float _monsterAttackCooldown;
    private bool _walking;

    /// <param name="character">El personaje del jugador, ya creado o cargado.</param>
    /// <param name="start">Dónde empieza el jugador.</param>
    /// <param name="ground">Altura del suelo en un punto del mapa.</param>
    /// <param name="seed">Semilla del azar, para que la partida salga igual en las pruebas.</param>
    /// <param name="mapName">Nombre del mapa, solo para el mensaje de bienvenida.</param>
    public Simulation(
        Character character,
        Vector3 start,
        Func<float, float, float> ground,
        int seed = 12345,
        string mapName = "LORENCIA")
    {
        ArgumentNullException.ThrowIfNull(character);
        ArgumentNullException.ThrowIfNull(ground);

        Player = character;
        Start = start;
        _ground = ground;
        _random = new Random(seed);

        PlayerPosition = new Vector3(start.X, ground(start.X, start.Z), start.Z);
        SpawnBots(start);
        SpawnMonsters(start);
        SpawnNpcs();

        Chat.Add(ChatKind.System, $"BIENVENIDO, {character.Name}");
        Chat.Add(ChatKind.System, $"{mapName}   BOTS {_bots.Count}   BICHOS {_monsters.Count}");
    }

    /// <summary>El personaje del jugador, con su nivel, su experiencia y su vida.</summary>
    public Character Player { get; }

    /// <summary>Dónde empieza el jugador (para reaparecer).</summary>
    public Vector3 Start { get; }

    /// <summary>Dónde está el jugador ahora mismo.</summary>
    public Vector3 PlayerPosition { get; private set; }

    /// <summary>Hacia dónde mira el jugador, en grados.</summary>
    public float PlayerHeadingDegrees { get; private set; }

    /// <summary>Los bots que pueblan el mapa.</summary>
    public IReadOnlyList<Bot> Bots => _bots;

    /// <summary>Los monstruos del mapa.</summary>
    public IReadOnlyList<Monster> Monsters => _monsters;

    /// <summary>Los NPC que hablan al acercarse.</summary>
    public IReadOnlyList<Npc> Npcs => _npcs;

    /// <summary>El chat del juego.</summary>
    public ChatLog Chat { get; } = new();

    /// <summary>Acción que debe reproducir el modelo del jugador.</summary>
    public int PlayerAction { get; private set; }

    /// <summary>Cierto si el jugador está vivo.</summary>
    public bool IsPlayerAlive => Player.IsAlive;

    /// <summary>Se avisa cada vez que el nivel o la experiencia cambian, para guardar.</summary>
    public event Action? ProgressChanged;

    /// <summary>
    /// Avanza un fotograma. El movimiento llega como un vector: X gira a la
    /// derecha, Y avanza hacia delante.
    /// </summary>
    /// <param name="seconds">Segundos transcurridos.</param>
    /// <param name="move">Movimiento pedido, de -1 a 1 en cada eje.</param>
    /// <param name="attack">Cierto si se ha pedido atacar en este fotograma.</param>
    public void Update(float seconds, Vector2 move, bool attack)
    {
        if (seconds <= 0f)
        {
            return;
        }

        UpdatePlayer(seconds, move, attack);
        UpdateMonsters(seconds);
        UpdateBots(seconds);
        UpdateBotChatter(seconds);
    }

    /// <summary>Habla con el NPC que esté cerca. Devuelve cierto si había alguno.</summary>
    public bool TalkToNearbyNpc()
    {
        foreach (var npc in _npcs)
        {
            if (npc.IsNear(PlayerPosition))
            {
                Chat.Add(ChatKind.Npc, $"{npc.Name}: {npc.Greeting}");
                return true;
            }
        }

        return false;
    }

    /// <summary>El jugador escribe algo en el chat.</summary>
    public void Say(string text) => Chat.Add(ChatKind.Player, $"{Player.Name}: {text}");

    /// <summary>Reanuda la partida tras volver de segundo plano.</summary>
    public void Resume()
    {
        Player.Revive();
        PlayerPosition = new Vector3(Start.X, _ground(Start.X, Start.Z), Start.Z);
        _playerDeathLeft = 0f;
        foreach (var bot in _bots)
        {
            bot.Revive();
        }
    }

    private void UpdatePlayer(float seconds, Vector2 move, bool attack)
    {
        if (_playerAttackAnimationLeft > 0f)
        {
            _playerAttackAnimationLeft = MathF.Max(0f, _playerAttackAnimationLeft - seconds);
        }

        if (_playerAttackCooldown > 0f)
        {
            _playerAttackCooldown = MathF.Max(0f, _playerAttackCooldown - seconds);
        }

        if (!Player.IsAlive)
        {
            PlayerAction = (int)Mu.Core.Formats.Bmd.PlayerAction.Die1;
            _playerDeathLeft -= seconds;
            if (_playerDeathLeft <= 0f)
            {
                RespawnPlayer();
            }

            return;
        }

        var forward = Math.Clamp(-move.Y, -1f, 1f);
        PlayerHeadingDegrees = WrapDegrees(PlayerHeadingDegrees + (move.X * 200f * seconds));

        _walking = MathF.Abs(forward) > 0.01f;
        if (_walking)
        {
            var heading = PlayerHeadingDegrees * MathF.PI / 180f;
            var step = new Vector3(MathF.Cos(heading), 0f, MathF.Sin(heading)) * (forward * PlayerSpeed * seconds);
            var position = PlayerPosition + step;
            position.X = Math.Clamp(position.X, 1f, 255f);
            position.Z = Math.Clamp(position.Z, 1f, 255f);
            position.Y = _ground(position.X, position.Z);
            PlayerPosition = position;
        }

        if (attack && _playerAttackCooldown <= 0f)
        {
            _playerAttackCooldown = PlayerAttackInterval;
            _playerAttackAnimationLeft = PlayerAttackAnimation;
            PlayerAttack();
        }

        PlayerAction = _playerAttackAnimationLeft > 0f
            ? (int)Mu.Core.Formats.Bmd.PlayerAction.AttackSwordRight1
            : _walking ? (int)Mu.Core.Formats.Bmd.PlayerAction.WalkMale : 0;

        if (Player.Health < Player.MaxHealth)
        {
            Player.Heal(Math.Max(1, (int)(PlayerRegeneration * seconds)));
        }
    }

    /// <summary>Golpea al enemigo vivo más cercano dentro del alcance.</summary>
    private void PlayerAttack()
    {
        var target = NearestTarget(PlayerAttackRange);
        if (target is null)
        {
            return;
        }

        FacePlayerTowards(target.Value.Body.Position);
        var taken = target.Value.Body.TakeDamage(Player.Attack);
        if (taken > 0 && !target.Value.Body.IsAlive)
        {
            var reward = target.Value.Body.ExperienceReward;
            var levels = Player.GainExperience(reward);
            Chat.Add(ChatKind.System, $"{target.Value.Body.Name} MUERTO   +{reward} EXP");
            if (levels > 0)
            {
                Chat.Add(ChatKind.System, $"SUBISTE AL NIVEL {Player.Level}");
            }

            ProgressChanged?.Invoke();
        }
    }

    /// <summary>El monstruo o bot vivo más cercano dentro de un alcance.</summary>
    private MonsterOrBot? NearestTarget(float range)
    {
        MonsterOrBot? best = null;
        var bestDistance = range;

        foreach (var monster in _monsters)
        {
            if (!monster.Body.IsAlive)
            {
                continue;
            }

            var distance = Vector3.Distance(PlayerPosition, monster.Body.Position);
            if (distance <= bestDistance)
            {
                best = new MonsterOrBot(monster.Body);
                bestDistance = distance;
            }
        }

        foreach (var bot in _bots)
        {
            if (!bot.Body.IsAlive)
            {
                continue;
            }

            var distance = Vector3.Distance(PlayerPosition, bot.Body.Position);
            if (distance <= bestDistance)
            {
                best = new MonsterOrBot(bot.Body);
                bestDistance = distance;
            }
        }

        return best;
    }

    private void UpdateMonsters(float seconds)
    {
        foreach (var monster in _monsters)
        {
            monster.Update(seconds);

            if (!monster.Body.IsAlive)
            {
                continue;
            }

            if (monster.Body.Health < monster.Body.MaxHealth)
            {
                monster.Body.Heal(Math.Max(1, (int)(2f * seconds)));
            }

            if (!Player.IsAlive)
            {
                continue;
            }

            if (Vector3.Distance(PlayerPosition, monster.Body.Position) > MonsterAttackRange)
            {
                continue;
            }

            _monsterAttackCooldown -= seconds;
            if (_monsterAttackCooldown > 0f)
            {
                continue;
            }

            _monsterAttackCooldown = 1.4f;
            var taken = Player.TakeDamage(monster.Body.Attack);
            if (taken > 0)
            {
                Chat.Add(ChatKind.System, $"{monster.Body.Name} TE GOLPEA   -{taken} HP");
            }

            if (!Player.IsAlive)
            {
                Chat.Add(ChatKind.System, "HAS MUERTO. VOLVIENDO AL PUEBLO...");
                _playerDeathLeft = 2.5f;
            }
        }
    }

    private void UpdateBots(float seconds)
    {
        foreach (var bot in _bots)
        {
            var current = bot;
            current.Update(seconds, _ground, _monsters, monster => OnBotKilledMonster(current, monster));

            if (current.Body.IsAlive && current.Body.Health < current.Body.MaxHealth)
            {
                current.Body.Heal(Math.Max(1, (int)(4f * seconds)));
            }
        }
    }

    private void OnBotKilledMonster(Bot bot, Monster monster) =>
        Chat.Add(ChatKind.Bot, $"{bot.Name} DERROTO A {monster.Body.Name}");

    private void UpdateBotChatter(float seconds)
    {
        _botChatterCooldown -= seconds;
        if (_botChatterCooldown > 0f || _bots.Count == 0)
        {
            return;
        }

        _botChatterCooldown = 6f + ((float)_random.NextDouble() * 8f);
        var bot = _bots[_random.Next(_bots.Count)];
        var line = BotChatter[_random.Next(BotChatter.Length)];
        Chat.Add(ChatKind.Bot, $"{bot.Name}: {line}");
    }

    private void RespawnPlayer()
    {
        Player.Revive();
        PlayerPosition = new Vector3(Start.X, _ground(Start.X, Start.Z), Start.Z);
        Chat.Add(ChatKind.System, "HAS VUELTO A LORENCIA");
    }

    private void FacePlayerTowards(Vector3 point)
    {
        var delta = point - PlayerPosition;
        if (delta.LengthSquared() > 0.0001f)
        {
            PlayerHeadingDegrees = MathF.Atan2(delta.Z, delta.X) * 180f / MathF.PI;
        }
    }

    /// <summary>Coloca los bots alrededor del punto de partida, cada uno con su ronda.</summary>
    private void SpawnBots(Vector3 start)
    {
        for (var i = 0; i < BotCount; i++)
        {
            var angle = i * MathF.Tau / BotCount;
            var radius = 6f + ((float)_random.NextDouble() * 14f);
            var home = new Vector3(
                start.X + (MathF.Cos(angle) * radius),
                0f,
                start.Z + (MathF.Sin(angle) * radius));
            home.Y = _ground(home.X, home.Z);

            _bots.Add(new Bot(BotNames[i % BotNames.Length], home, seed: 1000 + i));
        }
    }

    /// <summary>Reparte los monstruos por el mapa, lejos del punto de partida.</summary>
    private void SpawnMonsters(Vector3 start)
    {
        for (var i = 0; i < MonsterCount; i++)
        {
            var kind = MonsterCatalog.All[i % MonsterCatalog.All.Length];
            var angle = (float)(_random.NextDouble() * Math.Tau);
            var radius = 14f + ((float)_random.NextDouble() * 40f);
            var position = new Vector3(
                Math.Clamp(start.X + (MathF.Cos(angle) * radius), 8f, 248f),
                0f,
                Math.Clamp(start.Z + (MathF.Sin(angle) * radius), 8f, 248f));
            position.Y = _ground(position.X, position.Z);

            _monsters.Add(new Monster(kind, position, seed: 2000 + i));
        }
    }

    private void SpawnNpcs()
    {
        foreach (var npc in Npc.Lorencia)
        {
            _npcs.Add(npc);
        }
    }

    private static float WrapDegrees(float degrees)
    {
        degrees %= 360f;
        return degrees < 0f ? degrees + 360f : degrees;
    }

    /// <summary>Un objetivo al que puede pegar el jugador, sea monstruo o bot.</summary>
    private readonly record struct MonsterOrBot(Combatant Body);
}
