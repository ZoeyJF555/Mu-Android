using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Mu.Client;

/// <summary>
/// Juego compartido entre escritorio y Android. Aquí irá creciendo el cliente:
/// escenas, mundo, objetos y UI.
/// </summary>
public sealed class MuGame : Game
{
    private readonly GraphicsDeviceManager _graphics;

    public MuGame()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = 1280,
            PreferredBackBufferHeight = 720,
            SynchronizeWithVerticalRetrace = true,
        };

        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        Window.Title = "Mu Android";
        Window.AllowUserResizing = true;
        base.Initialize();
    }

    protected override void Update(GameTime gameTime)
    {
        if (Keyboard.GetState().IsKeyDown(Keys.Escape))
        {
            Exit();
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(10, 12, 18));
        base.Draw(gameTime);
    }
}
