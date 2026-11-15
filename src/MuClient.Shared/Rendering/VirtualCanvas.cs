using Microsoft.Xna.Framework;

namespace Mu.Client.Rendering;

/// <summary>
/// El lienzo sobre el que se dibuja el juego. Siempre mide lo mismo, así que el
/// mundo se ve igual en cualquier pantalla: al final se coloca en el móvil
/// conservando la proporción y con barras negras donde no llega. Sin esto, el
/// contenido de 16:9 se estiraba a pantallas más alargadas y todo salía deformado.
/// </summary>
public static class VirtualCanvas
{
    /// <summary>Ancho del lienzo, en píxeles.</summary>
    public const int Width = 1280;

    /// <summary>Alto del lienzo, en píxeles.</summary>
    public const int Height = 720;

    /// <summary>Proporción del lienzo, que es la que usa la cámara siempre.</summary>
    public const float Aspect = (float)Width / Height;

    /// <summary>
    /// Dónde se coloca el lienzo dentro de una pantalla. Si la pantalla es más
    /// alargada que el lienzo quedan barras a los lados; si es más cuadrada,
    /// arriba y abajo. La matemática está aparte del dibujo para poder probarla.
    /// </summary>
    /// <param name="screenWidth">Ancho de la pantalla, en píxeles.</param>
    /// <param name="screenHeight">Alto de la pantalla, en píxeles.</param>
    public static Rectangle Destination(int screenWidth, int screenHeight)
    {
        if (screenWidth <= 0 || screenHeight <= 0)
        {
            return new Rectangle(0, 0, Width, Height);
        }

        var scale = MathF.Min(screenWidth / (float)Width, screenHeight / (float)Height);
        var width = Math.Max(1, (int)MathF.Round(Width * scale));
        var height = Math.Max(1, (int)MathF.Round(Height * scale));

        return new Rectangle((screenWidth - width) / 2, (screenHeight - height) / 2, width, height);
    }

    /// <summary>
    /// Pasa un punto de la pantalla a coordenadas del lienzo. Hace falta porque
    /// el dedo llega en píxeles de la pantalla del móvil, y los botones se
    /// dibujan en el lienzo: sin esto, los toques caerían donde no se ven.
    /// </summary>
    /// <param name="x">Coordenada X en la pantalla.</param>
    /// <param name="y">Coordenada Y en la pantalla.</param>
    /// <param name="screenWidth">Ancho de la pantalla, en píxeles.</param>
    /// <param name="screenHeight">Alto de la pantalla, en píxeles.</param>
    public static Vector2 ToCanvas(float x, float y, int screenWidth, int screenHeight)
    {
        var destination = Destination(screenWidth, screenHeight);
        if (destination.Width <= 0 || destination.Height <= 0)
        {
            return Vector2.Zero;
        }

        return new Vector2(
            (x - destination.X) * Width / destination.Width,
            (y - destination.Y) * Height / destination.Height);
    }
}
