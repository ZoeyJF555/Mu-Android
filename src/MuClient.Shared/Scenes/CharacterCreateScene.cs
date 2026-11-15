using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Mu.Client.Rendering;
using Mu.Client.Ui;
using Mu.Core.Game;
using Mu.Core.Rendering;

namespace Mu.Client.Scenes;

/// <summary>
/// La pantalla para crear un personaje: se escribe el nombre con el teclado del
/// juego, se elige la clase y se confirma. Cada clase tiene sus propios números
/// de vida, maná y fuerza, que se ven aquí para poder elegir con conocimiento.
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

        var keyboard = KeyboardPanel.Update(input, _name, VirtualCanvas.Width, VirtualCanvas.Height);
        _name.UpdateFromKeyboard(input.Keyboard);

        for (var i = 0; i < CharacterClasses.All.Length; i++)
        {
            if (input.Tapped(ClassButton(i)))
            {
                _class = CharacterClasses.All[i];
            }
        }

        if (input.Tapped(ConfirmButton()) || keyboard.Enter || input.IsNewKey(Keys.Enter))
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
        var width = VirtualCanvas.Width;
        var height = VirtualCanvas.Height;
        renderer.Begin();

        renderer.Fill(new Rectangle(0, 0, width, height), new Color(8, 10, 16));
        renderer.TextCentered("CREAR PERSONAJE", new Rectangle(0, 20, width, 50), 3.4f, Theme.Title);

        renderer.Text("NOMBRE", new Vector2(NameField().X, NameField().Y - 26), 1.8f, Theme.TextDim);
        renderer.Fill(NameField(), new Color(20, 20, 26, 240));
        renderer.Frame(NameField(), Theme.Title, 2);
        var caret = _blink % 1f < 0.5f ? "_" : string.Empty;
        renderer.Text(_name.DisplayText + caret, new Vector2(NameField().X + 14, NameField().Y + 12), 2.2f, Theme.Text);

        renderer.Text("CLASE", new Vector2(ClassButton(0).X, ClassButton(0).Y - 28), 1.8f, Theme.TextDim);
        for (var i = 0; i < CharacterClasses.All.Length; i++)
        {
            var value = CharacterClasses.All[i];
            var bounds = ClassButton(i);
            var selected = value == _class;
            renderer.Fill(bounds, selected ? Theme.ButtonPressed : Theme.ButtonFill);
            renderer.Frame(bounds, selected ? Theme.Title : Theme.PanelBorder, 2);

            var stats = CharacterClasses.Base(value);
            renderer.TextCentered(CharacterClasses.Name(value), new Rectangle(bounds.X, bounds.Y + 10, bounds.Width, 26), 2.2f, selected ? Color.White : Theme.Text);
            renderer.TextCentered($"HP {stats.MaxHealth}   MP {stats.MaxMana}", new Rectangle(bounds.X, bounds.Y + 44, bounds.Width, 20), 1.4f, Theme.TextDim);
            renderer.TextCentered($"ATQ {stats.Attack}", new Rectangle(bounds.X, bounds.Y + 66, bounds.Width, 20), 1.4f, Theme.TextDim);
        }

        Theme.Button(renderer, ConfirmButton(), "CREAR", input.Holding(ConfirmButton()), 2.2f);
        Theme.Button(renderer, CancelButton(), "VOLVER", input.Holding(CancelButton()), 2.2f);

        renderer.TextCentered(
            _message,
            new Rectangle(0, ConfirmButton().Bottom + 8, width, 28),
            1.6f,
            _messageIsError ? new Color(255, 130, 120) : Theme.TextDim);

        KeyboardPanel.Draw(renderer, input, width, height);

        renderer.End();
    }

    /// <summary>El recuadro del nombre.</summary>
    public static Rectangle NameField()
    {
        var width = Math.Min(720, VirtualCanvas.Width - 120);
        return new Rectangle((VirtualCanvas.Width - width) / 2, 104, width, 56);
    }

    /// <summary>El botón de una clase.</summary>
    public static Rectangle ClassButton(int index)
    {
        var field = NameField();
        const int gap = 16;
        const int height = 100;
        var total = field.Width;
        var width = (total - (gap * (CharacterClasses.All.Length - 1))) / CharacterClasses.All.Length;
        return new Rectangle(field.X + (index * (width + gap)), field.Bottom + 48, width, height);
    }

    /// <summary>El botón de confirmar.</summary>
    public static Rectangle ConfirmButton()
    {
        var field = NameField();
        var width = (field.Width - 16) / 2;
        return new Rectangle(field.X, ClassButton(0).Bottom + 16, width, 56);
    }

    /// <summary>El botón de volver.</summary>
    public static Rectangle CancelButton()
    {
        var confirm = ConfirmButton();
        return new Rectangle(confirm.Right + 16, confirm.Y, confirm.Width, confirm.Height);
    }
}
