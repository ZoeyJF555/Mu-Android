using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mu.Core.Formats.Texture;
using Mu.Core.Rendering;
using Mu.Core.World;

namespace Mu.Client.Rendering;

/// <summary>
/// Dibuja un mundo completo: el suelo con su textura y los objetos encima. Todo
/// lo que se sube a la tarjeta gráfica se hace una vez al cargar, así que el
/// bucle de dibujo solo recorre objetos y emite órdenes de dibujo.
/// </summary>
public sealed class WorldRenderer : IDisposable
{
    /// <summary>Distancia máxima a la que se dibuja un objeto, en unidades.</summary>
    private const float ViewDistance = 220f;

    private readonly GraphicsDevice _device;
    private readonly BasicEffect _effect;
    private readonly GpuMesh _terrain;
    private readonly Texture2D _ground;
    private readonly Dictionary<int, ObjectPart[]> _objects;
    private readonly WorldAssets _world;
    private CharacterRenderer? _character;
    private float _characterHeading;

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

        _terrain = BuildTerrain(device, world.Terrain);
        _ground = CreateTexture(device, world.GroundTexture);

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

    /// <summary>El personaje que se dibuja sobre el mapa, si se cargó uno.</summary>
    public CharacterRenderer? Character
    {
        get => _character;
        set => _character = value;
    }

    /// <summary>Dónde está el personaje en el mundo, en unidades.</summary>
    public Vector3 CharacterPosition { get; set; } = new(128f, 0f, 128f);

    /// <summary>Hacia dónde mira el personaje, en grados.</summary>
    public float CharacterHeadingDegrees
    {
        get => _characterHeading;
        set => _characterHeading = value;
    }

    /// <summary>Los monstruos y demás actores que pueblan el mapa.</summary>
    public IList<Actor> Actors { get; } = new List<Actor>();

    /// <summary>Dibuja el mundo entero desde la cámara indicada.</summary>
    public void Draw(Camera camera) => Draw(camera, 0f);

    /// <summary>Dibuja el mundo y el personaje, avanzando su animación.</summary>
    public void Draw(Camera camera, float seconds)
    {
        var view = ToXna(camera.View);
        var projection = ToXna(camera.Projection);

        _effect.View = view;
        _effect.Projection = projection;
        _effect.World = Matrix.Identity;
        _effect.Texture = _ground;

        _device.DepthStencilState = DepthStencilState.Default;
        _device.RasterizerState = RasterizerState.CullCounterClockwise;
        _device.BlendState = BlendState.Opaque;

        foreach (var pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            var count = _terrain.Bind(_device);
            _device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, count / 3);
        }

        DrawObjects(camera);

        foreach (var actor in Actors)
        {
            DrawActor(actor, camera, seconds);
        }

        if (_character is not null)
        {
            var world = ModelOrientation.ToWorld(
                new System.Numerics.Vector3(CharacterPosition.X, CharacterPosition.Y, CharacterPosition.Z),
                CharacterHeadingDegrees);
            _character.Draw(ToXna(world), camera, seconds);
        }
    }

    private static void DrawActor(Actor actor, Camera camera, float seconds)
    {
        var world = ModelOrientation.ToWorld(
            new System.Numerics.Vector3(actor.Position.X, actor.Position.Y, actor.Position.Z),
            actor.HeadingDegrees);
        actor.Renderer.Draw(ToXna(world), camera, seconds);
    }

    private void DrawObjects(Camera camera)
    {
        var eye = camera.Position;

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

    private static GpuMesh BuildTerrain(GraphicsDevice device, TerrainMesh terrain)
    {
        var vertices = new VertexPositionColorTexture[terrain.Positions.Length];
        for (var i = 0; i < vertices.Length; i++)
        {
            var p = terrain.Positions[i];
            var c = terrain.Colors[i];
            vertices[i] = new VertexPositionColorTexture(
                new Vector3(p.X, p.Y, p.Z),
                new Color(c.X, c.Y, c.Z),
                new Vector2(terrain.TexCoords[i].X, terrain.TexCoords[i].Y));
        }

        return GpuMesh.Create(device, vertices, terrain.Indices);
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

    private static Texture2D CreateTexture(GraphicsDevice device, DecodedImage image)
    {
        var texture = new Texture2D(device, image.Width, image.Height, mipmap: false, SurfaceFormat.Color);
        texture.SetData(image.Pixels);
        return texture;
    }

    private static Texture2D? TryCreateTexture(GraphicsDevice device, DecodedImage? image) =>
        image is null || image.Width <= 0 || image.Height <= 0 ? null : CreateTexture(device, image);

    private static Matrix ToXna(System.Numerics.Matrix4x4 matrix) => new(
        matrix.M11, matrix.M12, matrix.M13, matrix.M14,
        matrix.M21, matrix.M22, matrix.M23, matrix.M24,
        matrix.M31, matrix.M32, matrix.M33, matrix.M34,
        matrix.M41, matrix.M42, matrix.M43, matrix.M44);

    public void Dispose()
    {
        _terrain.Dispose();
        _ground.Dispose();
        _effect.Dispose();

        foreach (var parts in _objects.Values)
        {
            foreach (var part in parts)
            {
                part.Mesh.Dispose();
                part.Texture?.Dispose();
            }
        }

        _character?.Dispose();
        foreach (var actor in Actors)
        {
            actor.Renderer.Dispose();
        }

        Actors.Clear();
    }

    private readonly record struct ObjectPart(GpuMesh Mesh, Texture2D? Texture);
}
