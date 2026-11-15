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
        var window = RequestedWindow();
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = window.Width,
            PreferredBackBufferHeight = window.Height,
            SynchronizeWithVerticalRetrace = true,
        };

        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    /// <summary>
    /// Tamaño de ventana pedido por entorno, para poder probar el escalado con
    /// la forma de un móvil concreto sin tener el móvil. Por ejemplo
    /// <c>MU_WINDOW=1080x2340</c>. Sin la variable se usa el de siempre.
    /// </summary>
    private static (int Width, int Height) RequestedWindow()
    {
        var text = Environment.GetEnvironmentVariable("MU_WINDOW");
        if (string.IsNullOrWhiteSpace(text))
        {
            return (1280, 720);
        }

        var parts = text.Split('x', 'X');
        if (parts.Length == 2 &&
            int.TryParse(parts[0], out var width) &&
            int.TryParse(parts[1], out var height) &&
            width > 0 && height > 0)
        {
            return (width, height);
        }

        return (1280, 720);
    }

    protected override void Initialize()
    {
        Window.Title = "Mu Android";
        Window.AllowUserResizing = true;

        // El lienzo se ajusta a la forma de la pantalla antes de cargar nada:
        // las escenas ya dibujan sobre él desde el primer fotograma.
        ConfigureCanvas();
        Window.ClientSizeChanged += (_, _) =>
        {
            ConfigureCanvas();
            _graphics.ApplyChanges();
        };

        base.Initialize();
    }

    /// <summary>Ajusta el lienzo virtual al tamaño actual de la pantalla.</summary>
    private void ConfigureCanvas()
    {
        var bounds = Window.ClientBounds;
        var width = bounds.Width > 0 ? bounds.Width : _graphics.PreferredBackBufferWidth;
        var height = bounds.Height > 0 ? bounds.Height : _graphics.PreferredBackBufferHeight;
        VirtualCanvas.Configure(width, height);
    }

    private int _lastCanvasWidth;
    private int _lastCanvasHeight;

    /// <summary>
    /// Vuelve a ajustar el lienzo si la pantalla ha cambiado de tamaño desde la
    /// última vez. Se llama cada fotograma, pero solo hace trabajo cuando de
    /// verdad ha cambiado.
    /// </summary>
    private void RefreshCanvasIfNeeded()
    {
        var bounds = Window.ClientBounds;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        if (bounds.Width == _lastCanvasWidth && bounds.Height == _lastCanvasHeight)
        {
            return;
        }

        _lastCanvasWidth = bounds.Width;
        _lastCanvasHeight = bounds.Height;
        VirtualCanvas.Configure(bounds.Width, bounds.Height);
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
            _smokeType = Environment.GetEnvironmentVariable("MU_SMOKE_TYPE");
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
        // El tamaño real de la pantalla puede conocerse después de arrancar (en
        // Android, al crearse la superficie). Se revisa cada fotograma para que
        // el lienzo se ajuste en cuanto se sepa, sin quedarse con barras negras.
        RefreshCanvasIfNeeded();

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
                // Si la comprobación tiene que escribir, no se cierra hasta
                // terminar, para poder ver el resultado en la captura.
                if (_smokeType is { Length: > 0 } && _smokeTypeIndex < _smokeType.Length)
                {
                    _smokeLeft = SmokeSeconds;
                }
                else
                {
                    SaveScreenshot();
                    Console.Error.WriteLine("[Mu] comprobacion automatica terminada sin fallos");
                    Exit();
                }
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
        // En la comprobación automática se pueden fabricar toques sobre el
        // teclado en pantalla para verificar sin dedo que escribir funciona.
        if (_smokeType is { Length: > 0 })
        {
            return SmokeTouches();
        }

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

    /// <summary>Texto que la comprobación automática escribe sola, letra a letra.</summary>
    private string? _smokeType;

    private int _smokeTypeIndex;
    private int _smokePhaseFrames;

    /// <summary>
    /// Fabrica un toque sobre la tecla de la letra que toca escribir. Mantiene el
    /// mismo dedo unos fotogramas y luego lo levanta otros tantos: como el juego
    /// solo cuenta un dedo nuevo al apoyarse, cada letra entra una sola vez.
    /// </summary>
    private TouchPoint[] SmokeTouches()
    {
        const int framesDown = 3;
        const int framesUp = 3;

        if (_smokeType is null || _smokeTypeIndex >= _smokeType.Length)
        {
            return [];
        }

        var phase = _smokePhaseFrames % (framesDown + framesUp);

        // Al terminar de levantar el dedo, se pasa a la siguiente letra.
        if (phase == 0 && _smokePhaseFrames > 0)
        {
            _smokeTypeIndex++;
            if (_smokeTypeIndex >= _smokeType.Length)
            {
                return [];
            }
        }

        _smokePhaseFrames++;

        if (phase >= framesDown)
        {
            return [];
        }

        var keys = Mu.Core.Rendering.OnScreenKeyboard.Build(VirtualCanvas.Width, VirtualCanvas.Height);
        var wanted = _smokeType[_smokeTypeIndex];
        var key = keys.FirstOrDefault(k => k.Action == Mu.Core.Rendering.KeyboardAction.Character && k.Value == wanted);

        if (key.Bounds.Width == 0)
        {
            return [];
        }

        // Un dedo distinto por letra, pero el mismo durante todo el toque.
        return [new TouchPoint(1000 + _smokeTypeIndex, key.Bounds.CenterX, key.Bounds.CenterY)];
    }

    protected override void UnloadContent()
    {
        _host.Current?.Unload();
        _text?.Dispose();
        base.UnloadContent();
    }
}
