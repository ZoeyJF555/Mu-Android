using Microsoft.Xna.Framework;
using Mu.Client.Rendering;
using Mu.Client.Ui;
using Mu.Core.Game;

namespace Mu.Client.Scenes;

/// <summary>
/// La pantalla donde se eligen los tres espacios de personaje. Se ve el nombre,
/// la clase y el nivel de cada uno; los vacíos se pueden crear y los llenos
/// borrar. Es el paso que hay entre entrar y jugar.
/// </summary>
public sealed class CharacterSelectScene : Scene
{
    private string _message = string.Empty;
    private float _blink;

    public override void Load()
    {
        var free = Host.Profile.FirstFreeSlot();
        _message = free < 0
            ? "LOS TRES ESPACIOS ESTAN OCUPADOS"
            : "TOCA UN ESPACIO PARA JUGAR O CREAR";
    }

    public override void Update(float seconds, InputState input)
    {
        _blink += seconds;

        var slots = Host.Profile.Slots();
        for (var i = 0; i < slots.Length; i++)
        {
            var bounds = SlotBox(i);
            if (!input.Tapped(bounds))
            {
                continue;
            }

            if (input.Tapped(DeleteButton(i)))
            {
                Delete(i);
            }
            else if (slots[i] is null)
            {
                Host.Change(new CharacterCreateScene(i));
            }
            else
            {
                Host.Change(new GameScene(i));
            }

            return;
        }
    }

    private void Delete(int slot)
    {
        Host.Profile.DeleteCharacter(slot);
        _message = "PERSONAJE BORRADO";
    }

    public override void Draw(InputState input)
    {
        var renderer = Host.Text;
        renderer.Begin();

        renderer.Fill(new Rectangle(0, 0, VirtualCanvas.Width, VirtualCanvas.Height), new Color(8, 10, 16));
        renderer.TextCentered("ELIGE PERSONAJE", new Rectangle(0, 60, VirtualCanvas.Width, 60), 3.6f, Theme.Title);

        var slots = Host.Profile.Slots();
        for (var i = 0; i < slots.Length; i++)
        {
            DrawSlot(renderer, input, i, slots[i]);
        }

        renderer.TextCentered(_message, new Rectangle(0, VirtualCanvas.Height - 90, VirtualCanvas.Width, 30), 1.8f, Theme.TextDim);
        renderer.End();
    }

    private void DrawSlot(TextRenderer renderer, InputState input, int slot, SavedCharacter? character)
    {
        var bounds = SlotBox(slot);
        var hovered = input.Holding(bounds);

        renderer.Fill(bounds, hovered ? new Color(30, 28, 24, 240) : Theme.PanelFill);
        renderer.Frame(bounds, character is null ? Theme.PanelBorder : Theme.Title, 2);

        if (character is null)
        {
            renderer.TextCentered("VACIO", new Rectangle(bounds.X, bounds.Center.Y - 16, bounds.Width, 32), 2.4f, Theme.TextDim);
            renderer.TextCentered("TOCA PARA CREAR", new Rectangle(bounds.X, bounds.Center.Y + 24, bounds.Width, 24), 1.4f, Theme.TextDim);
            return;
        }

        renderer.Text(character.Name, new Vector2(bounds.X + 20, bounds.Y + 20), 2.6f, Theme.Title);
        renderer.Text(
            $"NIVEL {character.Level}",
            new Vector2(bounds.X + 20, bounds.Y + 62),
            1.8f,
            Theme.Text);
        renderer.Text(
            CharacterClasses.Name((CharacterClass)character.Class),
            new Vector2(bounds.X + 20, bounds.Y + 92),
            1.8f,
            Theme.TextDim);

        Theme.Button(renderer, DeleteButton(slot), "BORRAR", input.Holding(DeleteButton(slot)), 1.4f);
    }

    /// <summary>El recuadro de un espacio de personaje.</summary>
    public static Rectangle SlotBox(int slot)
    {
        var height = 200;
        var gap = 24;
        var total = Math.Min(VirtualCanvas.Width - 120, 1100);
        var width = (total - (gap * (ProfileStore.SlotCount - 1))) / ProfileStore.SlotCount;
        var left = (VirtualCanvas.Width - total) / 2;
        return new Rectangle(left + (slot * (width + gap)), (VirtualCanvas.Height / 2) - (height / 2) - 20, width, height);
    }

    /// <summary>El botón de borrar de un espacio ocupado.</summary>
    public static Rectangle DeleteButton(int slot)
    {
        var box = SlotBox(slot);
        return new Rectangle(box.Right - 128, box.Bottom - 52, 108, 36);
    }
}
