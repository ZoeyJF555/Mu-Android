using Mu.Client.Ui;

namespace Mu.Client.Scenes;

/// <summary>
/// Una pantalla del juego: el login, la selección de personaje, el mapa... Cada
/// una recibe los fotogramas y se dibuja sola, y para pasar a otra pide el
/// cambio al anfitrión en vez de conocerla.
/// </summary>
public abstract class Scene
{
    /// <summary>El anfitrión, con la tarjeta gráfica y el perfil guardado.</summary>
    protected GameHost Host { get; private set; } = null!;

    /// <summary>Prepara la escena. Se llama una vez, al entrar.</summary>
    public virtual void Load()
    {
    }

    /// <summary>Suelta lo que tuviera cargado. Se llama al salir.</summary>
    public virtual void Unload()
    {
    }

    /// <summary>Avanza un fotograma.</summary>
    /// <param name="seconds">Segundos transcurridos.</param>
    /// <param name="input">Lo que está tocando el jugador.</param>
    public abstract void Update(float seconds, InputState input);

    /// <summary>Dibuja la pantalla.</summary>
    /// <param name="input">Lo que está tocando el jugador, para pintar los botones pulsados.</param>
    public abstract void Draw(InputState input);

    /// <summary>Guarda el anfitrión cuando la escena entra.</summary>
    internal void Attach(GameHost host)
    {
        Host = host;
    }
}
