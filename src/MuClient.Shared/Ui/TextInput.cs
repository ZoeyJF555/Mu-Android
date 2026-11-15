using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Mu.Client.Ui;

/// <summary>
/// Un campo de texto: guarda lo que se escribe, lo limita a un largo y lo
/// dibuja. Lo que se escribe llega de dos sitios: el teclado en pantalla del
/// juego (que es el que usa el móvil, en <see cref="KeyboardPanel"/>) y el
/// teclado físico (para probar en escritorio). Aquí solo se guardan las letras.
/// </summary>
public sealed class TextInput
{
    private readonly List<char> _text = [];
    private KeyboardState _previousKeyboard;

    /// <param name="maximumLength">Cuántas letras caben.</param>
    /// <param name="masked">Cierto si se dibuja oculto (para la contraseña).</param>
    public TextInput(int maximumLength, bool masked = false)
    {
        MaximumLength = Math.Max(1, maximumLength);
        Masked = masked;
    }

    /// <summary>Cuántas letras caben.</summary>
    public int MaximumLength { get; }

    /// <summary>Cierto si el texto se dibuja oculto.</summary>
    public bool Masked { get; }

    /// <summary>Lo que se ha escrito.</summary>
    public string Text => new(_text.ToArray());

    /// <summary>Cierto si está vacío.</summary>
    public bool IsEmpty => _text.Count == 0;

    /// <summary>Borra lo escrito.</summary>
    public void Clear()
    {
        _text.Clear();
        _previousKeyboard = default;
    }

    /// <summary>Pone un texto, recortado si no cabe.</summary>
    public void Set(string text)
    {
        _text.Clear();
        if (text is null)
        {
            return;
        }

        foreach (var character in text)
        {
            if (_text.Count >= MaximumLength)
            {
                break;
            }

            if (char.IsLetterOrDigit(character) || character is '-' or '_' or '.')
            {
                _text.Add(character);
            }
        }
    }

    /// <summary>
    /// Recoge las teclas del teclado físico. Devuelve cierto si algo ha cambiado,
    /// para que quien lo use sepa que hay que redibujar.
    /// </summary>
    public bool UpdateFromKeyboard(KeyboardState keyboard)
    {
        var changed = false;

        var typed = TryReadTypedCharacter(keyboard);
        if (typed is { } character)
        {
            changed = Append(character) || changed;
        }

        if (Pressed(keyboard, Keys.Back) || Pressed(keyboard, Keys.Delete))
        {
            changed = Backspace() || changed;
        }

        _previousKeyboard = keyboard;
        return changed;
    }

    /// <summary>Mete una letra, si cabe. Devuelve cierto si la ha aceptado.</summary>
    public bool Append(char character)
    {
        if (_text.Count >= MaximumLength)
        {
            return false;
        }

        if (!char.IsLetterOrDigit(character) && character is not ('-' or '_' or '.'))
        {
            return false;
        }

        _text.Add(character);
        return true;
    }

    /// <summary>Borra la última letra. Devuelve cierto si había algo que borrar.</summary>
    public bool Backspace()
    {
        if (_text.Count == 0)
        {
            return false;
        }

        _text.RemoveAt(_text.Count - 1);
        return true;
    }

    /// <summary>El texto tal como se dibuja (oculto con asteriscos si toca).</summary>
    public string DisplayText => Masked ? new string('*', _text.Count) : Text;

    /// <summary>La letra que se acaba de escribir con el teclado físico, si hay alguna.</summary>
    private char? TryReadTypedCharacter(KeyboardState keyboard)
    {
        for (var key = Keys.A; key <= Keys.Z; key++)
        {
            if (keyboard.IsKeyDown(key) && !_previousKeyboard.IsKeyDown(key))
            {
                return (char)('A' + (key - Keys.A));
            }
        }

        for (var key = Keys.D0; key <= Keys.D9; key++)
        {
            if (keyboard.IsKeyDown(key) && !_previousKeyboard.IsKeyDown(key))
            {
                return (char)('0' + (key - Keys.D0));
            }
        }

        if (keyboard.IsKeyDown(Keys.OemMinus) && !_previousKeyboard.IsKeyDown(Keys.OemMinus))
        {
            return '-';
        }

        if (keyboard.IsKeyDown(Keys.OemPeriod) && !_previousKeyboard.IsKeyDown(Keys.OemPeriod))
        {
            return '.';
        }

        if (keyboard.IsKeyDown(Keys.Space) && !_previousKeyboard.IsKeyDown(Keys.Space))
        {
            return '_';
        }

        return null;
    }

    private bool Pressed(KeyboardState keyboard, Keys key) =>
        keyboard.IsKeyDown(key) && !_previousKeyboard.IsKeyDown(key);
}
