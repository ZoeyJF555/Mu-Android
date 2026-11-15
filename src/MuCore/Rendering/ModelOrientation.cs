using System.Numerics;

namespace Mu.Core.Rendering;

/// <summary>
/// Convierte entre los ejes del modelo y los del mundo. Los BMD vienen con Z
/// arriba (como en el cliente original), mientras que el mundo tiene Y arriba.
/// </summary>
public static class ModelOrientation
{
    /// <summary>Gira el modelo para que su Z (arriba) pase a ser el Y del mundo.</summary>
    public static readonly Matrix4x4 ZUpToYUp = Matrix4x4.CreateRotationX(-MathF.PI / 2f);

    /// <summary>
    /// Matriz de mundo de un modelo colocado en un punto: lo pasa a las
    /// unidades del mundo, lo gira a Y arriba y lo traslada.
    /// </summary>
    public static Matrix4x4 ToWorld(Vector3 position, float headingDegrees = 0f, float scale = 1f)
    {
        var heading = Matrix4x4.CreateRotationY(headingDegrees * MathF.PI / 180f);
        return Matrix4x4.CreateScale(scale / Camera.CentimetersPerUnit)
            * ZUpToYUp
            * heading
            * Matrix4x4.CreateTranslation(position);
    }
}
