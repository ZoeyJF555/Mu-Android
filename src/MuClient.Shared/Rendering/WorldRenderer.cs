using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mu.Core.Formats.Texture;
using Mu.Core.Rendering;
using Mu.Core.World;

namespace Mu.Client.Rendering;

/// <summary>
/// Dibuja un mundo completo: el suelo con sus texturas y los objetos encima.
/// Todo lo que se sube a la tarjeta gráfica se hace una vez al cargar, así que
/// el bucle de dibujo solo recorre tandas y objetos y emite órdenes de dibujo.
/// Los personajes que pueblan el mapa no son cosa suya: los dibuja la escena,
/// que sabe qué hay en el mundo en cada momento.
/// </summary>
public sealed class WorldRenderer : IDisposable
{
    /// <summary>Distancia máxima a la que se dibuja un objeto, en unidades.</summary>
    private const float ViewDistance = 220f;

    private readonly GraphicsDevice _device;
    private readonly BasicEffect _effect;
    private readonly TerrainBatchMesh[] _base;
    private readonly TerrainBatchMesh[] _overlay;
    private readonly Texture2D?[] _ground;
    private readonly Dictionary<int, ObjectPart[]> _objects;
    private readonly WorldAssets _world;

    public WorldRenderer(GraphicsDevice device, WorldAssets world)
    {
        _device = device;
        _world = world;

        _effect = new BasicEffect(device)
        {
            TextureEnabled = true,
            VertexColorEnabled = true,
            LightingEnabled = false,
        };

        _ground = world.GroundTiles.Select(t => TryCreateTexture(device, t)).ToArray();
        _base = BuildBatches(device, world.Terrain.Base);
        _overlay = BuildBatches(device, world.Terrain.Overlay);

        _objects = new Dictionary<int, ObjectPart[]>();
        foreach (var (type, model) in world.ObjectModels)
        {
            var parts = StaticMeshBuilder.Build(model.Model)
                .Select((mesh, i) => new ObjectPart(
                    GpuMesh.Create(device, BuildVertices(mesh), mesh.Indices),
                    TryCreateTexture(device, model.Textures[i])))
                .ToArray();

            if (parts.Length > 0)
            {
                _objects[type] = parts;
            }
        }
    }

    /// <summary>Cuántos objetos distintos tienen modelo cargado.</summary>
    public int ObjectTypeCount => _objects.Count;

    /// <summary>Dibuja el suelo y los objetos del mapa desde la cámara indicada.</summary>
    public void Draw(Camera camera)
    {
        _effect.View = ToXna(camera.View);
        _effect.Projection = ToXna(camera.Projection);
        _effect.World = Matrix.Identity;

        _device.DepthStencilState = DepthStencilState.Default;
        _device.RasterizerState = RasterizerState.CullCounterClockwise;

        // El suelo se repite por el mapa, así que se muestrea con repetición: una
        // textura cubre 4 × 4 casillas y se repite sin costura por todo el terreno.
        _device.SamplerStates[0] = SamplerState.LinearWrap;
        DrawBatches(_base, BlendState.Opaque);
        DrawBatches(_overlay, BlendState.NonPremultiplied);

        DrawObjects(camera);
    }

    private void DrawBatches(TerrainBatchMesh[] batches, BlendState blend)
    {
        _device.BlendState = blend;

        foreach (var batch in batches)
        {
            var texture = _ground[batch.TextureIndex];
            if (texture is null)
            {
                continue;
            }

            _effect.Texture = texture;
            foreach (var pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                var count = batch.Mesh.Bind(_device);
                _device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, count / 3);
            }
        }
    }

    private void DrawObjects(Camera camera)
    {
        var eye = camera.Position;

        _device.BlendState = BlendState.Opaque;

        foreach (var obj in _world.Objects)
        {
            if (!_objects.TryGetValue(obj.Type, out var parts))
            {
                continue;
            }

            var position = new System.Numerics.Vector3(
                obj.Position.X / Camera.CentimetersPerUnit,
                obj.Position.Z / Camera.CentimetersPerUnit,
                obj.Position.Y / Camera.CentimetersPerUnit);

            if (System.Numerics.Vector3.DistanceSquared(position, eye) > ViewDistance * ViewDistance)
            {
                continue;
            }

            _effect.World = ToXna(TerrainObjectTransform.Build(obj));

            foreach (var part in parts)
            {
                if (part.Texture is null)
                {
                    continue;
                }

                _effect.Texture = part.Texture;
                foreach (var pass in _effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    var count = part.Mesh.Bind(_device);
                    _device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, count / 3);
                }
            }
        }
    }

    private static TerrainBatchMesh[] BuildBatches(GraphicsDevice device, IReadOnlyList<TerrainBatch> batches)
    {
        var result = new TerrainBatchMesh[batches.Count];
        for (var i = 0; i < batches.Count; i++)
        {
            var batch = batches[i];
            var vertices = new VertexPositionColor4Texture[batch.Positions.Length];
            for (var v = 0; v < vertices.Length; v++)
            {
                vertices[v] = new VertexPositionColor4Texture(
                    new Vector3(batch.Positions[v].X, batch.Positions[v].Y, batch.Positions[v].Z),
                    batch.Colors[v],
                    new Vector2(batch.TexCoords[v].X, batch.TexCoords[v].Y));
            }

            result[i] = new TerrainBatchMesh(batch.TextureIndex, GpuMesh.Create(device, vertices, batch.Indices));
        }

        return result;
    }

    private static VertexPositionColorTexture[] BuildVertices(StaticMesh mesh)
    {
        var vertices = new VertexPositionColorTexture[mesh.Positions.Length];
        for (var i = 0; i < vertices.Length; i++)
        {
            var p = mesh.Positions[i];
            var light = ObjectLighting.Shade(mesh.Normals[i]);
            vertices[i] = new VertexPositionColorTexture(
                new Vector3(p.X, p.Y, p.Z),
                new Color(light, light, light),
                new Vector2(mesh.TexCoords[i].X, mesh.TexCoords[i].Y));
        }

        return vertices;
    }

    private static Texture2D? TryCreateTexture(GraphicsDevice device, DecodedImage? image) =>
        image is null || image.Width <= 0 || image.Height <= 0 ? null : CreateTexture(device, image);

    private static Texture2D CreateTexture(GraphicsDevice device, DecodedImage image)
    {
        var texture = new Texture2D(device, image.Width, image.Height, mipmap: false, SurfaceFormat.Color);
        texture.SetData(image.Pixels);
        return texture;
    }

    private static Matrix ToXna(System.Numerics.Matrix4x4 matrix) => new(
        matrix.M11, matrix.M12, matrix.M13, matrix.M14,
        matrix.M21, matrix.M22, matrix.M23, matrix.M24,
        matrix.M31, matrix.M32, matrix.M33, matrix.M34,
        matrix.M41, matrix.M42, matrix.M43, matrix.M44);

    public void Dispose()
    {
        foreach (var batch in _base)
        {
            batch.Mesh.Dispose();
        }

        foreach (var batch in _overlay)
        {
            batch.Mesh.Dispose();
        }

        foreach (var texture in _ground)
        {
            texture?.Dispose();
        }

        _effect.Dispose();

        foreach (var parts in _objects.Values)
        {
            foreach (var part in parts)
            {
                part.Mesh.Dispose();
                part.Texture?.Dispose();
            }
        }
    }

    private readonly record struct TerrainBatchMesh(int TextureIndex, GpuMesh Mesh);

    private readonly record struct ObjectPart(GpuMesh Mesh, Texture2D? Texture);
}
