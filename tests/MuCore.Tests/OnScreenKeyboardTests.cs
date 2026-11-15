using Mu.Core.Rendering;
using Xunit;

namespace Mu.Core.Tests;

/// <summary>
/// Comprueba la colocación del teclado en pantalla. De esto depende que las
/// teclas caigan dentro del lienzo y que al tocarlas se escriba donde toca.
/// </summary>
public class OnScreenKeyboardTests
{
    [Fact]
    public void TodasLasTeclasCaenDentroDelLienzo()
    {
        var keys = OnScreenKeyboard.Build(1280, 720);

        foreach (var key in keys)
        {
            Assert.InRange(key.Bounds.X, 0, 1280);
            Assert.InRange(key.Bounds.Y, 0, 720);
            Assert.InRange(key.Bounds.Right, 0, 1280);
            Assert.InRange(key.Bounds.Bottom, 0, 720);
            Assert.True(key.Bounds.Width > 0 && key.Bounds.Height > 0);
        }
    }

    [Fact]
    public void TraeTodasLasLetrasYNumeros()
    {
        var keys = OnScreenKeyboard.Build(1280, 720);
        var letras = keys.Where(k => k.Action == KeyboardAction.Character).Select(k => k.Value).ToHashSet();

        foreach (var letter in "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789")
        {
            Assert.Contains(letter, letras);
        }
    }

    [Fact]
    public void TraeBorrarYListo()
    {
        var keys = OnScreenKeyboard.Build(1280, 720);

        Assert.Contains(keys, k => k.Action == KeyboardAction.Backspace);
        Assert.Contains(keys, k => k.Action == KeyboardAction.Enter);
    }

    [Fact]
    public void NingunaTeclaSeSolapaConOtra()
    {
        var keys = OnScreenKeyboard.Build(1280, 720);

        for (var i = 0; i < keys.Count; i++)
        {
            for (var j = i + 1; j < keys.Count; j++)
            {
                var a = keys[i].Bounds;
                var b = keys[j].Bounds;
                var separadas = a.Right <= b.X || b.Right <= a.X || a.Bottom <= b.Y || b.Bottom <= a.Y;
                Assert.True(separadas, $"las teclas '{keys[i].Label}' y '{keys[j].Label}' se solapan");
            }
        }
    }

    [Fact]
    public void AlTocarElCentroDeUnaTeclaSeDevuelveEsaTecla()
    {
        var keys = OnScreenKeyboard.Build(1280, 720);
        var target = keys.First(k => k.Value == 'A');

        var hit = OnScreenKeyboard.Hit(keys, target.Bounds.CenterX, target.Bounds.CenterY);

        Assert.NotNull(hit);
        Assert.Equal('A', hit!.Value.Value);
    }

    [Fact]
    public void UnPuntoFueraDelTecladoNoDaTecla()
    {
        var keys = OnScreenKeyboard.Build(1280, 720);

        // Bien arriba, donde va el panel de login.
        Assert.Null(OnScreenKeyboard.Hit(keys, 640, 60));
    }

    [Fact]
    public void ElTecladoOcupaElAnchoYEstaPegadoAbajo()
    {
        var area = OnScreenKeyboard.Area(1280, 720);

        Assert.Equal(0 + OnScreenKeyboard.Margin, area.X);
        Assert.Equal(1280 - OnScreenKeyboard.Margin, area.Right);
        Assert.Equal(720 - OnScreenKeyboard.Margin, area.Bottom);
        Assert.Equal(OnScreenKeyboard.AreaHeight, area.Height);
    }

    [Fact]
    public void ElTecladoSeAdaptaAlLienzoAnchoDeUnMovil()
    {
        // En un lienzo mas ancho las teclas deben seguir cabiendo enteras.
        var keys = OnScreenKeyboard.Build(1600, 720);

        foreach (var key in keys)
        {
            Assert.True(key.Bounds.Right <= 1600);
            Assert.True(key.Bounds.Bottom <= 720);
        }
    }

    [Fact]
    public void ElFormularioNuncaSeSolapaConElTeclado()
    {
        foreach (var (width, height) in new[] { (1024, 720), (1280, 720), (1560, 720), (1920, 720) })
        {
            var keyboard = OnScreenKeyboard.Area(width, height);
            var form = OnScreenKeyboard.FormArea(width, height);

            Assert.True(form.Bottom <= keyboard.Y, $"el formulario ({form.Bottom}) pisa el teclado ({keyboard.Y}) en {width}x{height}");
            Assert.True(form.Height > 200, $"el formulario queda muy bajo en {width}x{height}");
        }
    }
}
