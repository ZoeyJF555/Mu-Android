namespace Mu.Core.Rendering;

/// <summary>
/// Un rectángulo en píxeles del lienzo o de la pantalla. Está aquí, y no en
/// MonoGame, para que la colocación del interfaz se pueda probar sin dibujar.
/// </summary>
/// <param name="X">Coordenada X de la esquina superior izquierda.</param>
/// <param name="Y">Coordenada Y de la esquina superior izquierda.</param>
/// <param name="Width">Ancho.</param>
/// <param name="Height">Alto.</param>
public readonly record struct CanvasRect(int X, int Y, int Width, int Height)
{
    /// <summary>Coordenada X del borde derecho (no incluido).</summary>
    public int Right => X + Width;

    /// <summary>Coordenada Y del borde inferior (no incluido).</summary>
    public int Bottom => Y + Height;

    /// <summary>Coordenada X del centro.</summary>
    public int CenterX => X + (Width / 2);

    /// <summary>Coordenada Y del centro.</summary>
    public int CenterY => Y + (Height / 2);

    /// <summary>Cierto si el punto cae dentro del rectángulo.</summary>
    /// <param name="x">Coordenada X del punto.</param>
    /// <param name="y">Coordenada Y del punto.</param>
    public bool Contains(int x, int y) => x >= X && x < Right && y >= Y && y < Bottom;
}
