using System.Numerics;

namespace Mu.Core.Rendering;

/// <summary>
/// Sombreado de los objetos del mapa. Los objetos no traen luz horneada como el
/// terreno, así que se iluminan con una luz de sol fija. La dirección es la del
/// cliente de referencia: el sol está alto y detrás de la cámara por defecto.
/// </summary>
public static class ObjectLighting
{
    /// <summary>Hacia dónde apunta la luz (eje Y arriba, como en el mundo).</summary>
    private static readonly Vector3 Direction = Vector3.Normalize(new Vector3(-0.5f, 1f, -0.75f));

    /// <summary>Luz mínima de las caras en sombra, para que no queden negras.</summary>
    public const float Ambient = 0.45f;

    /// <summary>Brillo de una cara con la normal indicada, de 0 a 1.</summary>
    public static float Shade(Vector3 normal)
    {
        var diffuse = MathF.Max(0f, Vector3.Dot(Vector3.Normalize(normal), Direction));
        return Math.Clamp(Ambient + ((1f - Ambient) * diffuse), 0f, 1f);
    }
}
