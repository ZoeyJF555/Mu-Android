using Microsoft.Xna.Framework.Graphics;
using Mu.Client.Rendering;
using Mu.Core.Game;

namespace Mu.Client.Scenes;

/// <summary>
/// Lo que comparten todas las pantallas: la tarjeta gráfica, con qué se dibuja
/// la interfaz, dónde se guarda la partida y de dónde salen los recursos del
/// cliente. Pasa de una pantalla a otra sin que ellas se conozcan entre sí.
/// </summary>
public sealed class GameHost
{
    /// <param name="device">La tarjeta gráfica.</param>
    /// <param name="text">Con qué se dibuja el texto y los paneles.</param>
    /// <param name="profile">El perfil guardado en el dispositivo.</param>
    /// <param name="openAssets">De dónde salen los recursos del cliente.</param>
    public GameHost(
        GraphicsDevice device,
        TextRenderer text,
        ProfileStore profile,
        Func<Mu.Core.Assets.IAssetSource?> openAssets)
    {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(openAssets);

        Device = device;
        Text = text;
        Profile = profile;
        OpenAssets = openAssets;
    }

    /// <summary>La tarjeta gráfica.</summary>
    public GraphicsDevice Device { get; }

    /// <summary>Con qué se dibuja el texto y los paneles.</summary>
    public TextRenderer Text { get; }

    /// <summary>El perfil guardado en el dispositivo.</summary>
    public ProfileStore Profile { get; }

    /// <summary>De dónde salen los recursos del cliente.</summary>
    public Func<Mu.Core.Assets.IAssetSource?> OpenAssets { get; }

    /// <summary>La escena que se está mostrando.</summary>
    public Scene? Current { get; private set; }

    /// <summary>La escena que se quiere mostrar, que se aplica al principio del fotograma siguiente.</summary>
    public Scene? Next { get; private set; }

    /// <summary>Pide un cambio de escena; se aplica antes del siguiente fotograma.</summary>
    public void Change(Scene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        Next = scene;
    }

    /// <summary>Aplica el cambio de escena pendiente, si lo hay.</summary>
    public void ApplyPending()
    {
        if (Next is null)
        {
            return;
        }

        var previous = Current;
        Current = Next;
        Next = null;

        previous?.Unload();
        Current.Attach(this);
        Current.Load();
    }
}
