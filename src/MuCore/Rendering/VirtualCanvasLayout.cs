using System.Numerics;

namespace Mu.Core.Rendering;

/// <summary>Dónde y con qué tamaño se coloca el lienzo del juego dentro de la pantalla.</summary>
/// <param name="X">Coordenada X de la esquina superior izquierda.</param>
/// <param name="Y">Coordenada Y de la esquina superior izquierda.</param>
/// <param name="Width">Ancho que ocupa el lienzo, en píxeles de la pantalla.</param>
/// <param name="Height">Alto que ocupa el lienzo, en píxeles de la pantalla.</param>
public readonly record struct CanvasPlacement(int X, int Y, int Width, int Height);

/// <summary>Tamaño del lienzo virtual, en píxeles.</summary>
/// <param name="Width">Ancho.</param>
/// <param name="Height">Alto.</param>
public readonly record struct CanvasSize(int Width, int Height);

/// <summary>
/// Las cuentas del lienzo virtual, sin depender de MonoGame. El juego se dibuja
/// siempre sobre un lienzo del mismo alto (720 píxeles) y luego se coloca
/// escalado en la pantalla conservando la proporción: así se ve igual de grande
/// en un móvil pequeño que en uno enorme, y ningún panel sale diminuto.
///
/// El ancho del lienzo se ajusta a la forma de la pantalla, dentro de unos
/// límites, para no dejar barras negras en los móviles alargados de hoy. El
/// contenido se escala por igual en los dos ejes, así que nunca sale deformado.
/// Está aparte del dibujo para poder probarlo sin tarjeta gráfica.
/// </summary>
public static class VirtualCanvasLayout
{
    /// <summary>Alto del lienzo, en píxeles. Es fijo, para que el mundo se vea igual siempre.</summary>
    public const int Height = 720;

    /// <summary>Ancho del lienzo cuando no se sabe el tamaño de la pantalla.</summary>
    public const int DefaultWidth = 1280;

    /// <summary>Ancho mínimo del lienzo. Por debajo, el interfaz no cabría.</summary>
    public const int MinimumWidth = 1024;

    /// <summary>Ancho máximo del lienzo. Por encima, el interfaz quedaría desperdigado.</summary>
    public const int MaximumWidth = 1920;

    /// <summary>
    /// Elige el ancho del lienzo a partir de la forma de la pantalla. Un móvil
    /// alargado da un lienzo más ancho, para que no queden barras negras a los
    /// lados; uno cuadrado, uno más estrecho. El alto no cambia nunca.
    /// </summary>
    /// <param name="screenWidth">Ancho de la pantalla, en píxeles.</param>
    /// <param name="screenHeight">Alto de la pantalla, en píxeles.</param>
    public static CanvasSize Choose(int screenWidth, int screenHeight)
    {
        if (screenWidth <= 0 || screenHeight <= 0)
        {
            return new CanvasSize(DefaultWidth, Height);
        }

        var width = (int)MathF.Round(Height * (screenWidth / (float)screenHeight));
        width = Math.Clamp(width, MinimumWidth, MaximumWidth);

        // Un ancho par evita que el lienzo quede con medio píxel en un lado.
        width -= width % 2;

        return new CanvasSize(width, Height);
    }

    /// <summary>
    /// Dónde se coloca el lienzo dentro de una pantalla. Si la forma de la
    /// pantalla no coincide con la del lienzo quedan barras a los lados o arriba
    /// y abajo, pero el contenido nunca se deforma.
    /// </summary>
    /// <param name="canvasWidth">Ancho del lienzo, en píxeles.</param>
    /// <param name="canvasHeight">Alto del lienzo, en píxeles.</param>
    /// <param name="screenWidth">Ancho de la pantalla, en píxeles.</param>
    /// <param name="screenHeight">Alto de la pantalla, en píxeles.</param>
    public static CanvasPlacement Place(int canvasWidth, int canvasHeight, int screenWidth, int screenHeight)
    {
        if (canvasWidth <= 0 || canvasHeight <= 0 || screenWidth <= 0 || screenHeight <= 0)
        {
            return new CanvasPlacement(0, 0, canvasWidth, canvasHeight);
        }

        var scale = MathF.Min(screenWidth / (float)canvasWidth, screenHeight / (float)canvasHeight);
        var width = Math.Max(1, (int)MathF.Round(canvasWidth * scale));
        var height = Math.Max(1, (int)MathF.Round(canvasHeight * scale));

        return new CanvasPlacement((screenWidth - width) / 2, (screenHeight - height) / 2, width, height);
    }

    /// <summary>
    /// Pasa un punto de la pantalla a coordenadas del lienzo. Hace falta porque
    /// el dedo llega en píxeles de la pantalla del móvil y los botones se
    /// dibujan en el lienzo: sin esto, los toques caerían donde no se ven.
    /// </summary>
    /// <param name="x">Coordenada X en la pantalla.</param>
    /// <param name="y">Coordenada Y en la pantalla.</param>
    /// <param name="canvasWidth">Ancho del lienzo, en píxeles.</param>
    /// <param name="canvasHeight">Alto del lienzo, en píxeles.</param>
    /// <param name="screenWidth">Ancho de la pantalla, en píxeles.</param>
    /// <param name="screenHeight">Alto de la pantalla, en píxeles.</param>
    public static Vector2 ToCanvas(
        float x,
        float y,
        int canvasWidth,
        int canvasHeight,
        int screenWidth,
        int screenHeight)
    {
        var placement = Place(canvasWidth, canvasHeight, screenWidth, screenHeight);
        if (placement.Width <= 0 || placement.Height <= 0)
        {
            return Vector2.Zero;
        }

        return new Vector2(
            (x - placement.X) * canvasWidth / placement.Width,
            (y - placement.Y) * canvasHeight / placement.Height);
    }
}
