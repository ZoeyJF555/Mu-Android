using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Mu.Client.Rendering;

namespace Mu.Client.Ui;

/// <summary>
/// Lo que está tocando el jugador en este fotograma, ya traducido al lienzo del
/// juego. Distingue entre un dedo que acaba de apoyarse y uno que ya estaba,
/// que es lo que permite que un botón se pulse una vez y no muchas mientras se
/// mantiene el dedo.
/// </summary>
public sealed class InputState
{
    private readonly HashSet<int> _previous = [];
    private readonly HashSet<int> _current = [];
    private readonly List<TouchPoint> _points = [];
    private KeyboardState _previousKeyboard;

    /// <summary>Los dedos apoyados ahora mismo.</summary>
    public IReadOnlyList<TouchPoint> Points => _points;

    /// <summary>El teclado, para jugar en escritorio.</summary>
    public KeyboardState Keyboard { get; private set; }

    /// <summary>Cierto si la tecla se acaba de pulsar en este fotograma.</summary>
    public bool IsNewKey(Keys key) => Keyboard.IsKeyDown(key) && !_previousKeyboard.IsKeyDown(key);

    /// <summary>Cierto si el dedo acaba de apoyarse en este fotograma.</summary>
    public bool IsNew(TouchPoint point) => !_previous.Contains(point.Id);

    /// <summary>Cierto si hay algún dedo nuevo dentro del rectángulo.</summary>
    public bool Tapped(Rectangle rectangle)
    {
        foreach (var point in _points)
        {
            if (IsNew(point) && rectangle.Contains((int)point.X, (int)point.Y))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Cierto si hay algún dedo (nuevo o no) dentro del rectángulo.</summary>
    public bool Holding(Rectangle rectangle)
    {
        foreach (var point in _points)
        {
            if (rectangle.Contains((int)point.X, (int)point.Y))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Recalcula el estado con los dedos y el teclado de este fotograma.</summary>
    public void Update(IReadOnlyList<TouchPoint> points, KeyboardState keyboard)
    {
        ArgumentNullException.ThrowIfNull(points);

        Keyboard = keyboard;
        _points.Clear();
        _points.AddRange(points);

        // El "dedo anterior" pasa a ser el que había antes de recalcular; si se
        // copiara el de ahora, ningún toque parecería nuevo.
        _previous.Clear();
        foreach (var id in _current)
        {
            _previous.Add(id);
        }

        _current.Clear();
        foreach (var point in points)
        {
            _current.Add(point.Id);
        }

        _previousKeyboard = Keyboard;
    }
}
