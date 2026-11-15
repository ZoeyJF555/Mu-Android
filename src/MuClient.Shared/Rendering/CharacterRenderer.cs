using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mu.Core.Formats.Texture;
using Mu.Core.Rendering;

namespace Mu.Client.Rendering;

/// <summary>
/// Dibuja un personaje animado. Cada malla se sube a la tarjeta gráfica con sus
/// huesos y, en cada fotograma, se le pasan las matrices de la pose actual; la
/// tarjeta se encarga de mover los vértices (skinning).
/// </summary>
public sealed class CharacterRenderer : IDisposable
{
    /// <summary>Fotogramas por segundo a los que se reproduce la animación.</summary>
    private const float FramesPerSecond = 12f;

    /// <summary>Cuántos huesos puede mover a la vez la tarjeta; el máximo del efecto es 72 y el modelo tiene 60.</summary>
    private const int MaxBones = 72;

    private readonly GraphicsDevice _device;
    private readonly SkinnedEffect _effect;
    private readonly CharacterModel _model;
    private readonly BoneMesh[] _meshes;

    private float _frame;

    public CharacterRenderer(GraphicsDevice device, CharacterModel model, DecodedImage?[] textures)
    {
        _device = device;
        _model = model;

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

        _meshes = new BoneMesh[model.Meshes.Count];
        for (var i = 0; i < model.Meshes.Count; i++)
        {
            _meshes[i] = new BoneMesh(
                GpuMesh.Create(device, BuildVertices(model.Meshes[i]), model.Meshes[i].Indices),
                TryCreateTexture(device, textures[i]));
        }
    }

    /// <summary>Acción que se está reproduciendo (0 = parado).</summary>
    public int Action { get; set; }

    /// <summary>El modelo que se dibuja, para consultar sus acciones.</summary>
    public CharacterModel Model => _model;

    /// <summary>Avanza la animación y dibuja el personaje con la matriz indicada.</summary>
    public void Draw(Matrix world, Camera camera, float seconds)
    {
        Advance(seconds);

        var pose = _model.Animator.Pose(Action, _frame);
        var bones = new Matrix[MaxBones];
        for (var i = 0; i < bones.Length; i++)
        {
            bones[i] = i < pose.BoneMatrices.Length
                ? ToXna(pose.BoneMatrices[i])
                : Matrix.Identity;
        }

        _effect.View = ToXna(camera.View);
        _effect.Projection = ToXna(camera.Projection);
        _effect.World = world;
        _effect.SetBoneTransforms(bones);

        _device.DepthStencilState = DepthStencilState.Default;
        _device.RasterizerState = RasterizerState.CullCounterClockwise;
        _device.BlendState = BlendState.Opaque;

        foreach (var mesh in _meshes)
        {
            if (mesh.Texture is null)
            {
                continue;
            }

            _effect.Texture = mesh.Texture;
            foreach (var pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                var count = mesh.Mesh.Bind(_device);
                _device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, count / 3);
            }
        }
    }

    private void Advance(float seconds)
    {
        var keys = _model.Animator.KeyCount(Action);
        if (keys <= 0)
        {
            _frame = 0f;
            return;
        }

        _frame = (_frame + (seconds * FramesPerSecond)) % keys;
    }

    private static VertexPositionNormalTextureWeights[] BuildVertices(SkinnedMesh mesh)
    {
        var vertices = new VertexPositionNormalTextureWeights[mesh.Vertices.Length];
        for (var i = 0; i < vertices.Length; i++)
        {
            var v = mesh.Vertices[i];
            vertices[i] = new VertexPositionNormalTextureWeights(
                new Vector3(v.Position.X, v.Position.Y, v.Position.Z),
                new Vector3(v.Normal.X, v.Normal.Y, v.Normal.Z),
                new Vector2(v.TexCoord.X, v.TexCoord.Y),
                new Vector4(v.BoneIndex, 0f, 0f, 0f),
                new Vector4(1f, 0f, 0f, 0f));
        }

        return vertices;
    }

    private static Texture2D? TryCreateTexture(GraphicsDevice device, DecodedImage? image)
    {
        if (image is null || image.Width <= 0 || image.Height <= 0)
        {
            return null;
        }

        var texture = new Texture2D(device, image.Width, image.Height, mipmap: false, SurfaceFormat.Color);
        texture.SetData(image.Pixels);
        return texture;
    }

    private static Matrix ToXna(System.Numerics.Matrix4x4 m) => new(
        m.M11, m.M12, m.M13, m.M14,
        m.M21, m.M22, m.M23, m.M24,
        m.M31, m.M32, m.M33, m.M34,
        m.M41, m.M42, m.M43, m.M44);

    public void Dispose()
    {
        _effect.Dispose();
        foreach (var mesh in _meshes)
        {
            mesh.Mesh.Dispose();
            mesh.Texture?.Dispose();
        }
    }

    private readonly record struct BoneMesh(GpuMesh Mesh, Texture2D? Texture);
}
