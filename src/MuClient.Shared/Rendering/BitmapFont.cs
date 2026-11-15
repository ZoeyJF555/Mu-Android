namespace Mu.Client.Rendering;

/// <summary>
/// Una fuente de mapa de bits dibujada a mano. El proyecto no usa el
/// empaquetador de contenido de MonoGame, así que en lugar de cargar un
/// <c>SpriteFont</c> las letras se definen aquí y se dibujan píxel a píxel. Con
/// esto el juego puede enseñar un mensaje en pantalla cuando algo falla, que es
/// la única forma de enterarse en un móvil sin depurador.
/// </summary>
public static class BitmapFont
{
    /// <summary>Ancho de cada letra, en píxeles.</summary>
    public const int Width = 5;

    /// <summary>Alto de cada letra, en píxeles.</summary>
    public const int Height = 7;

    /// <summary>Separación entre letras, en píxeles.</summary>
    public const int Spacing = 1;

    /// <summary>
    /// Cada letra son siete filas de cinco bits. El bit de más peso es el píxel
    /// de la izquierda, así que se leen igual que se ven.
    /// </summary>
    private static readonly Dictionary<char, byte[]> Glyphs = new()
    {
        [' '] = [0b00000, 0b00000, 0b00000, 0b00000, 0b00000, 0b00000, 0b00000],
        ['A'] = [0b01110, 0b10001, 0b10001, 0b11111, 0b10001, 0b10001, 0b10001],
        ['B'] = [0b11110, 0b10001, 0b10001, 0b11110, 0b10001, 0b10001, 0b11110],
        ['C'] = [0b01110, 0b10001, 0b10000, 0b10000, 0b10000, 0b10001, 0b01110],
        ['D'] = [0b11110, 0b10001, 0b10001, 0b10001, 0b10001, 0b10001, 0b11110],
        ['E'] = [0b11111, 0b10000, 0b10000, 0b11110, 0b10000, 0b10000, 0b11111],
        ['F'] = [0b11111, 0b10000, 0b10000, 0b11110, 0b10000, 0b10000, 0b10000],
        ['G'] = [0b01110, 0b10001, 0b10000, 0b10111, 0b10001, 0b10001, 0b01111],
        ['H'] = [0b10001, 0b10001, 0b10001, 0b11111, 0b10001, 0b10001, 0b10001],
        ['I'] = [0b01110, 0b00100, 0b00100, 0b00100, 0b00100, 0b00100, 0b01110],
        ['J'] = [0b00111, 0b00010, 0b00010, 0b00010, 0b00010, 0b10010, 0b01100],
        ['K'] = [0b10001, 0b10010, 0b10100, 0b11000, 0b10100, 0b10010, 0b10001],
        ['L'] = [0b10000, 0b10000, 0b10000, 0b10000, 0b10000, 0b10000, 0b11111],
        ['M'] = [0b10001, 0b11011, 0b10101, 0b10101, 0b10001, 0b10001, 0b10001],
        ['N'] = [0b10001, 0b11001, 0b10101, 0b10011, 0b10001, 0b10001, 0b10001],
        ['O'] = [0b01110, 0b10001, 0b10001, 0b10001, 0b10001, 0b10001, 0b01110],
        ['P'] = [0b11110, 0b10001, 0b10001, 0b11110, 0b10000, 0b10000, 0b10000],
        ['Q'] = [0b01110, 0b10001, 0b10001, 0b10001, 0b10101, 0b10010, 0b01101],
        ['R'] = [0b11110, 0b10001, 0b10001, 0b11110, 0b10100, 0b10010, 0b10001],
        ['S'] = [0b01111, 0b10000, 0b10000, 0b01110, 0b00001, 0b00001, 0b11110],
        ['T'] = [0b11111, 0b00100, 0b00100, 0b00100, 0b00100, 0b00100, 0b00100],
        ['U'] = [0b10001, 0b10001, 0b10001, 0b10001, 0b10001, 0b10001, 0b01110],
        ['V'] = [0b10001, 0b10001, 0b10001, 0b10001, 0b10001, 0b01010, 0b00100],
        ['W'] = [0b10001, 0b10001, 0b10001, 0b10101, 0b10101, 0b11011, 0b10001],
        ['X'] = [0b10001, 0b10001, 0b01010, 0b00100, 0b01010, 0b10001, 0b10001],
        ['Y'] = [0b10001, 0b10001, 0b01010, 0b00100, 0b00100, 0b00100, 0b00100],
        ['Z'] = [0b11111, 0b00001, 0b00010, 0b00100, 0b01000, 0b10000, 0b11111],
        ['0'] = [0b01110, 0b10001, 0b10011, 0b10101, 0b11001, 0b10001, 0b01110],
        ['1'] = [0b00100, 0b01100, 0b00100, 0b00100, 0b00100, 0b00100, 0b01110],
        ['2'] = [0b01110, 0b10001, 0b00001, 0b00010, 0b00100, 0b01000, 0b11111],
        ['3'] = [0b11111, 0b00010, 0b00100, 0b00010, 0b00001, 0b10001, 0b01110],
        ['4'] = [0b00010, 0b00110, 0b01010, 0b10010, 0b11111, 0b00010, 0b00010],
        ['5'] = [0b11111, 0b10000, 0b11110, 0b00001, 0b00001, 0b10001, 0b01110],
        ['6'] = [0b00110, 0b01000, 0b10000, 0b11110, 0b10001, 0b10001, 0b01110],
        ['7'] = [0b11111, 0b00001, 0b00010, 0b00100, 0b01000, 0b01000, 0b01000],
        ['8'] = [0b01110, 0b10001, 0b10001, 0b01110, 0b10001, 0b10001, 0b01110],
        ['9'] = [0b01110, 0b10001, 0b10001, 0b01111, 0b00001, 0b00010, 0b01100],
        ['.'] = [0b00000, 0b00000, 0b00000, 0b00000, 0b00000, 0b01100, 0b01100],
        [','] = [0b00000, 0b00000, 0b00000, 0b00000, 0b01100, 0b01100, 0b01000],
        [':'] = [0b00000, 0b01100, 0b01100, 0b00000, 0b01100, 0b01100, 0b00000],
        ['-'] = [0b00000, 0b00000, 0b00000, 0b11111, 0b00000, 0b00000, 0b00000],
        ['_'] = [0b00000, 0b00000, 0b00000, 0b00000, 0b00000, 0b00000, 0b11111],
        ['/'] = [0b00001, 0b00010, 0b00010, 0b00100, 0b01000, 0b01000, 0b10000],
        ['('] = [0b00010, 0b00100, 0b01000, 0b01000, 0b01000, 0b00100, 0b00010],
        [')'] = [0b01000, 0b00100, 0b00010, 0b00010, 0b00010, 0b00100, 0b01000],
        ['!'] = [0b00100, 0b00100, 0b00100, 0b00100, 0b00100, 0b00000, 0b00100],
        ['?'] = [0b01110, 0b10001, 0b00001, 0b00110, 0b00100, 0b00000, 0b00100],
        ['='] = [0b00000, 0b00000, 0b11111, 0b00000, 0b11111, 0b00000, 0b00000],
        ['*'] = [0b00000, 0b10101, 0b01110, 0b11111, 0b01110, 0b10101, 0b00000],
    };

    /// <summary>El dibujo de una letra, en mayúsculas y sin acentos.</summary>
    private static byte[] GlyphFor(char character)
    {
        var upper = char.ToUpperInvariant(character);
        var plain = StripAccent(upper);
        return Glyphs.TryGetValue(plain, out var glyph) ? glyph : Glyphs['?'];
    }

    /// <summary>Quita el acento de una letra, que aquí no hay sitio para él.</summary>
    private static char StripAccent(char character) => character switch
    {
        'Á' or 'À' or 'Ä' or 'Â' => 'A',
        'É' or 'È' or 'Ë' or 'Ê' => 'E',
        'Í' or 'Ì' or 'Ï' or 'Î' => 'I',
        'Ó' or 'Ò' or 'Ö' or 'Ô' => 'O',
        'Ú' or 'Ù' or 'Ü' or 'Û' => 'U',
        'Ñ' => 'N',
        'Ç' => 'C',
        _ => character,
    };

    /// <summary>Cierto si la letra tiene dibujo propio.</summary>
    public static bool CanDraw(char character) =>
        Glyphs.ContainsKey(Normalize(character));

    /// <summary>Las letras que tienen dibujo, para construir un atlas con ellas.</summary>
    public static IReadOnlyCollection<char> Characters => Glyphs.Keys;

    /// <summary>
    /// La letra con la que se dibuja otra: en mayúsculas y sin acento, porque la
    /// fuente solo tiene esas. Las que no existen se dibujan como un interrogante.
    /// </summary>
    public static char Normalize(char character)
    {
        var plain = StripAccent(char.ToUpperInvariant(character));
        return Glyphs.ContainsKey(plain) ? plain : '?';
    }

    /// <summary>
    /// El dibujo de una letra, en siete filas de cinco bits, o null si no la hay.
    /// Se usa para montar la fuente como textura en vez de pintar píxel a píxel,
    /// que en el móvil sería demasiado lento.
    /// </summary>
    public static byte[]? TryGetGlyph(char character)
    {
        var upper = char.ToUpperInvariant(character);
        return Glyphs.TryGetValue(StripAccent(upper), out var glyph) ? glyph : null;
    }

    /// <summary>Ancho que ocupa un texto, en píxeles.</summary>
    public static int MeasureWidth(string text, int scale = 1)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        return ((text.Length * (Width + Spacing)) - Spacing) * scale;
    }

    /// <summary>Alto que ocupa un texto, en píxeles.</summary>
    public static int MeasureHeight(string text, int scale = 1) =>
        string.IsNullOrEmpty(text) ? 0 : Height * scale;

    /// <summary>
    /// Recorre las letras de un texto y avisa, para cada píxel encendido, de su
    /// posición en píxeles dentro del texto. Así se puede dibujar con lo que sea
    /// que pinte el juego, sin depender de una fuente del sistema.
    /// </summary>
    /// <param name="text">Texto a recorrer.</param>
    /// <param name="scale">Cuántos píxeles ocupa cada píxel de la letra.</param>
    /// <param name="pixel">Recibe la coordenada de cada píxel encendido.</param>
    public static void ForEachPixel(string text, int scale, Action<int, int> pixel)
    {
        ArgumentNullException.ThrowIfNull(pixel);
        if (string.IsNullOrEmpty(text) || scale < 1)
        {
            return;
        }

        for (var index = 0; index < text.Length; index++)
        {
            var glyph = GlyphFor(text[index]);
            var originX = index * (Width + Spacing) * scale;

            for (var row = 0; row < Height; row++)
            {
                var bits = glyph[row];
                for (var column = 0; column < Width; column++)
                {
                    var on = (bits & (1 << (Width - 1 - column))) != 0;
                    if (!on)
                    {
                        continue;
                    }

                    for (var dy = 0; dy < scale; dy++)
                    {
                        for (var dx = 0; dx < scale; dx++)
                        {
                            pixel(originX + (column * scale) + dx, (row * scale) + dy);
                        }
                    }
                }
            }
        }
    }
}
