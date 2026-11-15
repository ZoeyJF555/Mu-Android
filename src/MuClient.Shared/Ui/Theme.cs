using Microsoft.Xna.Framework;
using Mu.Client.Rendering;

namespace Mu.Client.Ui;

/// <summary>
/// Los colores y el dibujo de las piezas comunes de la interfaz: paneles,
/// títulos y botones. Están juntos para que todas las pantallas se vean igual y
/// para no repetir el mismo rectángulo en cada una.
/// </summary>
public static class Theme
{
    /// <summary>Color del marco de los paneles.</summary>
    public static readonly Color PanelBorder = new(168, 138, 84);

    /// <summary>Color del fondo de los paneles.</summary>
    public static readonly Color PanelFill = new(12, 10, 8, 226);

    /// <summary>Color del fondo de los botones.</summary>
    public static readonly Color ButtonFill = new(38, 32, 24, 235);

    /// <summary>Color del fondo de un botón pulsado.</summary>
    public static readonly Color ButtonPressed = new(96, 78, 44, 245);

    /// <summary>Color del texto normal.</summary>
    public static readonly Color Text = new(236, 224, 190);

    /// <summary>Color del texto secundario.</summary>
    public static readonly Color TextDim = new(170, 158, 130);

    /// <summary>Color del título.</summary>
    public static readonly Color Title = new(238, 200, 110);

    /// <summary>Rojo de la vida.</summary>
    public static readonly Color Health = new(196, 32, 32);

    /// <summary>Azul del maná.</summary>
    public static readonly Color Mana = new(40, 96, 208);

    /// <summary>Amarillo de la experiencia.</summary>
    public static readonly Color Experience = new(214, 182, 48);

    /// <summary>Dibuja un panel con su marco.</summary>
    public static void Panel(TextRenderer renderer, Rectangle bounds, Color? fill = null)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        renderer.Fill(bounds, fill ?? PanelFill);
        renderer.Frame(bounds, PanelBorder, 2);
    }

    /// <summary>Dibuja un botón, resaltado si está pulsado.</summary>
    public static void Button(TextRenderer renderer, Rectangle bounds, string label, bool pressed, float scale = 2f)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        renderer.Fill(bounds, pressed ? ButtonPressed : ButtonFill);
        renderer.Frame(bounds, PanelBorder, 2);
        renderer.TextCentered(label, bounds, scale, pressed ? Color.White : Text);
    }

    /// <summary>Dibuja una barra con su relleno proporcional y su texto encima.</summary>
    public static void Bar(
        TextRenderer renderer,
        Rectangle bounds,
        float fraction,
        Color color,
        string label)
    {
        ArgumentNullException.ThrowIfNull(renderer);

        renderer.Fill(bounds, new Color(8, 8, 10, 220));
        var filled = (int)MathHelper.Clamp(bounds.Width * fraction, 0f, bounds.Width);
        if (filled > 0)
        {
            renderer.Fill(new Rectangle(bounds.X, bounds.Y, filled, bounds.Height), color);
        }

        renderer.Frame(bounds, new Color(150, 130, 90), 1);
        if (!string.IsNullOrEmpty(label))
        {
            renderer.TextCentered(label, bounds, 1.4f, Color.White);
        }
    }
}
