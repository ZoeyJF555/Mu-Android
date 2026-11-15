using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Mu.Client.Rendering;
using Mu.Client.Scenes;
using Mu.Client.Ui;
using Mu.Core.Assets;
using Mu.Core.Game;

namespace Mu.Client;

/// <summary>
/// El juego compartido entre escritorio y Android. Se encarga de la ventana, de
/// repartir los fotogramas entre las pantallas y de pasar de una a otra; lo que
/// se ve y lo que se juega está en las escenas, y las reglas en MuCore. Todo el
/// juego funciona sin conexión: los datos van dentro del APK y la partida se
/// guarda en el propio dispositivo.
/// </summary>
public sealed class MuGame : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private readonly Func<IAssetSource?> _openAssets;

    private TextRenderer _text = null!;
    private InputState _input = null!;
    private GameHost _host = null!;

    /// <summary>Crea el juego buscando los datos en el disco, que es lo que hace el escritorio.</summary>
    public MuGame()
        : this(DataLocator.TryOpen)
    {
    }

    /// <summary>
    /// Crea el juego leyendo los datos de donde diga quien lo construye. Android
    /// los pasa desde dentro del APK, porque allí no son ficheros del disco.
    /// </summary>
    /// <param name="openAssets">Devuelve el origen de recursos, o null si no hay.</param>
    public MuGame(Func<IAssetSource?> openAssets)
    {
        ArgumentNullException.ThrowIfNull(openAssets);

        _openAssets = openAssets;
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
        Window.ClientSizeChanged += (_, _) => _graphics.ApplyChanges();
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _text = new TextRenderer(GraphicsDevice);
        _input = new InputState();
        _host = new GameHost(GraphicsDevice, _text, new ProfileStore(DataLocator.ProfilePath()), _openAssets);

        // Modo de comprobación automática: abre una pantalla, se queda unos
        // segundos y se cierra solo. Sirve para verificar sin pantalla que cada
        // pantalla carga y se dibuja.
        if (IsSmokeTest())
        {
            _smokeLeft = SmokeSeconds;
            _host.Change(BuildSmokeScene());
        }
        else
        {
            _host.Change(new LoginScene());
        }

        base.LoadContent();
    }

    /// <summary>Segundos que dura la comprobación automática.</summary>
    private const float SmokeSeconds = 4f;

    private float _smokeLeft;

    /// <summary>Cierto si se ha pedido la comprobación automática por entorno.</summary>
    private static bool IsSmokeTest() =>
        Environment.GetEnvironmentVariable("MU_SMOKE") is { Length: > 0 };

    /// <summary>La pantalla que pide la comprobación automática, con datos de prueba si hacen falta.</summary>
    private Scene BuildSmokeScene()
    {
        var which = Environment.GetEnvironmentVariable("MU_SMOKE_SCENE") ?? "game";
        Console.Error.WriteLine($"[Mu] comprobacion automatica: pantalla '{which}'");

        return which switch
        {
            "login" => new LoginScene(),
            "select" => SmokeSelection(),
            "create" => SmokeCreation(),
            _ => new GameScene(PrepareSmokeCharacter(), SmokeWorld()),
        };
    }

    /// <summary>Mundo que pide la comprobación automática, o 0 para el de siempre.</summary>
    private static int SmokeWorld() =>
        int.TryParse(Environment.GetEnvironmentVariable("MU_SMOKE_WORLD"), out var world) ? world : 0;

    /// <summary>Deja una cuenta con un personaje para poder ver la selección.</summary>
    private Scene SmokeSelection()
    {
        PrepareSmokeCharacter();
        return new CharacterSelectScene();
    }

    /// <summary>Abre la creación en un espacio libre.</summary>
    private Scene SmokeCreation()
    {
        var slot = PrepareSmokeCharacter();
        return new CharacterCreateScene(slot);
    }

    /// <summary>Crea cuenta y personaje si no hay, y devuelve el espacio a jugar.</summary>
    private int PrepareSmokeCharacter()
    {
        var profile = _host.Profile;
        if (!profile.HasAccount)
        {
            profile.LogIn("PRUEBA", "prueba");
        }

        var slot = profile.FirstFreeSlot();
        if (slot >= 0)
        {
            profile.CreateCharacter(slot, "PRUEBA", CharacterClass.BladeKnight);
        }
        else
        {
            slot = 0;
        }

        Console.Error.WriteLine($"[Mu] comprobacion automatica en el espacio {slot}");
        return slot;
    }

    protected override void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();
        if (keyboard.IsKeyDown(Keys.Escape))
        {
            Exit();
        }

        if (_smokeLeft > 0f)
        {
            _smokeLeft -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_smokeLeft <= 0f)
            {
                SaveScreenshot();
                Console.Error.WriteLine("[Mu] comprobacion automatica terminada sin fallos");
                Exit();
            }
        }

        _input.Update(ReadTouches(), keyboard);

        // Se aplica el cambio de pantalla pedido en el fotograma anterior, para
        // no soltar una escena mientras se está usando.
        _host.ApplyPending();
        _host.Current?.Update((float)gameTime.ElapsedGameTime.TotalSeconds, _input);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);
        _host.Current?.Draw(_input);
        base.Draw(gameTime);

        if (_smokeLeft > 0f)
        {
            _smokeShot = true;
        }
    }

    private bool _smokeShot;

    /// <summary>Guarda una captura del último fotograma, para poder revisarlo.</summary>
    private void SaveScreenshot()
    {
        if (!_smokeShot)
        {
            return;
        }

        try
        {
            var width = GraphicsDevice.PresentationParameters.BackBufferWidth;
            var height = GraphicsDevice.PresentationParameters.BackBufferHeight;
            var data = new Color[width * height];
            GraphicsDevice.GetBackBufferData(data);

            using var texture = new Texture2D(GraphicsDevice, width, height);
            texture.SetData(data);
            var path = Path.Combine(Directory.GetCurrentDirectory(), "comprobacion.png");
            using var stream = File.Create(path);
            texture.SaveAsPng(stream, width, height);
            Console.Error.WriteLine($"[Mu] captura guardada en {path}");
        }
        catch (Exception error)
        {
            Console.Error.WriteLine($"[Mu] no se pudo guardar la captura: {error.Message}");
        }
    }

    /// <summary>Los dedos apoyados ahora mismo, traducidos al lienzo del juego.</summary>
    private TouchPoint[] ReadTouches()
    {
        var state = TouchPanel.GetState();
        var points = new TouchPoint[state.Count];
        var width = GraphicsDevice.Viewport.Width;
        var height = GraphicsDevice.Viewport.Height;

        for (var index = 0; index < state.Count; index++)
        {
            var location = state[index];
            var canvasPoint = VirtualCanvas.ToCanvas(location.Position.X, location.Position.Y, width, height);
            points[index] = new TouchPoint(location.Id, canvasPoint.X, canvasPoint.Y);
        }

        return points;
    }

    protected override void UnloadContent()
    {
        _host.Current?.Unload();
        _text?.Dispose();
        base.UnloadContent();
    }
}
