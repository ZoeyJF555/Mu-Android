using System.Numerics;

namespace Mu.Core.Rendering;

/// <summary>
/// La pose de un esqueleto en un fotograma concreto: la matriz de mundo de
/// cada hueso. Los huesos forman una jerarquía, así que la matriz de un hueso
/// es la de su padre multiplicada por la suya.
/// </summary>
public sealed class SkeletonPose
{
    /// <summary>Matriz de mundo de cada hueso, indexada por número de hueso.</summary>
    public required Matrix4x4[] BoneMatrices { get; init; }
}

/// <summary>
/// Matemáticas del esqueleto. La rotación de cada hueso se guarda como tres
/// ángulos en radianes y se convierte a cuaternión con la misma fórmula que el
/// cliente original.
/// </summary>
public static class Skeleton
{
    /// <summary>Convierte tres ángulos (radianes) al cuaternión que usa el cliente.</summary>
    public static Quaternion AngleToQuaternion(Vector3 angles)
    {
        var (sr, cr) = MathF.SinCos(angles.X * 0.5f);
        var (sp, cp) = MathF.SinCos(angles.Y * 0.5f);
        var (sy, cy) = MathF.SinCos(angles.Z * 0.5f);

        return new Quaternion(
            sr * cp * cy - cr * sp * sy,
            cr * sp * cy + sr * cp * sy,
            cr * cp * sy - sr * sp * cy,
            cr * cp * cy + sr * sp * sy);
    }

    /// <summary>Transformación local de un hueso: su posición y su rotación.</summary>
    public static Matrix4x4 LocalTransform(Vector3 position, Vector3 angles) =>
        Matrix4x4.CreateFromQuaternion(AngleToQuaternion(angles)) *
        Matrix4x4.CreateTranslation(position);

    /// <summary>
    /// Devuelve el cuaternión interpolado entre dos fotogramas, para que la
    /// animación no dé saltos cuando el fotograma cae entre dos claves.
    /// </summary>
    public static Quaternion Interpolate(Quaternion from, Quaternion to, float amount) =>
        Quaternion.Slerp(from, to, amount);
}
