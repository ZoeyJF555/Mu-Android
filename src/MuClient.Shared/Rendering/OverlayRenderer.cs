using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Mu.Client.Rendering;

/// <summary>
/// Dibuja texto sobre el juego. El proyecto no usa el empaquetador de contenido
/// de MonoGame, así que en vez de cargar una fuente se crea una textura de un
/// píxel y se pinta cada letra con <see cref="BitmapFont"/>. Se usa para avisar
/// de los fallos al cargar, que en un móvil no se pueden ver de otra forma.
/// </summary>
public sealed class OverlayRenderer : IDisposable
{
    private readonly GraphicsDevice _device;
    private readonly SpriteBatch _batch;
    private readonly Texture2D _pixel;

    public OverlayRenderer(GraphicsDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);

        _device = device;
        _batch = new SpriteBatch(device);
        _pixel = new Texture2D(device, 1, 1);
        _pixel.SetData([Color.White]);
    }

    /// <summary>
    /// Escribe un texto sobre un fondo oscuro para que se lea siempre, partiendo
    /// las líneas que no quepan en el ancho de la pantalla.
    /// </summary>
    /// <param name="text">Texto a mostrar.</param>
    /// <param name="position">Esquina superior izquierda.</param>
    /// <param name="scale">Tamaño de cada letra.</param>
    /// <param name="color">Color del texto.</param>
    public void DrawText(string text, Point position, int scale = 2, Color? color = null)
    {
        if (string.IsNullOrEmpty(text) || scale < 1)
        {
            return;
        }

        var lines = Wrap(text, Math.Max(1, (_device.Viewport.Width - position.X - (8 * scale)) / ((BitmapFont.Width + BitmapFont.Spacing) * scale)));
        var lineHeight = (BitmapFont.Height + 2) * scale;
        var width = 0;
        foreach (var line in lines)
        {
            width = Math.Max(width, BitmapFont.MeasureWidth(line, scale));
        }

        var padding = 4 * scale;
        var background = new Rectangle(
            position.X - padding,
            position.Y - padding,
            width + (padding * 2),
            (lines.Count * lineHeight) + (padding * 2));

        _batch.Begin(samplerState: SamplerState.PointClamp);
        _batch.Draw(_pixel, background, new Color(0, 0, 0, 200));
        _batch.End();

        var foreground = color ?? new Color(255, 235, 200);
        _batch.Begin(samplerState: SamplerState.PointClamp);
        for (var index = 0; index < lines.Count; index++)
        {
            var originY = position.Y + (index * lineHeight);
            BitmapFont.ForEachPixel(lines[index], scale, (x, y) =>
                _batch.Draw(_pixel, new Rectangle(position.X + x, originY + y, 1, 1), foreground));
        }

        _batch.End();
    }

    /// <summary>
    /// Copia a la pantalla una imagen ya dibujada, estirada al rectángulo que se
    /// indique. Se usa para volcar el lienzo del juego conservando la proporción,
    /// que es lo que evita que se vea deformado en pantallas alargadas.
    /// </summary>
    public void DrawTexture(Texture2D texture, Rectangle destination)
    {
        ArgumentNullException.ThrowIfNull(texture);

        _batch.Begin(samplerState: SamplerState.LinearClamp);
        _batch.Draw(texture, destination, Color.White);
        _batch.End();
    }

    /// <summary>Dibuja un rectángulo relleno.</summary>
    public void DrawBox(Rectangle rectangle, Color color)
    {
        _batch.Begin(samplerState: SamplerState.PointClamp);
        _batch.Draw(_pixel, rectangle, color);
        _batch.End();
    }

    /// <summary>Dibuja un círculo relleno, para el mando y los botones.</summary>
    public void DrawCircle(Vector2 center, float radius, Color color)
    {
        var size = (int)(radius * 2f);
        var bounds = new Rectangle((int)(center.X - radius), (int)(center.Y - radius), size, size);

        _batch.Begin(samplerState: SamplerState.PointClamp);
        for (var y = 0; y < size; y++)
        {
            var dy = y - radius;
            var half = MathF.Sqrt(MathF.Max(0f, (radius * radius) - (dy * dy)));
            if (half <= 0f)
            {
                continue;
            }

            var left = (int)(center.X - half);
            var width = Math.Max(1, (int)(half * 2f));
            _batch.Draw(_pixel, new Rectangle(left, bounds.Y + y, width, 1), color);
        }

        _batch.End();
    }

    /// <summary>Parte un texto en líneas que quepan en un ancho dado en letras.</summary>
    private static List<string> Wrap(string text, int columns)
    {
        var lines = new List<string>();
        var current = new System.Text.StringBuilder();

        foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (current.Length > 0 && current.Length + 1 + word.Length > columns)
            {
                lines.Add(current.ToString());
                current.Clear();
            }

            if (current.Length > 0)
            {
                current.Append(' ');
            }

            current.Append(word);
        }

        if (current.Length > 0)
        {
            lines.Add(current.ToString());
        }

        return lines;
    }

    public void Dispose()
    {
        _pixel.Dispose();
        _batch.Dispose();
    }
}
