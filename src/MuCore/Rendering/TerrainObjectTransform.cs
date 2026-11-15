using System.Numerics;
using Mu.Core.Formats.Terrain;

namespace Mu.Core.Rendering;

/// <summary>
/// La matriz que coloca un objeto del mapa en el mundo. El fichero del mapa
/// guarda la posición y la rotación en centímetros y grados, con los ejes del
/// cliente original: X e Y son el plano del suelo y Z es la altura. En el
/// mundo, en cambio, el suelo es X/Z y la altura es Y, así que se intercambian.
/// </summary>
public static class TerrainObjectTransform
{
    /// <summary>La matriz de mundo de un objeto (escala, rotación y posición).</summary>
    public static Matrix4x4 Build(TerrainObject obj)
    {
        var rotation = Rotation(obj.Rotation);
        var position = new Vector3(
            obj.Position.X / Camera.CentimetersPerUnit,
            obj.Position.Z / Camera.CentimetersPerUnit,
            obj.Position.Y / Camera.CentimetersPerUnit);

        return Matrix4x4.CreateScale(obj.Scale / Camera.CentimetersPerUnit)
            * rotation
            * Matrix4x4.CreateTranslation(position);
    }

    /// <summary>
    /// La rotación del objeto. El original gira con `AngleMatrix` (Z·Y·X en su
    /// marco con Z arriba) y el modelo va reflejado, así que cada eje se niega
    /// al entrar; el viraje se queda en su signo y lo cambia el intercambio de
    /// ejes. Es la misma cuenta que hace el cliente de referencia.
    /// </summary>
    private static Matrix4x4 Rotation(Vector3 degrees)
    {
        const float toRadians = MathF.PI / 180f;
        var a0 = degrees.X * toRadians;
        var a1 = degrees.Y * toRadians;
        var a2 = -degrees.Z * toRadians;

        var (sr, cr) = MathF.SinCos(a0);
        var (sp, cp) = MathF.SinCos(a1);
        var (sy, cy) = MathF.SinCos(a2);

        var m00 = cp * cy;
        var m10 = cp * sy;
        var m20 = -sp;
        var m01 = (sr * sp * cy) - (cr * sy);
        var m11 = (sr * sp * sy) + (cr * cy);
        var m21 = sr * cp;
        var m02 = (cr * sp * cy) + (sr * sy);
        var m12 = (cr * sp * sy) - (sr * cy);
        var m22 = cr * cp;

        return new Matrix4x4(
            m00, m20, m10, 0f,
            m02, m22, m12, 0f,
            m01, m21, m11, 0f,
            0f, 0f, 0f, 1f);
    }
}
