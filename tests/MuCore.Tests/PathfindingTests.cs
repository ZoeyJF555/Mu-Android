using System.Numerics;
using Mu.Core.Game;
using Xunit;

namespace Mu.Core.Tests;

/// <summary>Comprueba que el buscador de caminos rodea los obstáculos, sin dibujar nada.</summary>
public class PathfindingTests
{
    /// <summary>Mapa abierto, con un muro de casillas bloqueadas donde se diga.</summary>
    private static Func<int, int, bool> OpenExcept(params (int X, int Y)[] blocked)
    {
        var set = blocked.ToHashSet();
        return (x, y) => !set.Contains((x, y));
    }

    [Fact]
    public void EnLineaRectaVaDirecto()
    {
        var path = Pathfinding.FindPath(new Vector2(0.5f, 0.5f), new Vector2(5.5f, 0.5f), OpenExcept());

        Assert.NotNull(path);
        Assert.Equal(5, path!.Count);
        Assert.Equal(new Vector2(5.5f, 0.5f), path[^1]);
    }

    [Fact]
    public void RodeaUnMuroEnVezDeAtravesarlo()
    {
        // Un muro vertical de tres casillas en x = 3, de y = 0 a y = 2, con paso
        // libre por arriba (y = 3).
        var blocked = new (int, int)[] { (3, 0), (3, 1), (3, 2) };
        var walkable = OpenExcept(blocked);

        var path = Pathfinding.FindPath(new Vector2(1.5f, 0.5f), new Vector2(5.5f, 0.5f), walkable);

        Assert.NotNull(path);
        Assert.DoesNotContain(path!, step => blocked.Contains(
            ((int)MathF.Floor(step.X), (int)MathF.Floor(step.Y))));
        Assert.Equal(new Vector2(5.5f, 0.5f), path[^1]);
    }

    [Fact]
    public void SinCaminoDevuelveNulo()
    {
        // El destino está encerrado por un muro de cuatro lados.
        var wall = new (int, int)[]
        {
            (2, 1), (2, 2), (2, 3), (4, 1), (4, 2), (4, 3), (3, 0), (3, 4),
        };
        var walkable = OpenExcept(wall);

        var path = Pathfinding.FindPath(new Vector2(0.5f, 0.5f), new Vector2(3.5f, 2.5f), walkable);

        Assert.Null(path);
    }

    [Fact]
    public void NoCortaLaEsquinaDeUnaPared()
    {
        // Dos muros en diagonal: la casilla de en medio está libre, pero pasar
        // por ella cortaría la esquina, así que no debe usarse.
        var blocked = new (int, int)[] { (1, 0), (0, 1) };
        var walkable = OpenExcept(blocked);

        var path = Pathfinding.FindPath(new Vector2(0.5f, 0.5f), new Vector2(1.5f, 1.5f), walkable);

        Assert.NotNull(path);
        // El paso en diagonal de (0,0) a (1,1) no vale; hay que dar un rodeo.
        Assert.True(path!.Count > 1, "no debe ir en diagonal pegando a las paredes");
    }

    [Fact]
    public void SeRindeSiLaBusquedaEsDemasiadoLarga()
    {
        // Laberinto enorme sin salida hacia el destino: el tope de casillas corta.
        var walkable = OpenExcept((255, 255));
        var path = Pathfinding.FindPath(new Vector2(0.5f, 0.5f), new Vector2(255.5f, 255.5f), walkable, nodeLimit: 10);

        Assert.Null(path);
    }
}
