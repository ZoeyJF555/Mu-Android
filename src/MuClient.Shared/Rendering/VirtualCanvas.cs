using Microsoft.Xna.Framework;
using Mu.Core.Rendering;

namespace Mu.Client.Rendering;

/// <summary>
/// El lienzo sobre el que se dibuja el juego. Todas las pantallas se pintan
/// siempre sobre este lienzo (alto fijo de 720 píxeles, ancho según la forma de
/// la pantalla) y luego se coloca escalado en el móvil: así ningún panel sale
/// diminuto ni deformado, dé igual el tamaño del teléfono.
///
/// El tamaño se ajusta al arrancar y cada vez que la pantalla cambia de tamaño
/// (en Android la superficie real puede conocerse un poco después), siempre
/// antes de dibujar. A partir de ahí solo se lee. Las cuentas están en
/// <see cref="VirtualCanvasLayout"/>, que no depende de MonoGame y por eso se
/// puede probar.
/// </summary>
public static class VirtualCanvas
{
    /// <summary>Ancho del lienzo, en píxeles.</summary>
    public static int Width { get; private set; } = VirtualCanvasLayout.DefaultWidth;

    /// <summary>Alto del lienzo, en píxeles.</summary>
    public static int Height { get; private set; } = VirtualCanvasLayout.Height;

    /// <summary>Proporción del lienzo, que es la que usa la cámara siempre.</summary>
    public static float Aspect => Width / (float)Height;

    /// <summary>
    /// Ajusta el lienzo a la forma de la pantalla. Se llama una vez, al arrancar
    /// y cada vez que cambia el tamaño de la ventana, siempre antes de dibujar.
    /// </summary>
    /// <param name="screenWidth">Ancho de la pantalla, en píxeles.</param>
    /// <param name="screenHeight">Alto de la pantalla, en píxeles.</param>
    public static void Configure(int screenWidth, int screenHeight)
    {
        var size = VirtualCanvasLayout.Choose(screenWidth, screenHeight);
        Width = size.Width;
        Height = size.Height;
    }

    /// <summary>Dónde se coloca el lienzo dentro de una pantalla.</summary>
    /// <param name="screenWidth">Ancho de la pantalla, en píxeles.</param>
    /// <param name="screenHeight">Alto de la pantalla, en píxeles.</param>
    public static Rectangle Destination(int screenWidth, int screenHeight)
    {
        var placement = VirtualCanvasLayout.Place(Width, Height, screenWidth, screenHeight);
        return new Rectangle(placement.X, placement.Y, placement.Width, placement.Height);
    }

    /// <summary>Pasa un punto de la pantalla a coordenadas del lienzo.</summary>
    /// <param name="x">Coordenada X en la pantalla.</param>
    /// <param name="y">Coordenada Y en la pantalla.</param>
    /// <param name="screenWidth">Ancho de la pantalla, en píxeles.</param>
    /// <param name="screenHeight">Alto de la pantalla, en píxeles.</param>
    public static Vector2 ToCanvas(float x, float y, int screenWidth, int screenHeight) =>
        VirtualCanvasLayout.ToCanvas(x, y, Width, Height, screenWidth, screenHeight);

    /// <summary>
    /// La matriz que coloca el lienzo en la pantalla: lleva lo que se dibuja en
    /// coordenadas del lienzo a los píxeles del móvil, escalado por igual en los
    /// dos ejes. Es lo que hace que el interfaz ocupe toda la pantalla en vez de
    /// salir en una esquina.
    /// </summary>
    /// <param name="screenWidth">Ancho de la pantalla, en píxeles.</param>
    /// <param name="screenHeight">Alto de la pantalla, en píxeles.</param>
    public static Matrix Transform(int screenWidth, int screenHeight)
    {
        var placement = VirtualCanvasLayout.Place(Width, Height, screenWidth, screenHeight);
        var scaleX = placement.Width / (float)Width;
        var scaleY = placement.Height / (float)Height;

        return Matrix.CreateScale(scaleX, scaleY, 1f)
            * Matrix.CreateTranslation(placement.X, placement.Y, 0f);
    }
}
