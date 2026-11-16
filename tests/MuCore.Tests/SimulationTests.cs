using System.Numerics;
using Mu.Core.Game;
using Xunit;

namespace Mu.Core.Tests;

/// <summary>Comprueba que los bots se mueven, pelean y vuelven, sin dibujar nada.</summary>
public class SimulationTests
{
    /// <summary>Suelo llano en la altura indicada, para no depender del mapa real.</summary>
    private static Func<float, float, float> Flat(float height = 0f) => (_, _) => height;

    private static Simulation NewSimulation(int seed = 1) =>
        new(Character.Create("HEROE", CharacterClass.BladeKnight), new Vector3(128f, 0f, 128f), Flat(), seed);

    [Fact]
    public void NaceConDiezBotsYMonstruos()
    {
        var simulation = NewSimulation();

        Assert.Equal(Simulation.BotCount, simulation.Bots.Count);
        Assert.True(simulation.Bots.Count >= 8, "hacen falta al menos 8 bots");
        Assert.True(simulation.Monsters.Count > 0);
        Assert.Equal(2, simulation.Npcs.Count);
    }

    [Fact]
    public void LosBotsSeMuevenConElTiempo()
    {
        var simulation = NewSimulation();
        var before = simulation.Bots.Select(b => b.Position).ToArray();

        for (var i = 0; i < 200; i++)
        {
            simulation.Update(0.05f, Vector2.Zero, attack: false);
        }

        var moved = 0;
        for (var i = 0; i < simulation.Bots.Count; i++)
        {
            if (Vector3.Distance(simulation.Bots[i].Position, before[i]) > 0.5f)
            {
                moved++;
            }
        }

        Assert.True(moved >= 3, $"solo se movieron {moved} bots");
    }

    [Fact]
    public void LosBotsSeQuedanSobreElSuelo()
    {
        var simulation = new Simulation(
            Character.Create("HEROE", CharacterClass.Elf),
            new Vector3(128f, 0f, 128f),
            Flat(1.65f));

        for (var i = 0; i < 100; i++)
        {
            simulation.Update(0.05f, Vector2.Zero, attack: false);
        }

        Assert.All(simulation.Bots, bot => Assert.Equal(1.65f, bot.Position.Y, 2));
        Assert.Equal(1.65f, simulation.PlayerPosition.Y, 2);
    }

    [Fact]
    public void LosBotsPeleanYAlgunMonstruoMuere()
    {
        var simulation = NewSimulation();

        for (var i = 0; i < 3000; i++)
        {
            simulation.Update(0.05f, Vector2.Zero, attack: false);
        }

        var wounded = simulation.Monsters.Count(m => m.Body.Health < m.Body.MaxHealth);
        var dead = simulation.Monsters.Count(m => !m.Body.IsAlive);
        var botHits = simulation.Chat.Messages.Count(m => m.Kind == ChatKind.Bot);

        Assert.True(botHits > 0, "los bots no han derrotado a nadie");
        Assert.True(wounded + dead > 0, "los monstruos siguen intactos");
    }

    [Fact]
    public void LosMonstruosMuertosVuelven()
    {
        var simulation = NewSimulation();
        var monster = simulation.Monsters[0];

        monster.TakeDamage(100000);
        Assert.False(monster.Body.IsAlive);

        monster.Update(Monster.RespawnSeconds + 0.1f);

        Assert.True(monster.Body.IsAlive);
        Assert.Equal(monster.Body.MaxHealth, monster.Body.Health);
    }

    [Fact]
    public void ElJugadorSeMueveHaciaDelanteConElMando()
    {
        var simulation = NewSimulation();
        var before = simulation.PlayerPosition;

        simulation.Update(0.5f, new Vector2(0f, -1f), attack: false);

        Assert.True(Vector3.Distance(before, simulation.PlayerPosition) > 1f);
    }

    [Fact]
    public void ElJugadorMiraHaciaDondeAnda()
    {
        var simulation = NewSimulation();
        var before = simulation.PlayerPosition;

        // El mando a la derecha: el personaje anda hacia el este y mira allí.
        simulation.Update(0.5f, new Vector2(1f, 0f), attack: false);

        Assert.True(simulation.PlayerPosition.X > before.X, "debe andar hacia el este");
        Assert.Equal(0f, simulation.PlayerHeadingDegrees, 1f);
    }

    [Fact]
    public void ElJugadorAndaComoElClienteOriginal()
    {
        // 3,5 casillas por segundo: es lo que casa con el paso de la animación,
        // de modo que los pies no patinan.
        var simulation = NewSimulation();
        var before = simulation.PlayerPosition;

        simulation.Update(1f, new Vector2(1f, 0f), attack: false);

        var travelled = Vector3.Distance(before, simulation.PlayerPosition);
        Assert.Equal(Simulation.PlayerSpeed, travelled, 0.05f);
        Assert.Equal(3.5f, Simulation.PlayerSpeed, 0.001f);
    }

    [Fact]
    public void MatarUnMonstruoDaExperiencia()
    {
        var simulation = NewSimulation();
        var monster = simulation.Monsters[0];

        // Se coloca al monstruo al lado del jugador y se le deja a un golpe.
        monster.Body.Position = simulation.PlayerPosition;
        monster.TakeDamage(monster.Body.MaxHealth - 1);

        simulation.Update(0.05f, Vector2.Zero, attack: true);

        Assert.True(simulation.Player.Experience > 0 || simulation.Player.Level > 1);
    }

    [Fact]
    public void AtacarSinNadieCercaNoHaceNada()
    {
        var simulation = NewSimulation();
        foreach (var monster in simulation.Monsters)
        {
            monster.Body.Position = new Vector3(1f, 0f, 1f);
        }

        simulation.Update(0.05f, Vector2.Zero, attack: true);

        Assert.Equal(0, simulation.Player.Experience);
        Assert.Equal(1, simulation.Player.Level);
    }

    [Fact]
    public void HablarConUnNpcEscribeEnElChat()
    {
        // Se empieza la partida justo donde está el NPC, que es cuando te habla.
        var npc = Npc.Lorencia[0];
        var simulation = new Simulation(
            Character.Create("HEROE", CharacterClass.Elf),
            npc.Position,
            Flat(),
            seed: 3);

        var talked = simulation.TalkToNearbyNpc();

        Assert.True(talked);
        Assert.Contains(simulation.Chat.Messages, m => m.Kind == ChatKind.Npc);
    }

    [Fact]
    public void ElChatNoCreceSinLimite()
    {
        var chat = new ChatLog();
        for (var i = 0; i < ChatLog.Capacity * 3; i++)
        {
            chat.Add(ChatKind.Bot, $"LINEA {i}");
        }

        Assert.Equal(ChatLog.Capacity, chat.Messages.Count);
        Assert.Contains(chat.Messages, m => m.Text == $"LINEA {(ChatLog.Capacity * 3) - 1}");
    }
}
