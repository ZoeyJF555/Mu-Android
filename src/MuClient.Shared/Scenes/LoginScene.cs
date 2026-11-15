using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Mu.Client.Rendering;
using Mu.Client.Ui;
using Mu.Core.Game;

namespace Mu.Client.Scenes;

/// <summary>
/// La pantalla de entrada: cuenta y contraseña, con el botón de entrar. La
/// primera vez que se abre el juego, escribe una cuenta y una contraseña y se
/// crea sola; a partir de ahí, hay que acordarse de ellas. Todo se guarda en el
/// propio dispositivo, sin servidor.
/// </summary>
public sealed class LoginScene : Scene
{
    private readonly TextInput _account = new(ProfileStore.MaximumNameLength);
    private readonly TextInput _password = new(ProfileStore.MaximumNameLength, masked: true);

    private TextInput _active = null!;
    private string _message = string.Empty;
    private bool _messageIsError;
    private float _blink;

    /// <summary>Cuál de los dos campos está activo (para saber dónde escribe el teclado).</summary>
    private enum Field
    {
        Account,
        Password,
    }

    private Field _field = Field.Account;

    public override void Load()
    {
        _active = _account;
        if (Host.Profile.HasAccount)
        {
            _message = "ESCRIBE TU CUENTA Y TU CONTRASENA";
        }
        else
        {
            _message = "PRIMERA VEZ: ESCRIBE UNA CUENTA Y UNA CONTRASENA (MINIMO 4)";
        }
    }

    public override void Update(float seconds, InputState input)
    {
        _blink += seconds;

        // El campo activo se puede cambiar tocando su recuadro.
        if (input.Tapped(AccountField()) && _field != Field.Account)
        {
            _field = Field.Account;
            _active = _account;
        }

        if (input.Tapped(PasswordField()) && _field != Field.Password)
        {
            _field = Field.Password;
            _active = _password;
        }

        var keyboard = input.Keyboard;
        if (input.IsNewKey(Keys.Tab))
        {
            _field = _field == Field.Account ? Field.Password : Field.Account;
            _active = _field == Field.Account ? _account : _password;
        }

        _active.UpdateFromKeyboard(keyboard);

        if (input.Tapped(EnterButton()) || input.IsNewKey(Keys.Enter))
        {
            Submit();
        }
    }

    private void Submit()
    {
        if (_account.IsEmpty || _password.IsEmpty)
        {
            Report("RELLENA LA CUENTA Y LA CONTRASENA", isError: true);
            return;
        }

        var result = Host.Profile.LogIn(_account.Text, _password.Text);
        switch (result)
        {
            case LoginResult.Created:
                Report("CUENTA CREADA. ELIGE PERSONAJE.", isError: false);
                GoToSelection();
                break;
            case LoginResult.Success:
                GoToSelection();
                break;
            case LoginResult.InvalidAccount:
                Report("ESA CUENTA NO EXISTE EN ESTE MOVIL", isError: true);
                break;
            case LoginResult.InvalidPassword:
                Report("CONTRASENA INCORRECTA", isError: true);
                break;
            case LoginResult.PasswordTooShort:
                Report("LA CONTRASENA ES DEMASIADO CORTA", isError: true);
                break;
        }
    }

    private void GoToSelection() => Host.Change(new CharacterSelectScene());

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

        var title = new Rectangle(0, 70, VirtualCanvas.Width, 60);
        renderer.TextCentered("MU ONLINE   SIN CONEXION", title, 4f, Theme.Title);

        var panel = Panel();
        Theme.Panel(renderer, panel);
        renderer.Text("ENTRAR", new Vector2(panel.X + 24, panel.Y + 18), 2.6f, Theme.Title);

        DrawField(renderer, AccountField(), "CUENTA", _account, _field == Field.Account);
        DrawField(renderer, PasswordField(), "CONTRASENA", _password, _field == Field.Password);

        Theme.Button(renderer, EnterButton(), "ENTRAR", input.Holding(EnterButton()), 2.4f);

        renderer.TextCentered(
            _message,
            new Rectangle(panel.X, EnterButton().Bottom + 18, panel.Width, 30),
            1.6f,
            _messageIsError ? new Color(255, 130, 120) : Theme.TextDim);

        renderer.End();
    }

    private void DrawField(TextRenderer renderer, Rectangle bounds, string label, TextInput field, bool active)
    {
        renderer.Text(label, new Vector2(bounds.X, bounds.Y - 26), 1.6f, Theme.TextDim);
        renderer.Fill(bounds, new Color(20, 20, 26, 240));
        renderer.Frame(bounds, active ? Theme.Title : Theme.PanelBorder, 2);

        // El cursor parpadea solo en el campo activo, para saber dónde se escribe.
        var caret = active && _blink % 1f < 0.5f ? "_" : string.Empty;
        renderer.Text(field.DisplayText + caret, new Vector2(bounds.X + 12, bounds.Y + 10), 2f, Theme.Text);
    }

    /// <summary>El recuadro donde se escribe la cuenta.</summary>
    public static Rectangle AccountField()
    {
        var panel = Panel();
        return new Rectangle(panel.X + 24, panel.Y + 92, panel.Width - 48, 44);
    }

    /// <summary>El recuadro donde se escribe la contraseña.</summary>
    public static Rectangle PasswordField()
    {
        var account = AccountField();
        return new Rectangle(account.X, account.Y + 92, account.Width, account.Height);
    }

    /// <summary>El botón de entrar.</summary>
    public static Rectangle EnterButton()
    {
        var panel = Panel();
        return new Rectangle(panel.X + 24, PasswordField().Bottom + 34, panel.Width - 48, 56);
    }

    /// <summary>El panel central de la pantalla de entrada.</summary>
    public static Rectangle Panel() =>
        new((VirtualCanvas.Width / 2) - 260, (VirtualCanvas.Height / 2) - 170, 520, 400);
}
