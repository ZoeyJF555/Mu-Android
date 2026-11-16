using System.Numerics;

namespace Mu.Core.Game;

/// <summary>
/// Busca el camino más corto entre dos puntos del mapa, casilla a casilla, con
/// el algoritmo A*. Es lo que usan los bots para rodear un árbol o una casa en
/// vez de atravesarlos: sin esto andaban en línea recta y se metían dentro de
/// todo lo que pillaban. Es lógica pura (solo números), así que se puede probar
/// sin dibujar nada.
/// </summary>
public static class Pathfinding
{
    /// <summary>
    /// Casillas que se pueden mirar como mucho antes de rendirse. En un mapa de
    /// 256 × 256 casillas una búsqueda larga recorrería decenas de miles; con
    /// este tope se corta y el bot vuelve al camino recto, para no gastar el
    /// fotograma entero en un rincón sin salida.
    /// </summary>
    public const int DefaultNodeLimit = 6000;

    private static readonly (int X, int Y)[] Directions =
    [
        (0, -1), (0, 1), (-1, 0), (1, 0),
        (-1, -1), (1, -1), (-1, 1), (1, 1),
    ];

    /// <summary>
    /// El camino desde <paramref name="start"/> hasta <paramref name="goal"/>,
    /// en casillas (sin incluir la de salida). Devuelve null si no hay camino o
    /// si la búsqueda supera el tope de casillas miradas.
    /// </summary>
    /// <param name="start">Casilla de salida.</param>
    /// <param name="goal">Casilla de destino.</param>
    /// <param name="walkable">Cierto si una casilla se puede pisar.</param>
    /// <param name="nodeLimit">Cuántas casillas se pueden mirar como mucho.</param>
    public static List<Vector2>? FindPath(
        Vector2 start,
        Vector2 goal,
        Func<int, int, bool> walkable,
        int nodeLimit = DefaultNodeLimit)
    {
        ArgumentNullException.ThrowIfNull(walkable);

        var startCell = (X: (int)MathF.Floor(start.X), Y: (int)MathF.Floor(start.Y));
        var goalCell = (X: (int)MathF.Floor(goal.X), Y: (int)MathF.Floor(goal.Y));

        if (startCell == goalCell)
        {
            return [];
        }

        if (!walkable(goalCell.X, goalCell.Y))
        {
            return null;
        }

        var open = new PriorityQueue<(int X, int Y), float>();
        var cameFrom = new Dictionary<(int X, int Y), (int X, int Y)>();
        var cost = new Dictionary<(int X, int Y), float> { [startCell] = 0f };
        var seen = new HashSet<(int X, int Y)>();

        open.Enqueue(startCell, Heuristic(startCell, goalCell));

        var visited = 0;
        while (open.Count > 0)
        {
            var current = open.Dequeue();
            if (!seen.Add(current))
            {
                continue;
            }

            if (current == goalCell)
            {
                return Retrace(cameFrom, current);
            }

            if (++visited > nodeLimit)
            {
                return null;
            }

            foreach (var (dx, dy) in Directions)
            {
                var next = (X: current.X + dx, Y: current.Y + dy);
                if (seen.Contains(next) || !walkable(next.X, next.Y))
                {
                    continue;
                }

                // En diagonal solo se pasa si las dos casillas de al lado también
                // se pueden pisar: si no, el bot cortaría la esquina de una pared.
                if (dx != 0 && dy != 0 &&
                    (!walkable(current.X + dx, current.Y) || !walkable(current.X, current.Y + dy)))
                {
                    continue;
                }

                var step = dx != 0 && dy != 0 ? 1.41421356f : 1f;
                var tentative = cost[current] + step;

                if (!cost.TryGetValue(next, out var known) || tentative < known)
                {
                    cost[next] = tentative;
                    cameFrom[next] = current;
                    open.Enqueue(next, tentative + Heuristic(next, goalCell));
                }
            }
        }

        return null;
    }

    private static List<Vector2> Retrace(
        Dictionary<(int X, int Y), (int X, int Y)> cameFrom,
        (int X, int Y) current)
    {
        var path = new List<Vector2>();
        while (cameFrom.TryGetValue(current, out var previous))
        {
            // Se apunta al centro de la casilla, que es por donde se anda.
            path.Add(new Vector2(current.X + 0.5f, current.Y + 0.5f));
            current = previous;
        }

        path.Reverse();
        return path;
    }

    /// <summary>Distancia en línea recta, que nunca sobreestima el coste real.</summary>
    private static float Heuristic((int X, int Y) a, (int X, int Y) b)
    {
        var dx = Math.Abs(a.X - b.X);
        var dy = Math.Abs(a.Y - b.Y);
        return (dx + dy) + (1.41421356f - 2f) * Math.Min(dx, dy);
    }
}
