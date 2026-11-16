using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Mu.Client.Rendering;

/// <summary>
/// Un vértice del terreno: posición, color (con transparencia, para la capa de
/// encima) y coordenadas de textura. Se usa en vez del de MonoGame porque este
/// lleva el color en RGBA y hace falta el canal de transparencia para mezclar
/// las dos capas del suelo.
/// </summary>
public struct VertexPositionColor4Texture : IVertexType
{
    /// <summary>Posición en unidades del mundo.</summary>
    public Vector3 Position;

    /// <summary>Color del vértice, con su transparencia.</summary>
    public Vector4 Color;

    /// <summary>Coordenadas de textura.</summary>
    public Vector2 TexCoord;

    public static readonly VertexDeclaration Declaration = new(
        new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
        new VertexElement(12, VertexElementFormat.Vector4, VertexElementUsage.Color, 0),
        new VertexElement(28, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0));

    public VertexPositionColor4Texture(Vector3 position, Vector4 color, Vector2 texCoord)
    {
        Position = position;
        Color = color;
        TexCoord = texCoord;
    }

    VertexDeclaration IVertexType.VertexDeclaration => Declaration;
}
