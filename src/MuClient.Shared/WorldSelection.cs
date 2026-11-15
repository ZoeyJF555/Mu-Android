namespace Mu.Client;

/// <summary>
/// Lleva la cuenta del mundo que se está viendo y de cuál es el siguiente al
/// pasar de mapa. Se mantiene aparte del juego para poder probar el recorrido
/// sin abrir una ventana.
/// </summary>
public sealed class WorldSelection
{
    /// <summary>Mundos que se pueden recorrer, en el orden en que se visitan.</summary>
    public static readonly int[] Worlds =
    [
        1, 2, 3, 4, 5, 7, 8, 9, 10, 11, 12, 19, 25, 31, 32, 34, 35, 38, 39, 40,
        41, 42, 43, 47, 52, 57, 58, 59, 63, 64, 65, 66, 67, 68, 69, 70, 71, 72,
        73, 74, 75, 78, 79, 80, 81, 82,
    ];

    /// <summary>El primer mundo de la lista, que es el que se carga al arrancar.</summary>
    public const int Default = 1;

    /// <summary>El mundo que se está viendo.</summary>
    public int Current { get; private set; } = Default;

    /// <summary>Índice de <see cref="Current"/> dentro de <see cref="Worlds"/>.</summary>
    private int Index
    {
        get
        {
            var index = Array.IndexOf(Worlds, Current);
            return index < 0 ? 0 : index;
        }
    }

    /// <summary>
    /// Elige el mundo de partida. Un número que no esté en la lista se ignora,
    /// porque no se podría cargar.
    /// </summary>
    public void StartAt(int world)
    {
        if (Worlds.Contains(world))
        {
            Current = world;
        }
    }

    /// <summary>
    /// Pasa al mundo siguiente o al anterior, dando la vuelta por los extremos.
    /// Devuelve el mundo nuevo, o el mismo si no había ninguno al que pasar.
    /// </summary>
    public int Advance(int step)
    {
        if (step != 0 && Worlds.Length > 0)
        {
            var index = ((Index + step) % Worlds.Length + Worlds.Length) % Worlds.Length;
            Current = Worlds[index];
        }

        return Current;
    }
}
