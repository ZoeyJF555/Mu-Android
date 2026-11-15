using Microsoft.Xna.Framework;
using Mu.Core.Rendering;

namespace Mu.Client.Rendering;

/// <summary>
/// Un personaje colocado en el mundo: su modelo animado, dónde está y hacia
/// dónde mira. El jugador y los monstruos son lo mismo para el dibujo, así que
/// se guardan en la misma lista.
/// </summary>
public sealed class Actor
{
    /// <summary>El modelo animado del actor.</summary>
    public required CharacterRenderer Renderer { get; init; }

    /// <summary>Dónde está, en unidades del mundo.</summary>
    public Vector3 Position { get; set; }

    /// <summary>Hacia dónde mira, en grados.</summary>
    public float HeadingDegrees { get; set; }

    /// <summary>Acción que reproduce (0 = parado).</summary>
    public int Action
    {
        get => Renderer.Action;
        set => Renderer.Action = value;
    }
}
