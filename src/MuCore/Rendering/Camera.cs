using System.Numerics;

namespace Mu.Core.Rendering;

/// <summary>
/// La cámara del juego. Mira al personaje desde atrás y desde arriba, como el
/// cliente original: un ángulo fijo de unos 48 grados y una distancia que se
/// puede alejar. Trabaja en las mismas unidades que el mundo (una casilla por
/// unidad), y convierte desde centímetros como el original.
/// </summary>
public sealed class Camera
{
    /// <summary>Centímetros que sube la cámara por encima de lo que retrocede.</summary>
    public const float HeightBackoff = 150f;

    /// <summary>Altura de los ojos del personaje, en centímetros.</summary>
    public const float EyeHeight = 160f;

    /// <summary>Ángulo de la cámara sobre la horizontal, en grados.</summary>
    public const float PitchDegrees = 48.5f;

    /// <summary>Ángulo de visión vertical, en grados, como el original.</summary>
    public const float FieldOfViewDegrees = 35f;

    /// <summary>Centímetros por unidad de mundo.</summary>
    public const float CentimetersPerUnit = 100f;

    /// <summary>El punto al que mira la cámara (normalmente los pies del personaje).</summary>
    public Vector3 Target { get; set; }

    /// <summary>Hacia dónde mira la cámara, en grados (el rumbo).</summary>
    public float HeadingDegrees { get; set; } = -45f;

    /// <summary>Distancia de la cámara al objetivo, en centímetros.</summary>
    public float Distance { get; set; } = 1700f;

    /// <summary>Posición de la cámara en el mundo.</summary>
    public Vector3 Position { get; private set; }

    /// <summary>Matriz de vista, para dibujar.</summary>
    public Matrix4x4 View { get; private set; } = Matrix4x4.Identity;

    /// <summary>Matriz de proyección, para dibujar.</summary>
    public Matrix4x4 Projection { get; private set; } = Matrix4x4.Identity;

    /// <summary>Recalcula la posición de la cámara y las matrices de dibujo.</summary>
    /// <param name="aspect">Ancho dividido entre alto de la ventana.</param>
    public void Update(float aspect)
    {
        var pitch = PitchDegrees * MathF.PI / 180f;
        var horizontal = Distance * MathF.Cos(pitch) / CentimetersPerUnit;
        var vertical = MathF.Max(0f, Distance - HeightBackoff) / CentimetersPerUnit;

        var heading = HeadingDegrees * MathF.PI / 180f;
        Position = Target + new Vector3(
            horizontal * MathF.Cos(heading),
            vertical,
            horizontal * MathF.Sin(heading));

        View = Matrix4x4.CreateLookAt(Position, Target, Vector3.UnitY);
        Projection = Matrix4x4.CreatePerspectiveFieldOfView(
            FieldOfViewDegrees * MathF.PI / 180f,
            aspect,
            nearPlaneDistance: 0.1f,
            farPlaneDistance: 2000f);
    }

    /// <summary>
    /// Proyecta un punto del mundo sobre la pantalla, en píxeles. Devuelve null
    /// si el punto queda detrás de la cámara, que es cuando no se debe dibujar.
    /// Se usa para colocar los nombres encima de los personajes.
    /// </summary>
    /// <param name="world">Punto del mundo.</param>
    /// <param name="width">Ancho de la pantalla, en píxeles.</param>
    /// <param name="height">Alto de la pantalla, en píxeles.</param>
    public Vector2? Project(Vector3 world, int width, int height)
    {
        var clip = Vector4.Transform(new Vector4(world, 1f), View * Projection);
        if (clip.W <= 0.001f)
        {
            return null;
        }

        var ndc = new Vector3(clip.X / clip.W, clip.Y / clip.W, clip.Z / clip.W);
        if (ndc.Z < 0f || ndc.Z > 1f)
        {
            return null;
        }

        return new Vector2(
            (ndc.X + 1f) * 0.5f * width,
            (1f - ndc.Y) * 0.5f * height);
    }
}
