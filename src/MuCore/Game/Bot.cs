using System.Numerics;

namespace Mu.Core.Game;

/// <summary>Qué está haciendo un bot ahora mismo.</summary>
public enum BotState
{
    /// <summary>Va andando hacia un punto lejano.</summary>
    Walking,

    /// <summary>Persigue a un monstruo cercano.</summary>
    Chasing,

    /// <summary>Está pegando a un monstruo.</summary>
    Attacking,

    /// <summary>Se queda quieto un rato, como un jugador que mira la pantalla.</summary>
    Idle,
}

/// <summary>
/// Un "jugador" simulado. Se comporta como alguien jugando: anda de un lado a
/// otro, se para, busca monstruos cercanos, los persigue y les pega. La lógica
/// es pura (solo números y azar con semilla) para poder probarla sin dibujar.
/// </summary>
public sealed class Bot
{
    /// <summary>Distancia a la que ve un monstruo, en unidades.</summary>
    public const float SightRange = 9f;

    /// <summary>Distancia a la que ya puede pegar, en unidades.</summary>
    public const float AttackRange = 2.2f;

    /// <summary>
    /// Velocidad al andar, en unidades por segundo. Es la misma que la del
    /// jugador, para que su paso (la animación de caminar) no patine.
    /// </summary>
    public const float WalkSpeed = 3.5f;

    /// <summary>Segundos entre golpe y golpe.</summary>
    public const float AttackInterval = 1.1f;

    /// <summary>Lo más lejos que se aleja de su ronda, en unidades.</summary>
    public const float RoamRadius = 22f;

    private readonly Random _random;
    private Vector3 _destination;
    private float _idleLeft;
    private float _thinkLeft;
    private List<Vector2>? _path;
    private int _pathIndex;

    /// <param name="name">Nombre que se enseña encima del bot.</param>
    /// <param name="home">Centro de su ronda.</param>
    /// <param name="seed">Semilla, para que la partida salga igual en las pruebas.</param>
    public Bot(string name, Vector3 home, int seed)
    {
        Name = name;
        Home = home;
        _random = new Random(seed);
        Body = new Combatant
        {
            Name = name,
            Position = home,
            MaxHealth = 140,
            Attack = 10,
            Defense = 2,
        };
        Body.Restore();
        _destination = home;
    }

    /// <summary>Nombre del bot.</summary>
    public string Name { get; }

    /// <summary>Centro alrededor del cual ronda.</summary>
    public Vector3 Home { get; }

    /// <summary>Sus números y su posición.</summary>
    public Combatant Body { get; }

    /// <summary>Qué está haciendo.</summary>
    public BotState State { get; private set; } = BotState.Idle;

    /// <summary>Posición, para abreviar.</summary>
    public Vector3 Position => Body.Position;

    /// <summary>Hacia dónde mira, para abreviar.</summary>
    public float HeadingDegrees => Body.HeadingDegrees;

    /// <summary>Acción de dibujo: 0 parado, si no la que toca.</summary>
    public BotAction Action { get; private set; }

    /// <summary>
    /// Avanza un fotograma. Recibe la altura del suelo para no hundirse, la
    /// lista de monstruos a los que puede pegar y si una casilla se puede pisar
    /// (para rodear los obstáculos en vez de atravesarlos).
    /// </summary>
    /// <param name="seconds">Segundos transcurridos.</param>
    /// <param name="ground">Altura del suelo en un punto.</param>
    /// <param name="monsters">Los monstruos que hay en el mapa.</param>
    /// <param name="onKill">Aviso de que ha matado a un monstruo (da experiencia).</param>
    /// <param name="walkable">Cierto si una casilla se puede pisar; null = todas.</param>
    public void Update(
        float seconds,
        Func<float, float, float> ground,
        IReadOnlyList<Monster> monsters,
        Action<Monster>? onKill = null,
        Func<int, int, bool>? walkable = null)
    {
        ArgumentNullException.ThrowIfNull(ground);
        ArgumentNullException.ThrowIfNull(monsters);

        if (Body.AttackAnimationLeft > 0f)
        {
            Body.AttackAnimationLeft = MathF.Max(0f, Body.AttackAnimationLeft - seconds);
        }

        if (Body.AttackCooldown > 0f)
        {
            Body.AttackCooldown = MathF.Max(0f, Body.AttackCooldown - seconds);
        }

        if (!Body.IsAlive)
        {
            Action = BotAction.Dead;
            return;
        }

        _thinkLeft -= seconds;
        if (_thinkLeft <= 0f)
        {
            _thinkLeft = 0.3f;
            Think(monsters);
        }

        switch (State)
        {
            case BotState.Idle:
                Idle(seconds);
                break;
            case BotState.Walking:
                Walk(seconds, ground, walkable);
                break;
            case BotState.Chasing:
                Chase(seconds, ground, monsters, walkable);
                break;
            case BotState.Attacking:
                Attack(seconds, monsters, onKill);
                break;
        }

        Body.Position = new Vector3(Body.Position.X, ground(Body.Position.X, Body.Position.Z), Body.Position.Z);
    }

    /// <summary>Reparte la vida de todos los bots por el mapa.</summary>
    public void Revive()
    {
        Body.Restore();
        Body.Position = Home;
        State = BotState.Idle;
        _idleLeft = (float)(_random.NextDouble() * 2.0);
    }

    /// <summary>Decide qué hacer: si hay un monstruo cerca, va a por él.</summary>
    private void Think(IReadOnlyList<Monster> monsters)
    {
        var target = NearestMonster(monsters);
        if (target is not null)
        {
            State = Distance(target.Body.Position) <= AttackRange ? BotState.Attacking : BotState.Chasing;
            return;
        }

        if (State is BotState.Chasing or BotState.Attacking)
        {
            State = BotState.Idle;
            _idleLeft = (float)(_random.NextDouble() * 1.5);
        }
    }

    private void Idle(float seconds)
    {
        Action = BotAction.Idle;
        _idleLeft -= seconds;
        if (_idleLeft > 0f)
        {
            return;
        }

        State = BotState.Walking;
        _destination = RandomPointAroundHome();
        _path = null;
    }

    private void Walk(float seconds, Func<float, float, float> ground, Func<int, int, bool>? walkable)
    {
        Action = BotAction.Walking;
        if (StepTowards(_destination, seconds, walkable))
        {
            State = BotState.Idle;
            _idleLeft = (float)(0.5 + (_random.NextDouble() * 2.5));
        }

        _ = ground;
    }

    private void Chase(float seconds, Func<float, float, float> ground, IReadOnlyList<Monster> monsters, Func<int, int, bool>? walkable)
    {
        var target = NearestMonster(monsters);
        if (target is null)
        {
            State = BotState.Idle;
            _idleLeft = 0.4f;
            return;
        }

        Action = BotAction.Walking;
        if (Distance(target.Body.Position) <= AttackRange)
        {
            State = BotState.Attacking;
            return;
        }

        StepTowards(target.Body.Position, seconds, walkable);
        _ = ground;
    }

    private void Attack(float seconds, IReadOnlyList<Monster> monsters, Action<Monster>? onKill)
    {
        _ = seconds;
        var target = NearestMonster(monsters);
        if (target is null)
        {
            State = BotState.Idle;
            _idleLeft = 0.4f;
            return;
        }

        FaceTowards(target.Body.Position);

        if (Body.AttackCooldown > 0f)
        {
            Action = BotAction.Idle;
            return;
        }

        Body.AttackCooldown = AttackInterval;
        Body.AttackAnimationLeft = 0.6f;
        Action = BotAction.Attacking;

        var killed = target.TakeDamage(Body.Attack);
        if (!target.Body.IsAlive)
        {
            onKill?.Invoke(target);
        }
    }

    /// <summary>El monstruo vivo más cercano dentro del alcance de la vista.</summary>
    private Monster? NearestMonster(IReadOnlyList<Monster> monsters)
    {
        Monster? best = null;
        var bestDistance = SightRange;

        foreach (var monster in monsters)
        {
            if (!monster.Body.IsAlive)
            {
                continue;
            }

            var distance = Distance(monster.Body.Position);
            if (distance <= bestDistance)
            {
                best = monster;
                bestDistance = distance;
            }
        }

        return best;
    }

    /// <summary>Anda hacia un punto. Devuelve cierto si ya ha llegado.</summary>
    private bool StepTowards(Vector3 destination, float seconds, Func<int, int, bool>? walkable)
    {
        var delta = destination - Body.Position;
        delta.Y = 0f;
        var distance = delta.Length();
        if (distance <= 0.05f)
        {
            return true;
        }

        var waypoint = NextWaypoint(destination, walkable);

        var toWaypoint = waypoint - Body.Position;
        toWaypoint.Y = 0f;
        var toWaypointDistance = toWaypoint.Length();
        if (toWaypointDistance <= 0.05f)
        {
            return false;
        }

        var direction = toWaypoint / toWaypointDistance;
        FaceTowards(waypoint);

        var advance = MathF.Min(WalkSpeed * seconds, distance);
        Body.Position += direction * advance;
        return advance >= distance - 0.05f;
    }

    /// <summary>
    /// El punto al que hay que apuntar ahora mismo. Si hay un camino trazado y
    /// aún queda por recorrer, es su siguiente esquina; si no, se traza uno
    /// nuevo con A* y se devuelve el primero. Sin camino (rincón cerrado), se va
    /// en línea recta, que es mejor que quedarse parado.
    /// </summary>
    private Vector3 NextWaypoint(Vector3 destination, Func<int, int, bool>? walkable)
    {
        if (_path is not null)
        {
            if (_pathIndex < _path.Count)
            {
                var step = _path[_pathIndex];
                var point = new Vector3(step.X, 0f, step.Y);
                var flat = new Vector2(point.X - Body.Position.X, point.Z - Body.Position.Z);
                if (flat.Length() <= 0.4f)
                {
                    _pathIndex++;
                    return NextWaypoint(destination, walkable);
                }

                return point;
            }

            _path = null;
        }

        if (walkable is null)
        {
            return destination;
        }

        var start = new Vector2(Body.Position.X, Body.Position.Z);
        var goal = new Vector2(destination.X, destination.Z);
        _path = Pathfinding.FindPath(start, goal, walkable);
        _pathIndex = 0;

        if (_path is null || _path.Count == 0)
        {
            _path = null;
            return destination;
        }

        return NextWaypoint(destination, walkable);
    }

    private void FaceTowards(Vector3 point)
    {
        var delta = point - Body.Position;
        if (delta.LengthSquared() < 0.0001f)
        {
            return;
        }

        Body.HeadingDegrees = MathF.Atan2(delta.Z, delta.X) * 180f / MathF.PI;
    }

    private float Distance(Vector3 point) => Vector3.Distance(Body.Position, point);

    private Vector3 RandomPointAroundHome()
    {
        var angle = _random.NextDouble() * Math.Tau;
        var radius = _random.NextDouble() * RoamRadius;
        return new Vector3(
            Home.X + (float)(Math.Cos(angle) * radius),
            Home.Y,
            Home.Z + (float)(Math.Sin(angle) * radius));
    }
}

/// <summary>La acción que reproduce el modelo de un bot.</summary>
public enum BotAction
{
    /// <summary>Quieto.</summary>
    Idle,

    /// <summary>Andando.</summary>
    Walking,

    /// <summary>Pegando.</summary>
    Attacking,

    /// <summary>Muerto.</summary>
    Dead,
}
