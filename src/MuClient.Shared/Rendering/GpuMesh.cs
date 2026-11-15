using Microsoft.Xna.Framework.Graphics;

namespace Mu.Client.Rendering;

/// <summary>
/// Una malla ya subida a la tarjeta gráfica, con sus vértices y sus índices.
/// Sirve tanto para mallas con color horneado (el terreno y los objetos) como
/// para mallas con huesos (los personajes).
/// </summary>
public sealed class GpuMesh : IDisposable
{
    private readonly VertexBuffer _vertices;
    private readonly IndexBuffer _indices;

    private GpuMesh(VertexBuffer vertices, IndexBuffer indices)
    {
        _vertices = vertices;
        _indices = indices;
    }

    /// <summary>Cuántos índices tiene la malla.</summary>
    public int IndexCount => _indices.IndexCount;

    /// <summary>Sube una malla a la tarjeta gráfica.</summary>
    public static GpuMesh Create<T>(GraphicsDevice device, T[] vertices, int[] indices)
        where T : struct, IVertexType
    {
        var vertexBuffer = new VertexBuffer(
            device, default(T).VertexDeclaration, vertices.Length, BufferUsage.WriteOnly);
        vertexBuffer.SetData(vertices);

        var indexBuffer = new IndexBuffer(
            device, IndexElementSize.ThirtyTwoBits, indices.Length, BufferUsage.WriteOnly);
        indexBuffer.SetData(indices);

        return new GpuMesh(vertexBuffer, indexBuffer);
    }

    /// <summary>Deja la malla lista para dibujar y devuelve cuántos índices tiene.</summary>
    public int Bind(GraphicsDevice device)
    {
        device.SetVertexBuffer(_vertices);
        device.Indices = _indices;
        return _indices.IndexCount;
    }

    public void Dispose()
    {
        _vertices.Dispose();
        _indices.Dispose();
    }
}

