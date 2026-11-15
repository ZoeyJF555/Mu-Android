using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Mu.Client.Rendering;

/// <summary>
/// Un vértice con un solo hueso, que es lo que necesita el skinning de MU:
/// cada vértice del BMD pertenece a un único hueso. MonoGame no trae este
/// tipo, así que se declara aquí con su formato en la tarjeta gráfica.
/// </summary>
public struct VertexPositionNormalTextureWeights : IVertexType
{
    /// <summary>Posición en el espacio del hueso.</summary>
    public Vector3 Position;

    /// <summary>Normal en el espacio del modelo.</summary>
    public Vector3 Normal;

    /// <summary>Coordenadas de textura.</summary>
    public Vector2 TexCoord;

    /// <summary>Índices de los huesos que mueven el vértice.</summary>
    public Vector4 BoneIndices;

    /// <summary>Peso de cada hueso; el primero vale 1.</summary>
    public Vector4 BoneWeights;

    public VertexPositionNormalTextureWeights(
        Vector3 position,
        Vector3 normal,
        Vector2 texCoord,
        Vector4 boneIndices,
        Vector4 boneWeights)
    {
        Position = position;
        Normal = normal;
        TexCoord = texCoord;
        BoneIndices = boneIndices;
        BoneWeights = boneWeights;
    }

    /// <summary>El formato que espera <see cref="SkinnedEffect"/>.</summary>
    public static readonly VertexDeclaration Declaration = new(
        new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
        new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
        new VertexElement(24, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
        new VertexElement(32, VertexElementFormat.Vector4, VertexElementUsage.BlendIndices, 0),
        new VertexElement(48, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 0));

    VertexDeclaration IVertexType.VertexDeclaration => Declaration;
}
