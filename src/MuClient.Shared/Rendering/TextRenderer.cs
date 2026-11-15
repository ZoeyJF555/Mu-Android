using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Mu.Client.Rendering;

/// <summary>
/// Dibuja la interfaz: texto, paneles, barras y círculos. El proyecto no usa el
/// empaquetador de contenido de MonoGame, así que el atlas de letras se monta al
/// arrancar a partir de <see cref="BitmapFont"/>. Se dibuja todo dentro de un
/// mismo lote por fotograma: con el HUD lleno de números, abrir y cerrar el lote
/// en cada etiqueta se notaba en el móvil.
/// </summary>
public sealed class TextRenderer : IDisposable
{
    private const int Columns = 16;
    private const int Rows = 8;
    private const int Padding = 2;

    private readonly SpriteBatch _batch;
    private readonly Texture2D _atlas;
    private readonly Texture2D _pixel;
    private readonly Dictionary<char, Rectangle> _cells;
    private bool _drawing;

    /// <param name="device">La tarjeta gráfica con la que se dibuja.</param>
    public TextRenderer(GraphicsDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);

        _batch = new SpriteBatch(device);
        _pixel = new Texture2D(device, 1, 1);
        _pixel.SetData([Color.White]);
        (_atlas, _cells) = BuildAtlas(device);
    }

    /// <summary>Ancho de cada letra, en píxeles.</summary>
    public static int GlyphWidth => BitmapFont.Width;

    /// <summary>Alto de cada letra, en píxeles.</summary>
    public static int GlyphHeight => BitmapFont.Height;

    /// <summary>
    /// Empieza a dibujar el fotograma. Todo lo que se pinte debe ir entre esto y
    /// <see cref="End"/>. Lo que se dibuje se da en coordenadas del lienzo: aquí
    /// se aplica la escala que lo coloca en la pantalla del móvil, así que las
    /// pantallas no tienen que saber de qué tamaño es el teléfono.
    /// </summary>
    /// <param name="smooth">
    /// Cierto para suavizar al agrandar (el mundo, que es una imagen). Falso para
    /// mantener el texto nítido.
    /// </param>
    public void Begin(bool smooth = false)
    {
        if (_drawing)
        {
            return;
        }

        // La interfaz va siempre por encima del mundo, así que se dibuja sin
        // prueba de profundidad: si no, las barras y los botones se colarían
        // detrás del terreno.
        _batch.GraphicsDevice.DepthStencilState = DepthStencilState.None;

        var viewport = _batch.GraphicsDevice.Viewport;
        var transform = VirtualCanvas.Transform(viewport.Width, viewport.Height);

        _batch.Begin(
            samplerState: smooth ? SamplerState.LinearClamp : SamplerState.PointClamp,
            blendState: BlendState.AlphaBlend,
            transformMatrix: transform);
        _drawing = true;
    }

    /// <summary>Cierra el fotograma de dibujo.</summary>
    public void End()
    {
        if (!_drawing)
        {
            return;
        }

        _batch.End();
        _drawing = false;
    }

    /// <summary>Escribe un texto.</summary>
    /// <param name="text">Texto a escribir.</param>
    /// <param name="position">Esquina superior izquierda.</param>
    /// <param name="scale">Cuántos píxeles ocupa cada píxel de la letra.</param>
    /// <param name="color">Color del texto.</param>
    public void Text(string text, Vector2 position, float scale = 1f, Color? color = null)
    {
        if (string.IsNullOrEmpty(text) || scale <= 0f)
        {
            return;
        }

        var tint = color ?? Color.White;
        var advance = (BitmapFont.Width + BitmapFont.Spacing) * scale;
        var x = position.X;
        var glyphWidth = MathF.Max(1f, BitmapFont.Width * scale);
        var glyphHeight = MathF.Max(1f, BitmapFont.Height * scale);

        foreach (var character in text)
        {
            if (_cells.TryGetValue(BitmapFont.Normalize(character), out var cell))
            {
                _batch.Draw(
                    _atlas,
                    new Rectangle(
                        (int)MathF.Round(x),
                        (int)MathF.Round(position.Y),
                        (int)MathF.Round(glyphWidth),
                        (int)MathF.Round(glyphHeight)),
                    cell,
                    tint);
            }

            x += advance;
        }
    }

    /// <summary>Escribe un texto centrado dentro de un rectángulo.</summary>
    public void TextCentered(string text, Rectangle bounds, float scale = 1f, Color? color = null)
    {
        var width = BitmapFont.MeasureWidth(text, 1) * scale;
        var height = BitmapFont.Height * scale;
        Text(
            text,
            new Vector2(bounds.X + ((bounds.Width - width) / 2f), bounds.Y + ((bounds.Height - height) / 2f)),
            scale,
            color);
    }

    /// <summary>Escribe un texto pegado al borde derecho de un rectángulo.</summary>
    public void TextRight(string text, Rectangle bounds, float scale = 1f, Color? color = null)
    {
        var width = BitmapFont.MeasureWidth(text, 1) * scale;
        var height = BitmapFont.Height * scale;
        Text(
            text,
            new Vector2(bounds.Right - width, bounds.Y + ((bounds.Height - height) / 2f)),
            scale,
            color);
    }

    /// <summary>Dibuja un rectángulo relleno (paneles y barras).</summary>
    public void Fill(Rectangle rectangle, Color color) => _batch.Draw(_pixel, rectangle, color);

    /// <summary>Dibuja una textura, estirada al rectángulo indicado.</summary>
    public void Texture(Microsoft.Xna.Framework.Graphics.Texture2D texture, Rectangle destination, Color? tint = null)
    {
        ArgumentNullException.ThrowIfNull(texture);
        _batch.Draw(texture, destination, tint ?? Color.White);
    }

    /// <summary>Dibuja el borde de un rectángulo.</summary>
    public void Frame(Rectangle rectangle, Color color, int thickness = 1)
    {
        _batch.Draw(_pixel, new Rectangle(rectangle.X, rectangle.Y, rectangle.Width, thickness), color);
        _batch.Draw(_pixel, new Rectangle(rectangle.X, rectangle.Bottom - thickness, rectangle.Width, thickness), color);
        _batch.Draw(_pixel, new Rectangle(rectangle.X, rectangle.Y, thickness, rectangle.Height), color);
        _batch.Draw(_pixel, new Rectangle(rectangle.Right - thickness, rectangle.Y, thickness, rectangle.Height), color);
    }

    /// <summary>Dibuja un círculo relleno, para el joystick y los botones.</summary>
    public void Circle(Vector2 center, float radius, Color color)
    {
        if (radius <= 0f)
        {
            return;
        }

        var size = (int)MathF.Ceiling(radius * 2f);
        for (var y = 0; y < size; y++)
        {
            var dy = y - radius;
            var half = MathF.Sqrt(MathF.Max(0f, (radius * radius) - (dy * dy)));
            if (half <= 0f)
            {
                continue;
            }

            _batch.Draw(
                _pixel,
                new Rectangle((int)(center.X - half), (int)(center.Y - radius) + y, Math.Max(1, (int)(half * 2f)), 1),
                color);
        }
    }

    /// <summary>Dibuja el contorno de un círculo.</summary>
    public void CircleOutline(Vector2 center, float radius, Color color, float thickness = 2f)
    {
        if (radius <= 0f)
        {
            return;
        }

        var segments = Math.Max(20, (int)(radius * 0.8f));
        for (var i = 0; i < segments; i++)
        {
            var angle = i * MathF.Tau / segments;
            var point = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
            Circle(point, MathF.Max(0.5f, thickness / 2f), color);
        }
    }

    /// <summary>Parte un texto en líneas que quepan en un número de letras.</summary>
    public static List<string> Wrap(string text, int columns)
    {
        var lines = new List<string>();
        if (string.IsNullOrEmpty(text) || columns <= 0)
        {
            return lines;
        }

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

    /// <summary>
    /// Monta el atlas y las casillas de cada letra de una sola pasada, para que
    /// el orden de una y otras no pueda descuadrarse.
    /// </summary>
    private static (Texture2D Atlas, Dictionary<char, Rectangle> Cells) BuildAtlas(GraphicsDevice device)
    {
        var cellWidth = BitmapFont.Width + Padding;
        var cellHeight = BitmapFont.Height + Padding;
        var width = Columns * cellWidth;
        var height = Rows * cellHeight;
        var pixels = new Color[width * height];
        var cells = new Dictionary<char, Rectangle>();

        var index = 0;
        foreach (var character in BitmapFont.Characters)
        {
            if (index >= Columns * Rows)
            {
                break;
            }

            var glyph = BitmapFont.TryGetGlyph(character);
            if (glyph is null)
            {
                index++;
                continue;
            }

            var column = index % Columns;
            var row = index / Columns;
            var originX = column * cellWidth;
            var originY = row * cellHeight;

            for (var y = 0; y < BitmapFont.Height; y++)
            {
                var bits = glyph[y];
                for (var x = 0; x < BitmapFont.Width; x++)
                {
                    if ((bits & (1 << (BitmapFont.Width - 1 - x))) != 0)
                    {
                        pixels[((originY + y) * width) + originX + x] = Color.White;
                    }
                }
            }

            cells[character] = new Rectangle(originX, originY, BitmapFont.Width, BitmapFont.Height);
            index++;
        }

        var texture = new Texture2D(device, width, height, mipmap: false, SurfaceFormat.Color);
        texture.SetData(pixels);
        return (texture, cells);
    }

    public void Dispose()
    {
        _atlas.Dispose();
        _pixel.Dispose();
        _batch.Dispose();
    }
}
