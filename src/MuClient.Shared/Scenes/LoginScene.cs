using Microsoft.Xna.Framework;
using Mu.Client.Rendering;
using Mu.Client.Ui;
using Mu.Core.Game;
using Mu.Core.Rendering;

namespace Mu.Client.Scenes;

/// <summary>
/// La pantalla de entrada. La primera vez hay que crear la cuenta con el botón
/// "CREAR CUENTA"; después se entra con "ENTRAR" y la misma cuenta y contraseña.
/// Se escribe con el teclado que el juego dibuja abajo, porque en el móvil no se
/// puede llamar al teclado de Android. Todo se guarda en el propio dispositivo.
/// </summary>
public sealed class LoginScene : Scene
{
    private readonly TextInput _account = new(ProfileStore.MaximumNameLength);
    private readonly TextInput _password = new(ProfileStore.MaximumNameLength, masked: true);

    private TextInput _active = null!;
    private Field _field = Field.Account;
    private string _message = string.Empty;
    private bool _messageIsError;
    private float _blink;

    /// <summary>Cuál de los dos campos se está escribiendo.</summary>
    private enum Field
    {
        Account,
        Password,
    }

    public override void Load()
    {
        _active = _account;
        _message = Host.Profile.HasAccount
            ? "ESCRIBE TU CUENTA Y CONTRASENA Y TOCA ENTRAR"
            : "PRIMERA VEZ: ESCRIBE CUENTA Y CONTRASENA Y TOCA CREAR CUENTA";
    }

    public override void Update(float seconds, InputState input)
    {
        _blink += seconds;

        // Tocar un recuadro lo deja activo, para saber dónde se escribe.
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

        // El teclado en pantalla escribe en el campo activo.
        var keyboard = KeyboardPanel.Update(input, _active, VirtualCanvas.Width, VirtualCanvas.Height);

        // En escritorio también vale el teclado físico.
        _active.UpdateFromKeyboard(input.Keyboard);

        if (input.Tapped(EnterButton()) || keyboard.Enter)
        {
            Enter();
        }

        if (input.Tapped(CreateButton()))
        {
            Create();
        }
    }

    /// <summary>Entra en una cuenta que ya existe.</summary>
    private void Enter()
    {
        if (_account.IsEmpty || _password.IsEmpty)
        {
            Report("RELLENA LA CUENTA Y LA CONTRASENA", isError: true);
            return;
        }

        if (!Host.Profile.HasAccount)
        {
            Report("TODAVIA NO HAY CUENTA: TOCA CREAR CUENTA", isError: true);
            return;
        }

        switch (Host.Profile.LogIn(_account.Text, _password.Text))
        {
            case LoginResult.Success:
                GoToSelection();
                break;
            case LoginResult.InvalidAccount:
                Report("ESA CUENTA NO COINCIDE CON LA DE ESTE MOVIL", isError: true);
                break;
            case LoginResult.InvalidPassword:
                Report("CONTRASENA INCORRECTA", isError: true);
                break;
            default:
                Report("NO SE PUDO ENTRAR", isError: true);
                break;
        }
    }

    /// <summary>Crea la cuenta con lo que se ha escrito.</summary>
    private void Create()
    {
        if (Host.Profile.HasAccount)
        {
            Report("YA HAY UNA CUENTA EN ESTE MOVIL: TOCA ENTRAR", isError: true);
            return;
        }

        if (_account.IsEmpty || _password.IsEmpty)
        {
            Report("RELLENA LA CUENTA Y LA CONTRASENA", isError: true);
            return;
        }

        if (_password.Text.Length < ProfileStore.MinimumPasswordLength)
        {
            Report($"LA CONTRASENA NECESITA {ProfileStore.MinimumPasswordLength} LETRAS O MAS", isError: true);
            return;
        }

        switch (Host.Profile.LogIn(_account.Text, _password.Text))
        {
            case LoginResult.Created:
                Report("CUENTA CREADA. ELIGE PERSONAJE.", isError: false);
                GoToSelection();
                break;
            case LoginResult.InvalidAccount:
                Report("LA CUENTA SOLO ADMITE LETRAS Y NUMEROS, HASTA 12", isError: true);
                break;
            case LoginResult.PasswordTooShort:
                Report($"LA CONTRASENA NECESITA {ProfileStore.MinimumPasswordLength} LETRAS O MAS", isError: true);
                break;
            default:
                Report("NO SE PUDO CREAR LA CUENTA", isError: true);
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
        var width = VirtualCanvas.Width;
        var height = VirtualCanvas.Height;
        renderer.Begin();

        renderer.Fill(new Rectangle(0, 0, width, height), new Color(8, 10, 16));

        renderer.TextCentered("MU ONLINE   SIN CONEXION", TitleArea(), 3.0f, Theme.Title);

        var panel = Panel();
        Theme.Panel(renderer, panel);

        DrawField(renderer, AccountField(), "CUENTA", _account, _field == Field.Account);
        DrawField(renderer, PasswordField(), "CONTRASENA", _password, _field == Field.Password);

        Theme.Button(renderer, EnterButton(), "ENTRAR", input.Holding(EnterButton()), 2.4f);
        Theme.Button(renderer, CreateButton(), "CREAR CUENTA", input.Holding(CreateButton()), 2.4f);

        renderer.TextCentered(_message, MessageArea(), 1.6f, _messageIsError ? new Color(255, 130, 120) : Theme.TextDim);

        KeyboardPanel.Draw(renderer, input, width, height);

        renderer.End();
    }

    private void DrawField(TextRenderer renderer, Rectangle bounds, string label, TextInput field, bool active)
    {
        renderer.Text(label, new Vector2(bounds.X, bounds.Y - 28), 1.8f, Theme.TextDim);
        renderer.Fill(bounds, new Color(20, 20, 26, 240));
        renderer.Frame(bounds, active ? Theme.Title : Theme.PanelBorder, 2);

        // El cursor parpadea solo en el campo activo, para saber dónde se escribe.
        var caret = active && _blink % 1f < 0.5f ? "_" : string.Empty;
        renderer.Text(field.DisplayText + caret, new Vector2(bounds.X + 14, bounds.Y + 12), 2.2f, Theme.Text);
    }

    /// <summary>El panel central, siempre por encima del teclado.</summary>
    public static Rectangle Panel()
    {
        var form = OnScreenKeyboard.FormArea(VirtualCanvas.Width, VirtualCanvas.Height);
        var width = Math.Min(760, form.Width - 120);
        // El título y el mensaje van en franjas propias; el panel ocupa el resto.
        var top = form.Y + 38;
        var height = Math.Max(200, form.Bottom - top - 28);
        return new Rectangle((VirtualCanvas.Width - width) / 2, top, width, height);
    }

    /// <summary>La franja del mensaje, justo debajo del panel.</summary>
    public static Rectangle MessageArea()
    {
        var panel = Panel();
        var form = OnScreenKeyboard.FormArea(VirtualCanvas.Width, VirtualCanvas.Height);
        return new Rectangle(panel.X, panel.Bottom + 2, panel.Width, Math.Max(20, form.Bottom - panel.Bottom - 2));
    }

    /// <summary>La franja donde va el título, encima del panel.</summary>
    public static Rectangle TitleArea()
    {
        var form = OnScreenKeyboard.FormArea(VirtualCanvas.Width, VirtualCanvas.Height);
        return new Rectangle(0, form.Y, VirtualCanvas.Width, 36);
    }

    /// <summary>El recuadro donde se escribe la cuenta.</summary>
    public static Rectangle AccountField()
    {
        var panel = Panel();
        return new Rectangle(panel.X + 36, panel.Y + 34, panel.Width - 72, 50);
    }

    /// <summary>El recuadro donde se escribe la contraseña.</summary>
    public static Rectangle PasswordField()
    {
        var account = AccountField();
        return new Rectangle(account.X, account.Bottom + 44, account.Width, account.Height);
    }

    /// <summary>El botón de entrar.</summary>
    public static Rectangle EnterButton()
    {
        var panel = Panel();
        var width = (panel.Width - 84) / 2;
        return new Rectangle(panel.X + 36, PasswordField().Bottom + 20, width, 54);
    }

    /// <summary>El botón de crear la cuenta.</summary>
    public static Rectangle CreateButton()
    {
        var enter = EnterButton();
        return new Rectangle(enter.Right + 12, enter.Y, enter.Width, enter.Height);
    }
}
