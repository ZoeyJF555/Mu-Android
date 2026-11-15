using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mu.Core.Formats.Texture;
using Mu.Core.Rendering;

namespace Mu.Client.Rendering;

/// <summary>
/// Lo que se sube a la tarjeta gráfica de un modelo animado: sus mallas y sus
/// texturas. Es caro de construir y no cambia, así que se comparte entre todas
/// las copias del mismo modelo. Con 10 bots, 18 monstruos y 2 NPC, tener una
/// copia por instancia multiplicaba la memoria por treinta sin necesidad.
/// </summary>
public sealed class CharacterResources : IDisposable
{
    private readonly GpuMesh[] _meshes;
    private readonly Texture2D?[] _textures;

    /// <param name="device">La tarjeta gráfica.</param>
    /// <param name="model">El modelo con sus mallas.</param>
    /// <param name="textures">La textura de cada malla, en su orden.</param>
    public CharacterResources(GraphicsDevice device, CharacterModel model, DecodedImage?[] textures)
    {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(model);

        _meshes = new GpuMesh[model.Meshes.Count];
        _textures = new Texture2D?[model.Meshes.Count];

        for (var i = 0; i < model.Meshes.Count; i++)
        {
            _meshes[i] = GpuMesh.Create(device, BuildVertices(model.Meshes[i]), model.Meshes[i].Indices);
            _textures[i] = i < textures.Length ? TryCreateTexture(device, textures[i]) : null;
        }
    }

    /// <summary>Cuántas mallas tiene el modelo.</summary>
    public int Count => _meshes.Length;

    /// <summary>La malla de una posición.</summary>
    public GpuMesh Mesh(int index) => _meshes[index];

    /// <summary>La textura de una posición, o null si le falta.</summary>
    public Texture2D? Texture(int index) => _textures[index];

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

    public void Dispose()
    {
        foreach (var mesh in _meshes)
        {
            mesh.Dispose();
        }

        foreach (var texture in _textures)
        {
            texture?.Dispose();
        }
    }
}
