using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mu.Client.Rendering;
using Mu.Client.Ui;
using Mu.Core.Assets;
using Mu.Core.Formats.Bmd;
using Mu.Core.Game;
using Mu.Core.Rendering;
using Mu.Core.World;

namespace Mu.Client.Scenes;

/// <summary>
/// La pantalla donde se juega: carga el mapa, coloca al jugador, a los bots, a
/// los monstruos y a los NPC, y dibuja el mundo con su interfaz. Toda la lógica
/// (movimiento, peleas, niveles) vive en <see cref="Simulation"/>, así que aquí
/// solo se traduce lo que toca el jugador a lo que entiende la simulación y se
/// dibuja el resultado.
/// </summary>
public sealed class GameScene : Scene
{
    /// <summary>Arma que lleva el personaje, si el cliente la trae.</summary>
    private const string WeaponPath = "Item/Sword01.bmd";

    /// <summary>Radio del mando en pantalla, como fracción del alto del lienzo.</summary>
    private const float JoystickRadius = 90f;

    /// <summary>Altura de los ojos por encima del suelo, para la cámara.</summary>
    private const float EyeHeight = 2f;

    /// <summary>Mundos que se pueden visitar con los botones de mapa, empezando por Lorencia.</summary>
    private static readonly int[] Worlds = [1, 2, 3, 7, 8, 4, 5, 9, 10];

    /// <summary>Nombre que se enseña de cada mundo.</summary>
    private static readonly Dictionary<int, string> WorldNames = new()
    {
        [1] = "LORENCIA",
        [2] = "DUNGEON",
        [3] = "DEVIAS",
        [4] = "NORIA",
        [5] = "LOSTTOWER",
        [7] = "ATLANS",
        [8] = "TARKAN",
        [9] = "ICARUS",
        [10] = "KALIMA",
    };

    private readonly int _slot;

    private IAssetSource? _source;
    private WorldAssets? _world;
    private WorldRenderer? _renderer;
    private ModelCache? _cache;
    private CharacterRenderer? _player;
    private RenderTarget2D? _canvas;
    private Simulation? _simulation;
    private readonly Camera _camera = new();

    private readonly List<ActorView> _bots = [];
    private readonly List<ActorView> _monsters = [];
    private readonly List<ActorView> _npcs = [];

    private int _worldIndex;
    private float _messageLeft;
    private string _message = string.Empty;
    private bool _showOptions;
    private bool _showInventory;
    private bool _showSkills;
    private bool _dirty;

    private int? _joystickId;
    private Vector2 _joystickOrigin;
    private Vector2 _joystickOffset;

    /// <param name="slot">El espacio de personaje que se va a jugar.</param>
    /// <param name="startWorld">Mundo con el que se empieza (1 = Lorencia).</param>
    public GameScene(int slot, int startWorld = 0)
    {
        _slot = slot;

        var index = Array.IndexOf(Worlds, startWorld);
        _worldIndex = index < 0 ? 0 : index;
    }

    public override void Load()
    {
        _source = Host.OpenAssets();
        _cache = new ModelCache(Host.Device);

        if (_source is null)
        {
            Report("NO SE ENCONTRARON LOS DATOS DEL JUEGO EN EL MOVIL");
            return;
        }

        LoadWorld(Worlds[_worldIndex]);
    }

    /// <summary>
    /// Prepara el lienzo del mundo con el tamaño de ahora. Se comprueba cada
    /// fotograma porque el tamaño de la pantalla puede cambiar (al girar el
    /// móvil), y el lienzo del mundo tiene que seguirle.
    /// </summary>
    private void EnsureCanvas()
    {
        var device = Host.Device;
        if (_canvas is not null &&
            _canvas.Width == VirtualCanvas.Width &&
            _canvas.Height == VirtualCanvas.Height)
        {
            return;
        }

        _canvas?.Dispose();
        _canvas = new RenderTarget2D(
            device,
            VirtualCanvas.Width,
            VirtualCanvas.Height,
            false,
            SurfaceFormat.Color,
            DepthFormat.Depth24);
    }

    public override void Unload()
    {
        _renderer?.Dispose();
        _cache?.Dispose();
        _canvas?.Dispose();

        // El progreso se guarda al salir, para no perder lo conseguido.
        SaveProgress();
    }

    private void LoadWorld(int world)
    {
        if (_source is null || _cache is null)
        {
            return;
        }

        try
        {
            _renderer?.Dispose();
            _bots.Clear();
            _monsters.Clear();
            _npcs.Clear();

            _world = WorldLoader.Load(_source, world);
            _renderer = new WorldRenderer(Host.Device, _world);

            var name = WorldNames.GetValueOrDefault(world, $"MUNDO {world}");
            var start = new System.Numerics.Vector3(128f, GroundHeight(128f, 128f), 128f);
            var character = BuildPlayerCharacter();
            _simulation = new Simulation(character, start, GroundHeight, seed: _slot * 7919 + world, mapName: name);
            _simulation.ProgressChanged += () => _dirty = true;

            _player = LoadPlayer();
            PlaceActors();
            _camera.Target = new System.Numerics.Vector3(start.X, start.Y + EyeHeight, start.Z);
            Report($"{name} (MUNDO {world})");
        }
        catch (Exception error) when (error is IOException or InvalidDataException or FileNotFoundException)
        {
            Report($"NO SE PUDO CARGAR EL MUNDO {world}: {error.Message}");
        }
    }

    /// <summary>Reconstruye el personaje del jugador desde el perfil guardado.</summary>
    private Character BuildPlayerCharacter()
    {
        var saved = Host.Profile.Slots()[_slot] ?? throw new InvalidOperationException("Ese espacio de personaje esta vacio.");

        var character = Character.Create(saved.Name, (CharacterClass)saved.Class);
        character.LoadProgress(saved.Level, saved.Experience);
        return character;
    }

    private CharacterRenderer? LoadPlayer()
    {
        if (_source is null || _cache is null)
        {
            return null;
        }

        var saved = Host.Profile.Slots()[_slot];
        var classNumber = saved is null ? 1 : CharacterClasses.ModelNumber((CharacterClass)saved.Class);

        return _cache.Create(
            $"player-{classNumber}",
            () =>
            {
                var assets = CharacterLoader.Load(_source, classNumber, WeaponPath);
                return (assets.Model, assets.Textures);
            });
    }

    /// <summary>Coloca a los bots, los monstruos y los NPC donde dice la simulación.</summary>
    private void PlaceActors()
    {
        if (_source is null || _cache is null || _simulation is null)
        {
            return;
        }

        var botRenderer = _cache.Create(
            "player-1",
            () =>
            {
                var assets = CharacterLoader.Load(_source, 1, WeaponPath);
                return (assets.Model, assets.Textures);
            });

        foreach (var bot in _simulation.Bots)
        {
            if (botRenderer is not null)
            {
                _bots.Add(new ActorView(botRenderer, () => bot.Position, () => bot.HeadingDegrees, () => MapBotAction(bot)));
            }
        }

        foreach (var monster in _simulation.Monsters)
        {
            var path = monster.Kind.ModelPath;
            var renderer = _cache.Create(path, () =>
            {
                var assets = SkinnedModelLoader.Load(_source, path);
                return (assets.Model, assets.Textures);
            });

            if (renderer is not null)
            {
                _monsters.Add(new ActorView(renderer, () => monster.Body.Position, () => monster.Body.HeadingDegrees, () => 0));
            }
        }

        foreach (var npc in _simulation.Npcs)
        {
            var path = npc.ModelPath;
            var renderer = _cache.Create(path, () =>
            {
                var assets = SkinnedModelLoader.Load(_source, path);
                return (assets.Model, assets.Textures);
            });

            if (renderer is not null)
            {
                _npcs.Add(new ActorView(renderer, () => npc.Position, () => 180f, () => 0));
            }
        }
    }

    private static int MapBotAction(Bot bot) => bot.Action switch
    {
        BotAction.Walking => (int)PlayerAction.WalkMale,
        BotAction.Attacking => (int)PlayerAction.AttackSwordRight1,
        BotAction.Dead => (int)PlayerAction.Die1,
        _ => 0,
    };

    /// <summary>Altura del suelo en un punto del mapa.</summary>
    private float GroundHeight(float x, float z) =>
        _world is null ? 0f : TerrainHeightSampler.Sample(_world.Heights, x, z);

    public override void Update(float seconds, InputState input)
    {
        if (_messageLeft > 0f)
        {
            _messageLeft = MathF.Max(0f, _messageLeft - seconds);
        }

        UpdateButtons(input);

        if (_simulation is null)
        {
            return;
        }

        var move = ReadMove(input);
        var attack = input.Holding(Hud.AttackButton(VirtualCanvas.Width, VirtualCanvas.Height));
        _simulation.Update(seconds, move, attack);

        FollowPlayer(seconds);
        SaveIfNeeded();
    }

    /// <summary>El movimiento pedido: el mando táctil o el teclado.</summary>
    private System.Numerics.Vector2 ReadMove(InputState input)
    {
        var keyboard = input.Keyboard;
        var forward = 0f;
        if (keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.W))
        {
            forward += 1f;
        }

        if (keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.S))
        {
            forward -= 1f;
        }

        var turn = 0f;
        if (keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.A))
        {
            turn -= 1f;
        }

        if (keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.D))
        {
            turn += 1f;
        }

        if (forward != 0f || turn != 0f)
        {
            return new System.Numerics.Vector2(turn, -forward);
        }

        return _joystickId is null
            ? System.Numerics.Vector2.Zero
            : new System.Numerics.Vector2(_joystickOffset.X / JoystickRadius, _joystickOffset.Y / JoystickRadius);
    }

    /// <summary>La cámara sigue al jugador por detrás.</summary>
    private void FollowPlayer(float seconds)
    {
        if (_simulation is null)
        {
            return;
        }

        var target = _simulation.PlayerPosition;
        _camera.Target = new System.Numerics.Vector3(target.X, target.Y + EyeHeight, target.Z);

        // El rumbo de la cámara se acerca poco a poco al del personaje, para que
        // la vista acompañe al girar sin dar tirones.
        var difference = Normalize(_simulation.PlayerHeadingDegrees - _camera.HeadingDegrees);
        _camera.HeadingDegrees += difference * MathF.Min(1f, seconds * 4f);
    }

    /// <summary>Reparte los toques entre el mando y los botones del interfaz.</summary>
    private void UpdateButtons(InputState input)
    {
        var width = VirtualCanvas.Width;
        var height = VirtualCanvas.Height;

        if (input.Tapped(Hud.OptionsButton(width)))
        {
            _showOptions = !_showOptions;
            _showInventory = false;
            _showSkills = false;
        }

        if (input.Tapped(Hud.InventoryButton(width, height)))
        {
            _showInventory = !_showInventory;
            _showOptions = false;
            _showSkills = false;
        }

        if (input.Tapped(Hud.SkillButton(width, height)))
        {
            _showSkills = !_showSkills;
            _showOptions = false;
            _showInventory = false;
        }

        if (input.Tapped(Hud.TalkButton(width)))
        {
            if (_simulation?.TalkToNearbyNpc() != true)
            {
                Report("NO HAY NADIE CERCA CON QUIEN HABLAR");
            }
        }

        if (_showOptions)
        {
            if (input.Tapped(PreviousWorldButton()))
            {
                StepWorld(-1);
            }
            else if (input.Tapped(NextWorldButton()))
            {
                StepWorld(1);
            }
        }

        UpdateJoystick(input, width, height);
    }

    /// <summary>Pasa al mapa siguiente o al anterior de la lista.</summary>
    private void StepWorld(int step)
    {
        _worldIndex = ((_worldIndex + step) % Worlds.Length + Worlds.Length) % Worlds.Length;
        LoadWorld(Worlds[_worldIndex]);
    }

    /// <summary>
    /// El mando nace donde se apoya el dedo, mientras no sea sobre un botón, y
    /// desaparece al levantarlo. Así se maneja con una mano sin mirar.
    /// </summary>
    private void UpdateJoystick(InputState input, int width, int height)
    {
        var stick = (TouchPoint?)null;
        foreach (var point in input.Points)
        {
            if (point.Id == _joystickId)
            {
                stick = point;
                break;
            }
        }

        if (stick is null && _joystickId is not null)
        {
            _joystickId = null;
            _joystickOffset = Vector2.Zero;
        }

        if (_joystickId is null)
        {
            foreach (var point in input.Points)
            {
                if (!input.IsNew(point) || point.X > width / 2f)
                {
                    continue;
                }

                if (Hud.AttackButton(width, height).Contains((int)point.X, (int)point.Y))
                {
                    continue;
                }

                _joystickId = point.Id;
                _joystickOrigin = new Vector2(point.X, point.Y);
                _joystickOffset = Vector2.Zero;
                break;
            }
        }

        if (stick is { } current)
        {
            var offset = new Vector2(current.X, current.Y) - _joystickOrigin;
            var length = offset.Length();
            _joystickOffset = length > JoystickRadius ? offset * (JoystickRadius / length) : offset;
        }
    }

    private void SaveIfNeeded()
    {
        if (!_dirty)
        {
            return;
        }

        _dirty = false;
        SaveProgress();
    }

    private void SaveProgress()
    {
        if (_simulation is not null)
        {
            Host.Profile.SaveProgress(_slot, _simulation.Player);
        }
    }

    private void Report(string message)
    {
        _message = message;
        _messageLeft = 4f;
        Console.Error.WriteLine($"[Mu] {message}");
    }

    public override void Draw(InputState input)
    {
        var device = Host.Device;
        EnsureCanvas();
        device.SetRenderTarget(_canvas);
        device.Clear(new Color(10, 12, 18));

        if (_renderer is not null && _simulation is not null)
        {
            _camera.Update(VirtualCanvas.Aspect);
            _renderer.Draw(_camera);

            var seconds = 1f / 60f;
            DrawActors(_monsters, seconds);
            DrawActors(_bots, seconds);
            DrawActors(_npcs, seconds);

            if (_player is not null)
            {
                _player.Action = _simulation.PlayerAction;
                _player.Draw(ModelOrientation.ToWorld(_simulation.PlayerPosition, _simulation.PlayerHeadingDegrees), _camera, seconds);
            }
        }

        device.SetRenderTarget(null);
        device.Clear(Color.Black);

        if (_canvas is not null)
        {
            // El lienzo se pega tal cual (ocupa todo el lienzo virtual) y es la
            // transformación la que lo agranda a la pantalla; por eso se pide
            // suavizado, que aquí sí conviene.
            Host.Text.Begin(smooth: true);
            Host.Text.Texture(_canvas, new Rectangle(0, 0, VirtualCanvas.Width, VirtualCanvas.Height));
            Host.Text.End();
        }

        if (_simulation is null)
        {
            DrawLoading();
            return;
        }

        Host.Text.Begin();
        Hud.Draw(
            Host.Text,
            _simulation,
            input,
            VirtualCanvas.Width,
            VirtualCanvas.Height,
            _joystickId is null ? null : _joystickOrigin,
            _joystickOffset,
            JoystickRadius);

        DrawNameTags();

        if (_messageLeft > 0f)
        {
            Host.Text.TextCentered(_message, new Rectangle(0, 150, VirtualCanvas.Width, 30), 2f, new Color(255, 230, 160));
        }

        if (_showOptions)
        {
            DrawOptions();
        }

        if (_showInventory)
        {
            DrawPanel("INVENTARIO", ["ESPADA BASICA", "POCION DE VIDA", "POCION DE MANA"], "TOCA INV PARA CERRAR");
        }

        if (_showSkills)
        {
            DrawPanel("HABILIDADES", ["GOLPE FUERTE", "DEFENSA", "CURACION"], "TOCA HAB PARA CERRAR");
        }

        Host.Text.End();
    }

    private void DrawActors(List<ActorView> actors, float seconds)
    {
        foreach (var actor in actors)
        {
            actor.Renderer.Action = actor.Action();
            actor.Renderer.Draw(ModelOrientation.ToWorld(actor.Position(), actor.Heading()), _camera, seconds);
        }
    }

    /// <summary>Pone el nombre de bots y monstruos cerca de ellos, como en el original.</summary>
    private void DrawNameTags()
    {
        if (_simulation is null)
        {
            return;
        }

        foreach (var bot in _simulation.Bots)
        {
            Tag(bot.Name, bot.Position, new Color(150, 200, 255));
        }

        foreach (var monster in _simulation.Monsters)
        {
            if (monster.Body.IsAlive)
            {
                Tag(monster.Body.Name, monster.Body.Position, new Color(240, 160, 150));
            }
        }

        foreach (var npc in _simulation.Npcs)
        {
            Tag(npc.Name, npc.Position, new Color(240, 220, 130));
        }
    }

    private void Tag(string text, System.Numerics.Vector3 position, Color color)
    {
        var screen = _camera.Project(new System.Numerics.Vector3(position.X, position.Y + 2.2f, position.Z), VirtualCanvas.Width, VirtualCanvas.Height);
        if (screen is not { } point)
        {
            return;
        }

        var size = BitmapFont.MeasureWidth(text, 1);
        Host.Text.Text(text, new Vector2(point.X - (size / 2f), point.Y), 1f, color);
    }

    private void DrawLoading()
    {
        Host.Text.Begin();
        Host.Text.TextCentered(_message, new Rectangle(0, (VirtualCanvas.Height / 2) - 20, VirtualCanvas.Width, 40), 2.4f, new Color(255, 210, 150));
        Host.Text.End();
    }

    private void DrawOptions()
    {
        var box = new Rectangle((VirtualCanvas.Width / 2) - 230, (VirtualCanvas.Height / 2) - 130, 460, 260);
        Theme.Panel(Host.Text, box);
        Host.Text.TextCentered("OPCIONES", new Rectangle(box.X, box.Y + 16, box.Width, 34), 2.6f, Theme.Title);

        var world = Worlds[_worldIndex];
        Host.Text.Text($"MUNDO {world}", new Vector2(box.X + 28, box.Y + 70), 2f, Theme.Text);
        Host.Text.Text($"BOTS {_simulation?.Bots.Count ?? 0}   BICHOS {_simulation?.Monsters.Count ?? 0}", new Vector2(box.X + 28, box.Y + 102), 1.6f, Theme.TextDim);

        var previous = PreviousWorldButton();
        var next = NextWorldButton();
        Theme.Button(Host.Text, previous, "< MAPA", false, 1.6f);
        Theme.Button(Host.Text, next, "MAPA >", false, 1.6f);

        Host.Text.TextCentered("TOCA OPC PARA CERRAR", new Rectangle(box.X, box.Bottom - 44, box.Width, 26), 1.5f, Theme.TextDim);
    }

    private void DrawPanel(string title, string[] lines, string hint)
    {
        var box = new Rectangle((VirtualCanvas.Width / 2) - 240, (VirtualCanvas.Height / 2) - 130, 480, 260);
        Theme.Panel(Host.Text, box);
        Host.Text.TextCentered(title, new Rectangle(box.X, box.Y + 16, box.Width, 34), 2.6f, Theme.Title);

        for (var i = 0; i < lines.Length; i++)
        {
            Host.Text.Text($"{i + 1}. {lines[i]}", new Vector2(box.X + 34, box.Y + 80 + (i * 34)), 1.8f, Theme.Text);
        }

        Host.Text.TextCentered(hint, new Rectangle(box.X, box.Bottom - 44, box.Width, 26), 1.5f, Theme.TextDim);
    }

    /// <summary>El botón de mapa anterior, dentro del panel de opciones.</summary>
    public static Rectangle PreviousWorldButton()
    {
        var box = new Rectangle((VirtualCanvas.Width / 2) - 230, (VirtualCanvas.Height / 2) - 130, 460, 260);
        return new Rectangle(box.X + 28, box.Bottom - 92, 190, 40);
    }

    /// <summary>El botón de mapa siguiente, dentro del panel de opciones.</summary>
    public static Rectangle NextWorldButton()
    {
        var box = new Rectangle((VirtualCanvas.Width / 2) - 230, (VirtualCanvas.Height / 2) - 130, 460, 260);
        return new Rectangle(box.Right - 218, box.Bottom - 92, 190, 40);
    }

    private static float Normalize(float degrees)
    {
        degrees %= 360f;
        if (degrees > 180f)
        {
            degrees -= 360f;
        }
        else if (degrees < -180f)
        {
            degrees += 360f;
        }

        return degrees;
    }

    /// <summary>Un actor dibujado: su dibujante y de dónde saca posición y acción.</summary>
    private sealed record ActorView(
        CharacterRenderer Renderer,
        Func<System.Numerics.Vector3> Position,
        Func<float> Heading,
        Func<int> Action);
}
