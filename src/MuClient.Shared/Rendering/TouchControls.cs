using Microsoft.Xna.Framework;

namespace Mu.Client.Rendering;

/// <summary>Un dedo apoyado en la pantalla, en píxeles.</summary>
public readonly record struct TouchPoint(int Id, float X, float Y);

/// <summary>
/// Los mandos en pantalla para jugar en un móvil. A la izquierda hay un mando
/// que aparece donde se apoya el dedo y que mueve al personaje; a la derecha,
/// dos botones para pasar de mapa. La lógica está aparte del juego para poder
/// probarla sin una pantalla táctil delante.
/// </summary>
public sealed class TouchControls
{
    /// <summary>Radio del mando, como fracción del lado más corto de la pantalla.</summary>
    private const float RadiusFactor = 0.18f;

    private int? _stickId;
    private Vector2 _stickOrigin;
    private bool _mapNextHeld;
    private bool _mapPreviousHeld;

    /// <summary>Hacia dónde se empuja el mando: X gira, Y avanza.</summary>
    public Vector2 Move { get; private set; }

    /// <summary>Paso de mapa pedido en este fotograma: -1, 0 o 1.</summary>
    public int MapStep { get; private set; }

    /// <summary>Cierto mientras haya un dedo en el mando.</summary>
    public bool IsMoving { get; private set; }

    /// <summary>Rectángulo del botón de mapa anterior.</summary>
    public static Rectangle MapPreviousButton(int width, int height) =>
        Button(width, height, 0);

    /// <summary>Rectángulo del botón de mapa siguiente.</summary>
    public static Rectangle MapNextButton(int width, int height) =>
        Button(width, height, 1);

    /// <summary>Radio del mando para una pantalla dada.</summary>
    public static float Radius(int width, int height) =>
        MathF.Max(24f, MathF.Min(width, height) * RadiusFactor);

    /// <summary>
    /// Recalcula el estado con los dedos que hay ahora mismo. Se llama una vez
    /// por fotograma; los botones solo avisan la primera vez que se tocan.
    /// </summary>
    /// <param name="points">Dedos apoyados en este fotograma.</param>
    /// <param name="width">Ancho de la pantalla, en píxeles.</param>
    /// <param name="height">Alto de la pantalla, en píxeles.</param>
    public void Update(IReadOnlyList<TouchPoint> points, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(points);

        MapStep = 0;
        Move = Vector2.Zero;
        IsMoving = false;

        var radius = Radius(width, height);
        var previousButton = MapPreviousButton(width, height);
        var nextButton = MapNextButton(width, height);

        var mapPreviousDown = false;
        var mapNextDown = false;
        var stick = (TouchPoint?)null;

        foreach (var point in points)
        {
            if (previousButton.Contains((int)point.X, (int)point.Y))
            {
                mapPreviousDown = true;
            }
            else if (nextButton.Contains((int)point.X, (int)point.Y))
            {
                mapNextDown = true;
            }
            else if (_stickId == point.Id)
            {
                stick = point;
            }
            else if (_stickId is null && point.X < width / 2f)
            {
                // El mando nace donde se apoya el dedo, así que no hace falta
                // apuntar a ningún sitio concreto.
                _stickId = point.Id;
                _stickOrigin = new Vector2(point.X, point.Y);
                stick = point;
            }
        }

        if (mapPreviousDown && !_mapPreviousHeld)
        {
            MapStep = -1;
        }
        else if (mapNextDown && !_mapNextHeld)
        {
            MapStep = 1;
        }

        _mapPreviousHeld = mapPreviousDown;
        _mapNextHeld = mapNextDown;

        if (stick is { } current)
        {
            var offset = new Vector2(current.X, current.Y) - _stickOrigin;
            var length = offset.Length();
            if (length > radius)
            {
                offset *= radius / length;
            }

            Move = new Vector2(offset.X / radius, -offset.Y / radius);
            IsMoving = true;
        }
        else
        {
            _stickId = null;
        }
    }

    private static Rectangle Button(int width, int height, int index)
    {
        var size = (int)(Radius(width, height) * 1.6f);
        var margin = size / 3;
        return new Rectangle(
            width - ((index + 1) * (size + margin)),
            height - (size + margin),
            size,
            size);
    }
}
