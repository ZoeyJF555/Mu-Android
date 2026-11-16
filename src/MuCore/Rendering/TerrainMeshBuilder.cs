using System.Numerics;
using Mu.Core.Formats.Terrain;
using Mu.Core.Formats.Texture;

namespace Mu.Core.Rendering;

/// <summary>
/// Una porción del terreno que comparte textura, ya en el formato que espera el
/// renderizador. Se separa por textura porque cada una se dibuja con su propia
/// imagen: agrupar las casillas que la usan deja unas pocas órdenes de dibujo
/// por fotograma en vez de una por casilla.
/// </summary>
public sealed class TerrainBatch
{
    /// <summary>Qué textura de suelo usa (índice del mapa).</summary>
    public required int TextureIndex { get; init; }

    /// <summary>Posiciones de los vértices, en unidades del mundo.</summary>
    public required Vector3[] Positions { get; init; }

    /// <summary>Color de cada vértice: la luz horneada (RGB) y la mezcla (A).</summary>
    public required Vector4[] Colors { get; init; }

    /// <summary>Coordenadas de textura de cada vértice.</summary>
    public required Vector2[] TexCoords { get; init; }

    /// <summary>Índices de los triángulos.</summary>
    public required int[] Indices { get; init; }
}

/// <summary>
/// La geometría del terreno, partida en tandas por textura. La capa base de
/// cada casilla va en <see cref="Base"/> (se dibuja opaca) y la capa de encima,
/// si la hay, en <see cref="Overlay"/> (se dibuja mezclada con su transparencia).
/// Es lógica pura (sin GPU), así que se puede probar sin pantalla.
/// </summary>
public sealed class TerrainGeometry
{
    /// <summary>Las tandas opacas: la textura base de cada casilla.</summary>
    public required IReadOnlyList<TerrainBatch> Base { get; init; }

    /// <summary>Las tandas mezcladas: la textura de encima, con su transparencia.</summary>
    public required IReadOnlyList<TerrainBatch> Overlay { get; init; }
}

/// <summary>
/// Convierte el mapa de alturas, sus texturas y su luz en una malla lista para
/// dibujar. Cada casilla se dibuja con la textura que le toca, repetida sobre el
/// mapa: una textura cubre 4 × 4 casillas, igual que en el cliente original. Es
/// lo que da el aspecto de MU y no de bloques de color: antes se pintaba un solo
/// punto por casilla en una imagen del tamaño del mapa, y por eso el suelo salía
/// liso.
/// </summary>
public static class TerrainMeshBuilder
{
    /// <summary>
    /// Píxeles de textura por casilla. El cliente original calcula la
    /// coordenada de textura como <c>x · 64 / ancho</c>, de modo que una textura
    /// de 256 px cubre 4 × 4 casillas y una de 128 px cubre 2 × 2. Con este
    /// número, cada textura se repite a su tamaño real y el suelo no sale
    /// estirado como una mancha lisa.
    /// </summary>
    public const float PixelsPerTile = 64f;

    /// <summary>Altura a la que se hunden las casillas sin suelo, para no verlas.</summary>
    private const float NoGroundHeight = -10000f;

    /// <summary>Construye la geometría del terreno.</summary>
    /// <param name="heights">Altura por casilla.</param>
    /// <param name="attributes">Atributos por casilla (suelo, agua, agujeros...).</param>
    /// <param name="light">Color de luz por casilla; si es null, se usa luz plena.</param>
    /// <param name="tileSizes">Tamaño en píxeles de cada textura de suelo.</param>
    /// <param name="layer1">Textura base de cada casilla.</param>
    /// <param name="layer2">Textura de encima de cada casilla.</param>
    /// <param name="alpha">Mezcla de la capa de encima (0 = nada, 255 = solo ella).</param>
    public static TerrainGeometry Build(
        float[] heights,
        ushort[] attributes,
        Vector3[]? light,
        IReadOnlyList<(int Width, int Height)> tileSizes,
        byte[] layer1,
        byte[] layer2,
        byte[] alpha)
    {
        ArgumentNullException.ThrowIfNull(heights);
        ArgumentNullException.ThrowIfNull(attributes);
        ArgumentNullException.ThrowIfNull(tileSizes);
        ArgumentNullException.ThrowIfNull(layer1);
        ArgumentNullException.ThrowIfNull(layer2);
        ArgumentNullException.ThrowIfNull(alpha);

        var baseBatches = new Dictionary<int, BatchBuilder>();
        var overlayBatches = new Dictionary<int, BatchBuilder>();

        var size = TerrainConstants.Size;

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var index = (y * size) + x;
                if ((attributes[index] & TerrainAttributeReader.FlagNoGround) != 0)
                {
                    continue;
                }

                var blend = index < alpha.Length ? alpha[index] : (byte)0;
                var bottom = index < layer1.Length ? layer1[index] : (byte)0;
                var top = index < layer2.Length ? layer2[index] : (byte)0;

                // Con la casilla del todo cubierta por la capa de encima, esa es
                // la base; si no, la base es la de abajo y la de encima se mezcla.
                var baseTile = blend == 255 ? top : bottom;
                var overlayTile = blend is > 0 and < 255 ? top : -1;

                AddTile(
                    baseBatches, baseTile, x, y, index, heights, light, tileSizes, (byte)255);

                if (overlayTile >= 0)
                {
                    AddTile(
                        overlayBatches, overlayTile, x, y, index, heights, light, tileSizes, blend);
                }
            }
        }

        return new TerrainGeometry
        {
            Base = Build(baseBatches),
            Overlay = Build(overlayBatches),
        };
    }

    private static IReadOnlyList<TerrainBatch> Build(Dictionary<int, BatchBuilder> batches) =>
        batches
            .OrderBy(pair => pair.Key)
            .Select(pair => pair.Value.ToBatch(pair.Key))
            .ToArray();

    private static void AddTile(
        Dictionary<int, BatchBuilder> batches,
        int texture,
        int x,
        int y,
        int index,
        float[] heights,
        Vector3[]? light,
        IReadOnlyList<(int Width, int Height)> tileSizes,
        byte blend)
    {
        if (texture < 0 || texture >= tileSizes.Count)
        {
            // Los mundos que usan texturas que no están en los datos (los
            // índices 9 y siguientes) caen a la primera, para no dejar agujeros.
            texture = 0;
        }

        var (textureWidth, textureHeight) = tileSizes[texture];
        if (textureWidth <= 0 || textureHeight <= 0)
        {
            return;
        }

        if (!batches.TryGetValue(texture, out var batch))
        {
            batch = new BatchBuilder();
            batches[texture] = batch;
        }

        var idx1 = index;
        var idx2 = Index(x + 1, y);
        var idx3 = Index(x + 1, y + 1);
        var idx4 = Index(x, y + 1);

        var h1 = heights[idx1];
        var h2 = heights[idx2];
        var h3 = heights[idx3];
        var h4 = heights[idx4];

        // Las coordenadas de textura van como en el cliente original: la casilla
        // (x, y) ocupa el trozo (x·64/ancho, y·64/alto) de su textura, de modo
        // que cada textura se repite a su tamaño real por todo el mapa.
        var u0 = x * PixelsPerTile / textureWidth;
        var u1 = (x + 1) * PixelsPerTile / textureWidth;
        var v0 = y * PixelsPerTile / textureHeight;
        var v1 = (y + 1) * PixelsPerTile / textureHeight;

        var alpha = blend / 255f;

        var first = batch.Count;
        batch.Add(
            new Vector3(x, h1, y), Light(light, idx1, alpha), new Vector2(u0, v0),
            new Vector3(x + 1, h2, y), Light(light, idx2, alpha), new Vector2(u1, v0),
            new Vector3(x + 1, h3, y + 1), Light(light, idx3, alpha), new Vector2(u1, v1),
            new Vector3(x, h4, y + 1), Light(light, idx4, alpha), new Vector2(u0, v1));

        batch.AddQuad(first);
    }

    private static Vector4 Light(Vector3[]? light, int index, float alpha)
    {
        var color = light is not null && index < light.Length ? light[index] : Vector3.One;
        return new Vector4(color, alpha);
    }

    private static int Index(int x, int y)
    {
        var size = TerrainConstants.Size;
        x = Math.Clamp(x, 0, size - 1);
        y = Math.Clamp(y, 0, size - 1);
        return (y * size) + x;
    }

    private sealed class BatchBuilder
    {
        private readonly List<Vector3> _positions = [];
        private readonly List<Vector4> _colors = [];
        private readonly List<Vector2> _texCoords = [];
        private readonly List<int> _indices = [];

        public int Count => _positions.Count;

        public void Add(
            Vector3 p0, Vector4 c0, Vector2 t0,
            Vector3 p1, Vector4 c1, Vector2 t1,
            Vector3 p2, Vector4 c2, Vector2 t2,
            Vector3 p3, Vector4 c3, Vector2 t3)
        {
            _positions.Add(p0);
            _positions.Add(p1);
            _positions.Add(p2);
            _positions.Add(p3);
            _colors.Add(c0);
            _colors.Add(c1);
            _colors.Add(c2);
            _colors.Add(c3);
            _texCoords.Add(t0);
            _texCoords.Add(t1);
            _texCoords.Add(t2);
            _texCoords.Add(t3);
        }

        public void AddQuad(int first)
        {
            _indices.Add(first);
            _indices.Add(first + 1);
            _indices.Add(first + 2);
            _indices.Add(first);
            _indices.Add(first + 2);
            _indices.Add(first + 3);
        }

        public TerrainBatch ToBatch(int texture) => new()
        {
            TextureIndex = texture,
            Positions = _positions.ToArray(),
            Colors = _colors.ToArray(),
            TexCoords = _texCoords.ToArray(),
            Indices = _indices.ToArray(),
        };
    }
}
