using Microsoft.Xna.Framework;
using Mu.Client.Rendering;
using Mu.Core.Game;

namespace Mu.Client.Ui;

/// <summary>
/// El interfaz del juego mientras se juega: las barras de vida, mana y
/// experiencia, el minimapa, el chat y los botones. Todo se mide y se dibuja
/// sobre el lienzo virtual, así que se ve igual en cualquier pantalla. La
/// colocación de las piezas está en métodos aparte para que se pueda probar sin
/// dibujar nada.
/// </summary>
public static class Hud
{
    /// <summary>Margen hasta el borde, en píxeles.</summary>
    private const int Margin = 16;

    /// <summary>Ancho del panel de estado (nombre y barras).</summary>
    private const int PanelWidth = 340;

    /// <summary>Alto de cada barra.</summary>
    private const int BarHeight = 22;

    /// <summary>Lado del minimapa.</summary>
    private const int MinimapSize = 190;

    /// <summary>Lado del botón grande de ataque.</summary>
    private const int AttackSize = 130;

    /// <summary>Lado de los botones pequeños.</summary>
    private const int SmallButton = 64;

    /// <summary>Cuántas líneas de chat se ven.</summary>
    private const int ChatLines = 6;

    /// <summary>Panel de estado, arriba a la izquierda.</summary>
    public static Rectangle StatusPanel(int width) => new(Margin, Margin, PanelWidth, 128);

    /// <summary>Barra de vida.</summary>
    public static Rectangle HealthBar(int width) =>
        new(Margin + 12, Margin + 56, PanelWidth - 24, BarHeight);

    /// <summary>Barra de mana, debajo de la de vida.</summary>
    public static Rectangle ManaBar(int width) =>
        new(Margin + 12, Margin + 82, PanelWidth - 24, BarHeight);

    /// <summary>Barra de experiencia, debajo de la de mana.</summary>
    public static Rectangle ExperienceBar(int width) =>
        new(Margin + 12, Margin + 108, PanelWidth - 24, 14);

    /// <summary>Minimapa, arriba a la derecha.</summary>
    public static Rectangle Minimap(int width) =>
        new(width - Margin - MinimapSize, Margin, MinimapSize, MinimapSize);

    /// <summary>Botón de ataque, abajo a la derecha.</summary>
    public static Rectangle AttackButton(int width, int height) =>
        new(width - Margin - AttackSize, height - Margin - AttackSize, AttackSize, AttackSize);

    /// <summary>Botón de inventario, junto al de ataque.</summary>
    public static Rectangle InventoryButton(int width, int height) =>
        new(width - Margin - AttackSize - SmallButton - 12, height - Margin - SmallButton, SmallButton, SmallButton);

    /// <summary>Botón de habilidades, encima del de inventario.</summary>
    public static Rectangle SkillButton(int width, int height) =>
        new(width - Margin - AttackSize - SmallButton - 12, height - Margin - (SmallButton * 2) - 12, SmallButton, SmallButton);

    /// <summary>Botón de opciones, debajo del minimapa.</summary>
    public static Rectangle OptionsButton(int width) =>
        new(width - Margin - SmallButton, Margin + MinimapSize + 12, SmallButton, SmallButton);

    /// <summary>Botón para hablar con el NPC que esté cerca.</summary>
    public static Rectangle TalkButton(int width) =>
        new(width - Margin - (SmallButton * 2) - 12, Margin + MinimapSize + 12, SmallButton * 2, SmallButton);

    /// <summary>Dónde se escribe el chat, abajo al centro.</summary>
    public static Rectangle ChatArea(int width, int height) =>
        new(Margin + 150, height - Margin - (ChatLines * 22) - 8, (width / 2) - 100, (ChatLines * 22) + 8);

    /// <summary>Dibuja todo el interfaz del juego.</summary>
    /// <param name="renderer">Con qué se dibuja.</param>
    /// <param name="simulation">El mundo que se está jugando.</param>
    /// <param name="input">Lo que está tocando el jugador.</param>
    /// <param name="width">Ancho del lienzo.</param>
    /// <param name="height">Alto del lienzo.</param>
    /// <param name="joystickCentre">Dónde está el mando, o null si no se está usando.</param>
    /// <param name="joystickOffset">Cuánto se ha empujado el mando.</param>
    /// <param name="joystickRadius">Radio del mando.</param>
    public static void Draw(
        TextRenderer renderer,
        Simulation simulation,
        InputState input,
        int width,
        int height,
        Vector2? joystickCentre,
        Vector2 joystickOffset,
        float joystickRadius)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentNullException.ThrowIfNull(simulation);
        ArgumentNullException.ThrowIfNull(input);

        DrawStatus(renderer, simulation, width);
        DrawMinimap(renderer, simulation, width);
        DrawButtons(renderer, input, width, height);
        DrawChat(renderer, simulation, width, height);

        if (joystickCentre is { } centre)
        {
            renderer.Circle(centre, joystickRadius, new Color(255, 255, 255, 40));
            renderer.CircleOutline(centre, joystickRadius, new Color(220, 210, 180, 120), 3f);
            renderer.Circle(centre + joystickOffset, joystickRadius / 3f, new Color(230, 220, 190, 180));
        }
    }

    /// <summary>Nombre, nivel y las tres barras.</summary>
    private static void DrawStatus(TextRenderer renderer, Simulation simulation, int width)
    {
        var panel = StatusPanel(width);
        Theme.Panel(renderer, panel);

        var character = simulation.Player;
        renderer.Text($"{character.Name}   NIVEL {character.Level}", new Vector2(panel.X + 12, panel.Y + 12), 2f, Theme.Title);

        var health = HealthBar(width);
        Theme.Bar(renderer, health, character.HealthFraction, Theme.Health, $"{character.Health}/{character.MaxHealth}");

        var mana = ManaBar(width);
        Theme.Bar(renderer, mana, character.ManaFraction, Theme.Mana, $"{character.Mana}/{character.MaxMana}");

        var experience = ExperienceBar(width);
        Theme.Bar(renderer, experience, character.ExperienceFraction, Theme.Experience, string.Empty);
    }

    /// <summary>El minimapa con el jugador, los bots y los monstruos.</summary>
    private static void DrawMinimap(TextRenderer renderer, Simulation simulation, int width)
    {
        var map = Minimap(width);
        Theme.Panel(renderer, map);

        var inner = new Rectangle(map.X + 4, map.Y + 4, map.Width - 8, map.Height - 8);
        renderer.Fill(inner, new Color(18, 30, 18, 235));

        // El mapa se dibuja alrededor del jugador, con el mundo medido en
        // unidades y el jugador en el centro.
        const float range = 60f;
        var centre = new Vector2(inner.Center.X, inner.Center.Y);
        var scale = (inner.Width / 2f) / range;

        foreach (var monster in simulation.Monsters)
        {
            if (monster.Body.IsAlive)
            {
                Dot(renderer, centre, scale, inner, simulation.PlayerPosition, monster.Body.Position, new Color(230, 70, 60));
            }
        }

        foreach (var bot in simulation.Bots)
        {
            if (bot.Body.IsAlive)
            {
                Dot(renderer, centre, scale, inner, simulation.PlayerPosition, bot.Position, new Color(90, 170, 255));
            }
        }

        foreach (var npc in simulation.Npcs)
        {
            Dot(renderer, centre, scale, inner, simulation.PlayerPosition, npc.Position, new Color(240, 220, 120));
        }

        renderer.Circle(centre, 4f, Color.White);
        renderer.Frame(map, Theme.PanelBorder, 2);
    }

    private static void Dot(
        TextRenderer renderer,
        Vector2 centre,
        float scale,
        Rectangle inner,
        System.Numerics.Vector3 player,
        System.Numerics.Vector3 position,
        Color color)
    {
        var offset = new Vector2(position.X - player.X, position.Z - player.Z) * scale;
        var point = centre + offset;
        if (!inner.Contains((int)point.X, (int)point.Y))
        {
            return;
        }

        renderer.Circle(point, 3.5f, color);
    }

    /// <summary>Los botones de ataque, inventario, habilidades, hablar y opciones.</summary>
    private static void DrawButtons(TextRenderer renderer, InputState input, int width, int height)
    {
        var attack = AttackButton(width, height);
        var attackDown = input.Holding(attack);
        renderer.Fill(attack, attackDown ? new Color(170, 60, 50, 235) : new Color(120, 40, 34, 220));
        renderer.CircleOutline(attack.Center.ToVector2(), attack.Width / 2f, Theme.PanelBorder, 3f);
        renderer.TextCentered("ATACAR", attack, 2f, new Color(255, 230, 210));

        Theme.Button(renderer, InventoryButton(width, height), "INV", input.Holding(InventoryButton(width, height)), 1.6f);
        Theme.Button(renderer, SkillButton(width, height), "HAB", input.Holding(SkillButton(width, height)), 1.6f);
        Theme.Button(renderer, TalkButton(width), "HABLAR", input.Holding(TalkButton(width)), 1.6f);
        Theme.Button(renderer, OptionsButton(width), "OPC", input.Holding(OptionsButton(width)), 1.6f);
    }

    /// <summary>Las últimas líneas del chat.</summary>
    private static void DrawChat(TextRenderer renderer, Simulation simulation, int width, int height)
    {
        var area = ChatArea(width, height);
        renderer.Fill(area, new Color(0, 0, 0, 130));

        var lines = simulation.Chat.Tail(ChatLines);
        var lineHeight = 22;
        var y = area.Bottom - (lines.Count * lineHeight);

        foreach (var line in lines)
        {
            var color = line.Kind switch
            {
                ChatKind.System => new Color(255, 214, 120),
                ChatKind.Player => new Color(230, 230, 230),
                ChatKind.Bot => new Color(150, 200, 255),
                ChatKind.Npc => new Color(160, 240, 170),
                _ => Theme.Text,
            };

            renderer.Text(line.Text, new Vector2(area.X + 6, y), 1.4f, color);
            y += lineHeight;
        }
    }
}
