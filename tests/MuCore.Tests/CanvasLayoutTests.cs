using Mu.Core.Rendering;
using Xunit;

namespace Mu.Core.Tests;

/// <summary>
/// Comprueba las cuentas que colocan el lienzo del juego en la pantalla del
/// móvil. De esto depende que el interfaz ocupe toda la pantalla en vez de salir
/// en una esquina, así que se prueba con pantallas reales de móvil.
/// </summary>
public class CanvasLayoutTests
{
    [Theory]
    [InlineData(2400, 1080)] // movil alargado
    [InlineData(1920, 1080)] // 16:9 clasico
    [InlineData(1080, 1920)] // vertical (girado)
    [InlineData(1280, 800)]  // tableta
    [InlineData(720, 1280)]  // movil pequeño vertical
    public void ElLienzoSeAjustaAlaFormaDeLaPantalla(int screenWidth, int screenHeight)
    {
        var size = VirtualCanvasLayout.Choose(screenWidth, screenHeight);

        Assert.Equal(VirtualCanvasLayout.Height, size.Height);
        Assert.InRange(size.Width, VirtualCanvasLayout.MinimumWidth, VirtualCanvasLayout.MaximumWidth);
        Assert.Equal(0, size.Width % 2);
    }

    [Fact]
    public void UnMovilAlargadoDaUnLienzoMasAnchoQueUnoCuadrado()
    {
        var alargado = VirtualCanvasLayout.Choose(2400, 1080);
        var cuadrado = VirtualCanvasLayout.Choose(1080, 1080);

        Assert.True(alargado.Width > cuadrado.Width);
    }

    [Fact]
    public void SinSaberLaPantallaUsaElLienzoDeSiempre()
    {
        var size = VirtualCanvasLayout.Choose(0, 0);

        Assert.Equal(VirtualCanvasLayout.DefaultWidth, size.Width);
        Assert.Equal(VirtualCanvasLayout.Height, size.Height);
    }

    [Fact]
    public void ElLienzoOcupaTodaLaPantallaCuandoLaFormaCoincide()
    {
        var size = VirtualCanvasLayout.Choose(2400, 1080);
        var placement = VirtualCanvasLayout.Place(size.Width, size.Height, 2400, 1080);

        // Al elegir el ancho a partir de la pantalla, no deben quedar barras
        // grandes: el lienzo tiene que llenarla entera.
        Assert.Equal(2400, placement.Width);
        Assert.Equal(1080, placement.Height);
        Assert.Equal(0, placement.X);
        Assert.Equal(0, placement.Y);
    }

    [Fact]
    public void NuncaSeDeformaAunqueNoCuadreLaForma()
    {
        var size = VirtualCanvasLayout.Choose(1280, 720);
        var placement = VirtualCanvasLayout.Place(size.Width, size.Height, 1000, 1000);

        var escalaX = placement.Width / (float)size.Width;
        var escalaY = placement.Height / (float)size.Height;

        Assert.Equal(escalaX, escalaY, precision: 3);
        Assert.True(placement.Width <= 1000 && placement.Height <= 1000);
    }

    [Fact]
    public void UnToqueEnElCentroDeLaPantallaCaeEnElCentroDelLienzo()
    {
        var size = VirtualCanvasLayout.Choose(2400, 1080);
        var point = VirtualCanvasLayout.ToCanvas(1200, 540, size.Width, size.Height, 2400, 1080);

        Assert.Equal(size.Width / 2f, point.X, precision: 1);
        Assert.Equal(size.Height / 2f, point.Y, precision: 1);
    }

    [Fact]
    public void UnToqueEnElBordeInferiorCaeEnElBordeDelLienzo()
    {
        var size = VirtualCanvasLayout.Choose(2400, 1080);
        var point = VirtualCanvasLayout.ToCanvas(1200, 1080, size.Width, size.Height, 2400, 1080);

        Assert.Equal(size.Height, point.Y, precision: 1);
    }
}
