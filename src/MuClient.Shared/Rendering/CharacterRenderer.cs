using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mu.Core.Formats.Texture;
using Mu.Core.Rendering;

namespace Mu.Client.Rendering;

/// <summary>
/// Dibuja un personaje animado. Cada instancia solo guarda su fotograma actual:
/// las mallas y las texturas son de <see cref="CharacterResources"/>, que se
/// comparte entre todos los que usan el mismo modelo. Así los diez bots y los
/// dieciocho monstruos no triplican la memoria.
/// </summary>
public sealed class CharacterRenderer : IDisposable
{
    /// <summary>
    /// Fotogramas por segundo a los que se reproduce la animación. El cliente
    /// original reproduce a 5 (los monstruos a 4), y es lo que hace que el paso
    /// case con la velocidad: a 12 los pies patinaban, porque el cuerpo andaba
    /// menos de lo que movían las piernas.
    /// </summary>
    private const float FramesPerSecond = 5f;

    /// <summary>Cuántos huesos puede mover a la vez la tarjeta; el máximo del efecto es 72 y el modelo tiene 60.</summary>
    private const int MaxBones = 72;

    private readonly GraphicsDevice _device;
    private readonly SkinnedEffect _effect;
    private readonly CharacterResources _resources;
    private readonly Matrix[] _bones = new Matrix[MaxBones];

    private int _action;
    private float _frame;

    /// <param name="device">La tarjeta gráfica.</param>
    /// <param name="model">El modelo, del que se sacan las animaciones.</param>
    /// <param name="resources">Las mallas y texturas compartidas.</param>
    public CharacterRenderer(GraphicsDevice device, CharacterModel model, CharacterResources resources)
    {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(resources);

        _device = device;
        Model = model;
        _resources = resources;

        _effect = new SkinnedEffect(device)
        {
            WeightsPerVertex = 1,
            PreferPerPixelLighting = false,
            AmbientLightColor = new Vector3(ObjectLighting.Ambient),
            DiffuseColor = new Vector3(1f - ObjectLighting.Ambient),
        };

        _effect.DirectionalLight0.Enabled = true;
        _effect.DirectionalLight0.Direction = new Vector3(-0.5f, -1f, -0.75f);
        _effect.DirectionalLight0.DiffuseColor = Vector3.One;
    }

    /// <summary>Acción que se está reproduciendo (0 = parado).</summary>
    public int Action
    {
        get => _action;
        set
        {
            if (_action == value)
            {
                return;
            }

            // Al cambiar de acción se vuelve al primer fotograma, como el cliente
            // original: si no, un golpe empezaría a mitad y se vería raro.
            _action = value;
            _frame = 0f;
        }
    }

    /// <summary>El modelo que se dibuja, para consultar sus acciones.</summary>
    public CharacterModel Model { get; }

    /// <summary>Avanza la animación y dibuja el personaje con la matriz indicada.</summary>
    public void Draw(Matrix world, Camera camera, float seconds)
    {
        Advance(seconds);

        var pose = Model.Animator.Pose(Action, _frame);
        for (var i = 0; i < _bones.Length; i++)
        {
            _bones[i] = i < pose.BoneMatrices.Length
                ? ToXna(pose.BoneMatrices[i])
                : Matrix.Identity;
        }

        _effect.View = ToXna(camera.View);
        _effect.Projection = ToXna(camera.Projection);
        _effect.World = world;
        _effect.SetBoneTransforms(_bones);

        _device.DepthStencilState = DepthStencilState.Default;
        _device.RasterizerState = RasterizerState.CullCounterClockwise;
        _device.BlendState = BlendState.Opaque;

        for (var i = 0; i < _resources.Count; i++)
        {
            var texture = _resources.Texture(i);
            if (texture is null)
            {
                continue;
            }

            _effect.Texture = texture;
            foreach (var pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                var count = _resources.Mesh(i).Bind(_device);
                _device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, count / 3);
            }
        }
    }

    private void Advance(float seconds)
    {
        var keys = Model.Animator.KeyCount(Action);
        if (keys <= 0)
        {
            _frame = 0f;
            return;
        }

        _frame = (_frame + (seconds * FramesPerSecond)) % keys;
    }

    private static Matrix ToXna(System.Numerics.Matrix4x4 m) => new(
        m.M11, m.M12, m.M13, m.M14,
        m.M21, m.M22, m.M23, m.M24,
        m.M31, m.M32, m.M33, m.M34,
        m.M41, m.M42, m.M43, m.M44);

    /// <summary>
    /// Solo suelta el efecto: las mallas y las texturas son compartidas y las
    /// libera quien las creó.
    /// </summary>
    public void Dispose() => _effect.Dispose();
}
