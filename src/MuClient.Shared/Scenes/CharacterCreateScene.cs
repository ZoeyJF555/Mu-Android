using Microsoft.Xna.Framework;
using Mu.Client.Rendering;
using Mu.Client.Ui;
using Mu.Core.Game;

namespace Mu.Client.Scenes;

/// <summary>
/// La pantalla para crear un personaje: se escribe el nombre, se elige la clase
/// y se confirma. Cada clase tiene sus propios números de vida, maná y fuerza,
/// que se ven aquí para poder elegir con conocimiento.
/// </summary>
public sealed class CharacterCreateScene : Scene
{
    private readonly int _slot;
    private readonly TextInput _name = new(ProfileStore.MaximumNameLength);

    private CharacterClass _class = CharacterClass.DarkWizard;
    private string _message = string.Empty;
    private bool _messageIsError;
    private float _blink;

    /// <param name="slot">En qué espacio se va a crear.</param>
    public CharacterCreateScene(int slot)
    {
        _slot = slot;
    }

    public override void Load() => _message = "ESCRIBE UN NOMBRE Y ELIGE CLASE";

    public override void Update(float seconds, InputState input)
    {
        _blink += seconds;
        _name.UpdateFromKeyboard(input.Keyboard);

        for (var i = 0; i < CharacterClasses.All.Length; i++)
        {
            if (input.Tapped(ClassButton(i)))
            {
                _class = CharacterClasses.All[i];
            }
        }

        if (input.Tapped(ConfirmButton()) || input.IsNewKey(Microsoft.Xna.Framework.Input.Keys.Enter))
        {
            Confirm();
        }

        if (input.Tapped(CancelButton()))
        {
            Host.Change(new CharacterSelectScene());
        }
    }

    private void Confirm()
    {
        var name = _name.Text.Trim();
        if (!ProfileStore.IsValidName(name))
        {
            Report("EL NOMBRE DEBE TENER LETRAS O NUMEROS, HASTA 12", isError: true);
            return;
        }

        if (!Host.Profile.IsNameFree(name))
        {
            Report("YA TIENES UN PERSONAJE CON ESE NOMBRE", isError: true);
            return;
        }

        if (!Host.Profile.CreateCharacter(_slot, name, _class))
        {
            Report("NO SE PUDO CREAR EL PERSONAJE", isError: true);
            return;
        }

        Host.Change(new CharacterSelectScene());
    }

    private void Report(string message, bool isError)
    {
        _message = message;
        _messageIsError = isError;
    }

    public override void Draw(InputState input)
    {
        var renderer = Host.Text;
        renderer.Begin();

        renderer.Fill(new Rectangle(0, 0, VirtualCanvas.Width, VirtualCanvas.Height), new Color(8, 10, 16));
        renderer.TextCentered("CREAR PERSONAJE", new Rectangle(0, 50, VirtualCanvas.Width, 50), 3.4f, Theme.Title);

        var panel = Panel();
        Theme.Panel(renderer, panel);

        renderer.Text("NOMBRE", new Vector2(NameField().X, NameField().Y - 28), 1.6f, Theme.TextDim);
        renderer.Fill(NameField(), new Color(20, 20, 26, 240));
        renderer.Frame(NameField(), Theme.Title, 2);
        var caret = _blink % 1f < 0.5f ? "_" : string.Empty;
        renderer.Text(_name.DisplayText + caret, new Vector2(NameField().X + 12, NameField().Y + 10), 2f, Theme.Text);

        renderer.Text("CLASE", new Vector2(panel.X + 24, NameField().Bottom + 22), 1.6f, Theme.TextDim);
        for (var i = 0; i < CharacterClasses.All.Length; i++)
        {
            var value = CharacterClasses.All[i];
            var bounds = ClassButton(i);
            var selected = value == _class;
            renderer.Fill(bounds, selected ? Theme.ButtonPressed : Theme.ButtonFill);
            renderer.Frame(bounds, selected ? Theme.Title : Theme.PanelBorder, 2);

            var stats = CharacterClasses.Base(value);
            renderer.TextCentered(CharacterClasses.Name(value), new Rectangle(bounds.X, bounds.Y + 8, bounds.Width, 24), 2f, selected ? Color.White : Theme.Text);
            renderer.TextCentered($"HP {stats.MaxHealth}", new Rectangle(bounds.X, bounds.Y + 40, bounds.Width, 20), 1.3f, Theme.TextDim);
            renderer.TextCentered($"MP {stats.MaxMana}", new Rectangle(bounds.X, bounds.Y + 60, bounds.Width, 20), 1.3f, Theme.TextDim);
            renderer.TextCentered($"ATQ {stats.Attack}", new Rectangle(bounds.X, bounds.Y + 80, bounds.Width, 20), 1.3f, Theme.TextDim);
        }

        Theme.Button(renderer, ConfirmButton(), "CREAR", input.Holding(ConfirmButton()), 2.2f);
        Theme.Button(renderer, CancelButton(), "VOLVER", input.Holding(CancelButton()), 2.2f);

        renderer.TextCentered(
            _message,
            new Rectangle(panel.X, ConfirmButton().Bottom + 16, panel.Width, 30),
            1.5f,
            _messageIsError ? new Color(255, 130, 120) : Theme.TextDim);

        renderer.End();
    }

    /// <summary>El panel central.</summary>
    public static Rectangle Panel() => new((VirtualCanvas.Width / 2) - 300, 130, 600, 470);

    /// <summary>El recuadro del nombre.</summary>
    public static Rectangle NameField()
    {
        var panel = Panel();
        return new Rectangle(panel.X + 24, panel.Y + 64, panel.Width - 48, 44);
    }

    /// <summary>El botón de una clase.</summary>
    public static Rectangle ClassButton(int index)
    {
        var field = NameField();
        var panel = Panel();
        const int width = 168;
        const int height = 106;
        var left = panel.X + 24;
        return new Rectangle(left + (index * (width + 12)), field.Bottom + 52, width, height);
    }

    /// <summary>El botón de confirmar.</summary>
    public static Rectangle ConfirmButton()
    {
        var panel = Panel();
        return new Rectangle(panel.X + 24, panel.Bottom - 88, 240, 52);
    }

    /// <summary>El botón de volver.</summary>
    public static Rectangle CancelButton()
    {
        var panel = Panel();
        return new Rectangle(panel.Right - 264, panel.Bottom - 88, 240, 52);
    }
}
