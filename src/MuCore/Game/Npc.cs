using System.Numerics;

namespace Mu.Core.Game;

/// <summary>
/// Un habitante quieto del mapa que habla si te acercas. Los datos del cliente
/// no traen modelos de NPC propios, así que se dibuja con un modelo que sí
/// existe; lo que importa aquí es su sitio y su frase.
/// </summary>
public sealed class Npc
{
    /// <summary>Distancia a la que ya te habla, en unidades.</summary>
    public const float TalkRange = 4f;

    /// <param name="name">Nombre que se enseña encima.</param>
    /// <param name="position">Dónde está parado.</param>
    /// <param name="modelPath">Modelo con el que se dibuja.</param>
    /// <param name="greeting">Lo que dice al acercarse.</param>
    public Npc(string name, Vector3 position, string modelPath, string greeting)
    {
        Name = name;
        Position = position;
        ModelPath = modelPath;
        Greeting = greeting;
    }

    /// <summary>Nombre del NPC.</summary>
    public string Name { get; }

    /// <summary>Dónde está.</summary>
    public Vector3 Position { get; }

    /// <summary>Modelo con el que se dibuja.</summary>
    public string ModelPath { get; }

    /// <summary>Lo que dice.</summary>
    public string Greeting { get; }

    /// <summary>Cierto si el punto indicado está lo bastante cerca para hablar.</summary>
    public bool IsNear(Vector3 point) => Vector3.Distance(Position, point) <= TalkRange;

    /// <summary>Los NPC de Lorencia, junto a la fuente y la plaza.</summary>
    public static readonly Npc[] Lorencia =
    [
        new(
            "GUARDIA",
            new Vector3(133f, 0f, 124f),
            "Monster/Monster04.bmd",
            "BIENVENIDO A LORENCIA, VIAJERO."),
        new(
            "COMERCIANTE",
            new Vector3(123f, 0f, 131f),
            "Monster/Monster05.bmd",
            "VENDO POCIONES. VUELVE PRONTO."),
    ];
}
