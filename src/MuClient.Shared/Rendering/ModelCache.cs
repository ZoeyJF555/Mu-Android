using Microsoft.Xna.Framework.Graphics;
using Mu.Core.Formats.Texture;
using Mu.Core.Rendering;

namespace Mu.Client.Rendering;

/// <summary>
/// Guarda los modelos que se repiten en el mapa. Los diez bots usan el mismo
/// modelo y los monstruos se reparten entre seis, así que sin esta caché cada
/// uno subiría sus propias mallas y texturas a la tarjeta gráfica: con treinta
/// personajes eso es mucha memoria de más en un móvil.
/// </summary>
public sealed class ModelCache : IDisposable
{
    private readonly GraphicsDevice _device;
    private readonly Dictionary<string, Entry> _entries = new();

    /// <param name="device">La tarjeta gráfica.</param>
    public ModelCache(GraphicsDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);
        _device = device;
    }

    /// <summary>
    /// Crea un dibujante del modelo indicado. Si es la primera vez que se pide,
    /// lo carga con <paramref name="load"/>; si no, reutiliza lo que ya hay.
    /// Devuelve null si el modelo no se puede leer, para que el mapa siga en pie.
    /// </summary>
    /// <param name="key">Con qué se identifica el modelo (su ruta, por ejemplo).</param>
    /// <param name="load">Cómo cargarlo la primera vez.</param>
    public CharacterRenderer? Create(string key, Func<(CharacterModel Model, DecodedImage?[] Textures)> load)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(load);

        if (!_entries.TryGetValue(key, out var entry))
        {
            try
            {
                var (model, textures) = load();
                entry = new Entry(model, new CharacterResources(_device, model, textures));
            }
            catch (Exception error) when (error is IOException or InvalidDataException or FileNotFoundException)
            {
                Console.Error.WriteLine($"[Mu] no se pudo cargar '{key}': {error.Message}");
                return null;
            }

            _entries[key] = entry;
        }

        return new CharacterRenderer(_device, entry.Model, entry.Resources);
    }

    public void Dispose()
    {
        foreach (var entry in _entries.Values)
        {
            entry.Resources.Dispose();
        }

        _entries.Clear();
    }

    private sealed record Entry(CharacterModel Model, CharacterResources Resources);
}
