using Microsoft.Xna.Framework;
using Mu.Client.Rendering;
using Mu.Core.Rendering;

namespace Mu.Client.Ui;

/// <summary>
/// El teclado que se dibuja dentro del juego para escribir la cuenta, la
/// contraseña y el nombre del personaje. MonoGame para Android, en la versión
/// que usa este proyecto, no ofrece ninguna forma de abrir el teclado del
/// sistema ni de recoger lo que se escribe en él, así que el juego trae el suyo:
/// así se puede escribir igual en cualquier móvil, sin depender de nada de
/// fuera, y las teclas son lo bastante grandes para el dedo.
///
/// La colocación de las teclas está en <see cref="OnScreenKeyboard"/>, que no
/// depende de MonoGame y por eso se prueba sin pantalla.
/// </summary>
public static class KeyboardPanel
{
    /// <summary>Lo que ha pedido el jugador al teclado en este fotograma.</summary>
    /// <param name="Enter">Cierto si ha pulsado "LISTO".</param>
    public readonly record struct Result(bool Enter);

    /// <summary>
    /// Reparte los toques entre las teclas y escribe en el campo activo.
    /// </summary>
    /// <param name="input">Lo que está tocando el jugador.</param>
    /// <param name="target">El campo donde se escribe.</param>
    /// <param name="width">Ancho del lienzo, en píxeles.</param>
    /// <param name="height">Alto del lienzo, en píxeles.</param>
    public static Result Update(InputState input, TextInput target, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(target);

        var keys = OnScreenKeyboard.Build(width, height);
        var enter = false;

        foreach (var point in input.Points)
        {
            if (!input.IsNew(point))
            {
                continue;
            }

            var hit = OnScreenKeyboard.Hit(keys, point.X, point.Y);
            if (hit is not { } key)
            {
                continue;
            }

            switch (key.Action)
            {
                case KeyboardAction.Character:
                    target.Append(key.Value);
                    break;
                case KeyboardAction.Backspace:
                    target.Backspace();
                    break;
                case KeyboardAction.Enter:
                    enter = true;
                    break;
            }
        }

        return new Result(enter);
    }

    /// <summary>Dibuja el teclado, resaltando las teclas que se están pulsando.</summary>
    /// <param name="renderer">Con qué se dibuja.</param>
    /// <param name="input">Lo que está tocando el jugador.</param>
    /// <param name="width">Ancho del lienzo, en píxeles.</param>
    /// <param name="height">Alto del lienzo, en píxeles.</param>
    public static void Draw(TextRenderer renderer, InputState input, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentNullException.ThrowIfNull(input);

        var area = OnScreenKeyboard.Area(width, height);
        renderer.Fill(Rect(area), new Color(10, 12, 18, 240));

        foreach (var key in OnScreenKeyboard.Build(width, height))
        {
            var bounds = Rect(key.Bounds);
            var down = input.Holding(bounds);
            var isAction = key.Action != KeyboardAction.Character;

            renderer.Fill(bounds, down ? Theme.ButtonPressed : Theme.ButtonFill);
            renderer.Frame(bounds, isAction ? Theme.Title : Theme.PanelBorder, 2);
            renderer.TextCentered(key.Label, bounds, isAction ? 1.6f : 2.2f, down ? Color.White : Theme.Text);
        }
    }

    private static Rectangle Rect(CanvasRect rect) => new(rect.X, rect.Y, rect.Width, rect.Height);
}
