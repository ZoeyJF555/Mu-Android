namespace Mu.Core.Rendering;

/// <summary>Qué hace una tecla del teclado en pantalla al pulsarla.</summary>
public enum KeyboardAction
{
    /// <summary>Escribe su letra.</summary>
    Character,

    /// <summary>Borra la última letra.</summary>
    Backspace,

    /// <summary>Da por terminado lo que se estaba escribiendo.</summary>
    Enter,
}

/// <summary>Una tecla del teclado en pantalla: lo que enseña, dónde está y qué hace.</summary>
/// <param name="Label">El texto que se dibuja sobre la tecla.</param>
/// <param name="Value">La letra que escribe, si escribe alguna.</param>
/// <param name="Action">Lo que hace al pulsarla.</param>
/// <param name="Bounds">Dónde está, en coordenadas del lienzo.</param>
public readonly record struct KeyboardKey(string Label, char Value, KeyboardAction Action, CanvasRect Bounds);

/// <summary>
/// El teclado que se dibuja dentro del juego para escribir la cuenta, la
/// contraseña y el nombre del personaje. Se dibuja el juego su propio teclado, y
/// no se llama al del sistema, porque MonoGame para Android en esta versión no
/// ofrece forma de abrir el teclado de Android ni de recoger lo que se escribe;
/// con este teclado se escribe igual en cualquier móvil y sin depender de nada
/// de fuera. La colocación de las teclas está aparte del dibujo para poder
/// probarla sin pantalla.
/// </summary>
public static class OnScreenKeyboard
{
    /// <summary>Margen hasta el borde del lienzo, en píxeles.</summary>
    public const int Margin = 24;

    /// <summary>Separación entre teclas, en píxeles.</summary>
    public const int Gap = 8;

    /// <summary>Alto total del teclado, en píxeles. Deja sitio al formulario encima.</summary>
    public const int AreaHeight = 260;

    /// <summary>Dónde empieza la zona del formulario, contando desde arriba.</summary>
    public const int FormTop = 80;

    /// <summary>Separación entre el formulario y el teclado, en píxeles.</summary>
    public const int FormGap = 24;

    /// <summary>Las teclas que se ofrecen en cada fila de letras.</summary>
    private static readonly string[] Rows =
    [
        "1234567890",
        "QWERTYUIOP",
        "ASDFGHJKL-",
        "ZXCVBNM_.",
    ];

    /// <summary>Zona que ocupa el teclado, pegado al borde inferior del lienzo.</summary>
    /// <param name="canvasWidth">Ancho del lienzo, en píxeles.</param>
    /// <param name="canvasHeight">Alto del lienzo, en píxeles.</param>
    public static CanvasRect Area(int canvasWidth, int canvasHeight) =>
        new(Margin, canvasHeight - AreaHeight - Margin, canvasWidth - (Margin * 2), AreaHeight);

    /// <summary>
    /// Zona libre que queda por encima del teclado, donde van el formulario y los
    /// botones. Se calcula a partir del teclado para que nunca se solapen: el
    /// panel se coloca aquí y siempre queda entero a la vista.
    /// </summary>
    /// <param name="canvasWidth">Ancho del lienzo, en píxeles.</param>
    /// <param name="canvasHeight">Alto del lienzo, en píxeles.</param>
    public static CanvasRect FormArea(int canvasWidth, int canvasHeight)
    {
        var keyboard = Area(canvasWidth, canvasHeight);
        var top = FormTop;
        var bottom = keyboard.Y - FormGap;
        return new CanvasRect(Margin, top, canvasWidth - (Margin * 2), Math.Max(1, bottom - top));
    }

    /// <summary>Coloca todas las teclas dentro del lienzo.</summary>
    /// <param name="canvasWidth">Ancho del lienzo, en píxeles.</param>
    /// <param name="canvasHeight">Alto del lienzo, en píxeles.</param>
    public static IReadOnlyList<KeyboardKey> Build(int canvasWidth, int canvasHeight)
    {
        var area = Area(canvasWidth, canvasHeight);
        var rows = Rows.Length + 1;
        var columns = 10;

        var keyWidth = (area.Width - ((columns - 1) * Gap)) / columns;
        var keyHeight = (area.Height - ((rows - 1) * Gap)) / rows;

        var keys = new List<KeyboardKey>();

        for (var row = 0; row < Rows.Length; row++)
        {
            var letters = Rows[row];
            var y = area.Y + (row * (keyHeight + Gap));

            for (var column = 0; column < letters.Length; column++)
            {
                var x = area.X + (column * (keyWidth + Gap));
                keys.Add(new KeyboardKey(
                    letters[column].ToString(),
                    letters[column],
                    KeyboardAction.Character,
                    new CanvasRect(x, y, keyWidth, keyHeight)));
            }
        }

        // La última fila lleva el borrado y el "listo", que no escriben letras.
        var actionY = area.Y + (Rows.Length * (keyHeight + Gap));
        var backWidth = (keyWidth * 3) + (Gap * 2);
        var doneWidth = (keyWidth * 3) + (Gap * 2);

        keys.Add(new KeyboardKey(
            "BORRAR",
            '\0',
            KeyboardAction.Backspace,
            new CanvasRect(area.X, actionY, backWidth, keyHeight)));

        keys.Add(new KeyboardKey(
            "LISTO",
            '\0',
            KeyboardAction.Enter,
            new CanvasRect(area.Right - doneWidth, actionY, doneWidth, keyHeight)));

        return keys;
    }

    /// <summary>La tecla que cae bajo un punto, o null si no cae en ninguna.</summary>
    /// <param name="keys">Las teclas, tal como las devuelve <see cref="Build"/>.</param>
    /// <param name="x">Coordenada X del punto, en el lienzo.</param>
    /// <param name="y">Coordenada Y del punto, en el lienzo.</param>
    public static KeyboardKey? Hit(IReadOnlyList<KeyboardKey> keys, float x, float y)
    {
        ArgumentNullException.ThrowIfNull(keys);

        foreach (var key in keys)
        {
            if (key.Bounds.Contains((int)x, (int)y))
            {
                return key;
            }
        }

        return null;
    }

    /// <summary>Cierto si el punto cae dentro de la zona del teclado.</summary>
    /// <param name="x">Coordenada X del punto, en el lienzo.</param>
    /// <param name="y">Coordenada Y del punto, en el lienzo.</param>
    /// <param name="canvasWidth">Ancho del lienzo, en píxeles.</param>
    /// <param name="canvasHeight">Alto del lienzo, en píxeles.</param>
    public static bool Contains(float x, float y, int canvasWidth, int canvasHeight) =>
        Area(canvasWidth, canvasHeight).Contains((int)x, (int)y);
}
