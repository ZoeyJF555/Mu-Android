using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Mu.Client.Rendering;
using Mu.Core.Assets;
using Mu.Core.Formats.Bmd;
using Mu.Core.Rendering;
using Mu.Core.World;

namespace Mu.Client;

/// <summary>
/// Juego compartido entre escritorio y Android. Carga un mundo del cliente y lo
/// dibuja, con una cámara que se puede girar y acercar con el teclado. En el
/// móvil se maneja con los mandos en pantalla.
/// </summary>
public sealed class MuGame : Game
{
    /// <summary>Clase del personaje (1 = mago oscuro, 2 = caballero...).</summary>
    private const int StartClass = 1;

    /// <summary>Velocidad de avance del personaje, en unidades por segundo.</summary>
    private const float MoveSpeed = 12f;

    /// <summary>Acción de caminar del esqueleto del personaje.</summary>
    private const int WalkingAction = (int)PlayerAction.WalkMale;

    /// <summary>Arma que lleva el personaje en la mano derecha.</summary>
    private const string StartWeapon = "Item/Sword01.bmd";

    /// <summary>Variable de entorno para elegir el mundo inicial sin recompilar.</summary>
    private const string WorldEnvironmentVariable = "MU_WORLD";

    /// <summary>Monstruos que se reparten por el mapa como muestra.</summary>
    private static readonly string[] SampleMonsters =
    [
        "Monster/Monster01.bmd",
        "Monster/Monster02.bmd",
        "Monster/Monster03.bmd",
        "Monster/Monster04.bmd",
        "Monster/Monster05.bmd",
        "Monster/Monster06.bmd",
    ];

    private readonly GraphicsDeviceManager _graphics;
    private readonly Func<IAssetSource?> _openAssets;

    private IAssetSource? _source;
    private WorldAssets? _world;
    private WorldRenderer? _renderer;
    private OverlayRenderer? _overlay;
    private readonly Camera _camera = new();
    private readonly TouchControls _touch = new();
    private string? _message;
    private readonly WorldSelection _worlds = new();
    private KeyboardState _previousKeyboard;

    /// <summary>
    /// Crea el juego buscando los datos en el disco, que es lo que hace el
    /// escritorio.
    /// </summary>
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
        _worlds.StartAt(ReadStartWorld());
        base.Initialize();
    }

    /// <summary>El mundo pedido por el entorno; si no vale, el de por defecto.</summary>
    private static int ReadStartWorld()
    {
        var text = Environment.GetEnvironmentVariable(WorldEnvironmentVariable);
        return int.TryParse(text, out var world) ? world : WorldSelection.Default;
    }

    protected override void LoadContent()
    {
        _overlay = new OverlayRenderer(GraphicsDevice);
        LoadWorld();
        base.LoadContent();
    }

    private void LoadWorld()
    {
        _source ??= _openAssets();
        if (_source is null)
        {
            Report(
                "No se encontraron los datos del cliente. En el escritorio, indica su carpeta en la " +
                $"variable de entorno {DataLocator.EnvironmentVariable}; en Android deben ir dentro del APK.");
            return;
        }

        _renderer?.Dispose();
        _renderer = null;
        _world = null;
        _message = null;

        try
        {
            _world = WorldLoader.Load(_source, _worlds.Current);
            _renderer = new WorldRenderer(GraphicsDevice, _world);
            _renderer.Character = LoadCharacter(_source);
            _renderer.CharacterPosition = new Vector3(128f, 0f, 128f);
            SpawnMonsters(_source);
            _camera.Target = new System.Numerics.Vector3(128f, 2f, 128f);
            Window.Title = $"Mu Android — mundo {_worlds.Current}";
        }
        catch (Exception error) when (error is IOException or InvalidDataException)
        {
            Report($"No se pudo cargar el mundo {_worlds.Current}: {error.Message}");
        }
    }

    /// <summary>
    /// Guarda el mensaje para enseñarlo en pantalla y lo escribe en el registro
    /// del sistema, que es lo único que se puede mirar en un móvil.
    /// </summary>
    private void Report(string message)
    {
        _message = message;
        Console.Error.WriteLine($"[Mu] {message}");
    }

    /// <summary>Reparte los monstruos de muestra alrededor del punto de partida.</summary>
    private void SpawnMonsters(IAssetSource source)
    {
        if (_renderer is null)
        {
            return;
        }

        const float radius = 3f;
        for (var i = 0; i < SampleMonsters.Length; i++)
        {
            var angle = i * MathF.Tau / SampleMonsters.Length;
            var position = new Vector3(
                128f + (MathF.Cos(angle) * radius),
                0f,
                128f + (MathF.Sin(angle) * radius));

            if (LoadMonster(source, SampleMonsters[i], position, angle * 180f / MathF.PI) is { } monster)
            {
                _renderer.Actors.Add(monster);
            }
        }
    }

    private CharacterRenderer? LoadCharacter(IAssetSource source)
    {
        try
        {
            var character = CharacterLoader.Load(source, StartClass, StartWeapon);
            return new CharacterRenderer(GraphicsDevice, character.Model, character.Textures);
        }
        catch (Exception error) when (error is IOException or InvalidDataException)
        {
            // Sin personaje el mapa se sigue viendo, así que no es un fallo fatal.
            return null;
        }
    }

    /// <summary>Carga un monstruo y lo coloca en el mapa.</summary>
    private Actor? LoadMonster(IAssetSource source, string path, Vector3 position, float heading)
    {
        try
        {
            var monster = SkinnedModelLoader.Load(source, path);
            return new Actor
            {
                Renderer = new CharacterRenderer(GraphicsDevice, monster.Model, monster.Textures),
                Position = position,
                HeadingDegrees = heading,
            };
        }
        catch (Exception error) when (error is IOException or InvalidDataException)
        {
            return null;
        }
    }

    protected override void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();
        if (keyboard.IsKeyDown(Keys.Escape))
        {
            Exit();
        }

        var seconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var touch = ReadTouches();
        _touch.Update(touch, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);

        UpdateWorldSelection(keyboard);
        UpdateCamera(keyboard, seconds);
        UpdateCharacter(keyboard, seconds);
        _previousKeyboard = keyboard;
        base.Update(gameTime);
    }

    /// <summary>Los dedos apoyados en la pantalla ahora mismo, en píxeles.</summary>
    private static TouchPoint[] ReadTouches()
    {
        var state = TouchPanel.GetState();
        var points = new TouchPoint[state.Count];
        for (var index = 0; index < state.Count; index++)
        {
            var location = state[index];
            points[index] = new TouchPoint(location.Id, location.Position.X, location.Position.Y);
        }

        return points;
    }

    /// <summary>Cambia de mapa con AvPág (siguiente) y RePág (anterior), o con los botones.</summary>
    private void UpdateWorldSelection(KeyboardState keyboard)
    {
        var step = 0;
        if (Pressed(keyboard, Keys.PageDown) || _touch.MapStep > 0)
        {
            step = 1;
        }
        else if (Pressed(keyboard, Keys.PageUp) || _touch.MapStep < 0)
        {
            step = -1;
        }

        if (step != 0)
        {
            _worlds.Advance(step);
            LoadWorld();
        }
    }

    /// <summary>Cierto si la tecla se acaba de pulsar en este fotograma.</summary>
    private bool Pressed(KeyboardState keyboard, Keys key) =>
        keyboard.IsKeyDown(key) && !_previousKeyboard.IsKeyDown(key);

    private void UpdateCamera(KeyboardState keyboard, float seconds)
    {
        const float turnDegreesPerSecond = 90f;
        const float zoomPerSecond = 1200f;

        if (keyboard.IsKeyDown(Keys.Left))
        {
            _camera.HeadingDegrees += turnDegreesPerSecond * seconds;
        }

        if (keyboard.IsKeyDown(Keys.Right))
        {
            _camera.HeadingDegrees -= turnDegreesPerSecond * seconds;
        }

        if (keyboard.IsKeyDown(Keys.Up))
        {
            _camera.Distance = Math.Max(300f, _camera.Distance - (zoomPerSecond * seconds));
        }

        if (keyboard.IsKeyDown(Keys.Down))
        {
            _camera.Distance = Math.Min(2900f, _camera.Distance + (zoomPerSecond * seconds));
        }
    }

    private void UpdateCharacter(KeyboardState keyboard, float seconds)
    {
        if (_renderer is null)
        {
            return;
        }

        var forward = 0f;
        if (keyboard.IsKeyDown(Keys.W))
        {
            forward += 1f;
        }

        if (keyboard.IsKeyDown(Keys.S))
        {
            forward -= 1f;
        }

        if (keyboard.IsKeyDown(Keys.A))
        {
            _renderer.CharacterHeadingDegrees += 180f * seconds;
        }

        if (keyboard.IsKeyDown(Keys.D))
        {
            _renderer.CharacterHeadingDegrees -= 180f * seconds;
        }

        // El mando mueve y gira a la vez: hacia arriba avanza y hacia los lados
        // gira, que es lo que se espera de un mando analógico.
        if (_touch.IsMoving)
        {
            forward += -_touch.Move.Y;
            _renderer.CharacterHeadingDegrees += _touch.Move.X * 180f * seconds;
        }

        forward = Math.Clamp(forward, -1f, 1f);

        var position = _renderer.CharacterPosition;
        if (forward != 0f)
        {
            var heading = _renderer.CharacterHeadingDegrees * MathF.PI / 180f;
            position += new Vector3(
                MathF.Cos(heading), 0f, MathF.Sin(heading)) * (forward * MoveSpeed * seconds);
            _renderer.CharacterPosition = position;
        }

        if (_renderer.Character is not null)
        {
            _renderer.Character.Action = forward != 0f ? WalkingAction : 0;
        }

        _camera.Target = new System.Numerics.Vector3(position.X, position.Y + 2f, position.Z);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(10, 12, 18));

        if (_renderer is not null)
        {
            var aspect = GraphicsDevice.Viewport.AspectRatio;
            _camera.Update(aspect <= 0f ? 16f / 9f : aspect);
            _renderer.Draw(_camera, (float)gameTime.ElapsedGameTime.TotalSeconds);
        }

        DrawInterface();
        base.Draw(gameTime);
    }

    /// <summary>Dibuja los mandos en pantalla y, si hubo un fallo, el aviso.</summary>
    private void DrawInterface()
    {
        if (_overlay is null)
        {
            return;
        }

        var width = GraphicsDevice.Viewport.Width;
        var height = GraphicsDevice.Viewport.Height;

        if (_message is not null)
        {
            _overlay.DrawText(_message, new Point(24, 24), 2, new Color(255, 200, 150));
        }

        if (!TouchPanel.GetCapabilities().IsConnected)
        {
            return;
        }

        DrawTouchControls(width, height);
    }

    private void DrawTouchControls(int width, int height)
    {
        var radius = TouchControls.Radius(width, height);
        var centre = new Vector2(radius + (radius / 2f), height - radius - (radius / 2f));

        _overlay!.DrawCircle(centre, radius, new Color(255, 255, 255, 40));
        _overlay.DrawCircle(centre + (_touch.Move * radius), radius / 3f, new Color(255, 255, 255, 90));

        DrawButton(TouchControls.MapPreviousButton(width, height), "-");
        DrawButton(TouchControls.MapNextButton(width, height), "+");
    }

    private void DrawButton(Rectangle rectangle, string label)
    {
        _overlay!.DrawBox(rectangle, new Color(255, 255, 255, 40));
        var size = BitmapFont.MeasureWidth(label, 3);
        _overlay.DrawText(
            label,
            new Point(rectangle.Center.X - (size / 2), rectangle.Center.Y - (BitmapFont.MeasureHeight(label, 3) / 2)),
            3);
    }

    protected override void UnloadContent()
    {
        _renderer?.Dispose();
        _overlay?.Dispose();
        base.UnloadContent();
    }
}
