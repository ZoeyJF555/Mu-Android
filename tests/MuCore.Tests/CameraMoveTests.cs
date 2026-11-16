using System.Numerics;
using Mu.Core.Game;
using Mu.Core.Rendering;
using Xunit;

namespace Mu.Core.Tests;

/// <summary>Comprueba que "adelante" en el mando apunta al fondo de la pantalla.</summary>
public class CameraMoveTests
{
    private const int Width = 800;
    private const int Height = 480;

    [Fact]
    public void ElFondoDeLaPantallaEsHaciaDondeApuntaElMando()
    {
        var camera = new Camera();
        var player = new Vector3(128f, 0f, 128f);
        camera.Target = player;
        camera.Update((float)Width / Height);

        // Un punto a 10 casillas "adelante" y otro "atrás", según la dirección
        // que produce el mando. En pantalla, el de adelante debe salir más arriba
        // (más al fondo), y el de atrás más abajo.
        var ahead = player + new Vector3(camera.Forward.X, 0f, camera.Forward.Y) * 10f;
        var behind = player - new Vector3(camera.Forward.X, 0f, camera.Forward.Y) * 10f;

        var aheadScreen = camera.Project(ahead, Width, Height);
        var behindScreen = camera.Project(behind, Width, Height);

        Assert.NotNull(aheadScreen);
        Assert.NotNull(behindScreen);
        Assert.True(aheadScreen!.Value.Y < behindScreen!.Value.Y,
            $"lo de adelante ({aheadScreen.Value.Y}) debería estar por encima de lo de atrás ({behindScreen.Value.Y})");
    }

    [Fact]
    public void LaDerechaDelMandoEsLaDerechaDeLaPantalla()
    {
        var camera = new Camera();
        var player = new Vector3(128f, 0f, 128f);
        camera.Target = player;
        camera.Update((float)Width / Height);

        var right = player + new Vector3(camera.Right.X, 0f, camera.Right.Y) * 10f;
        var left = player - new Vector3(camera.Right.X, 0f, camera.Right.Y) * 10f;

        var rightScreen = camera.Project(right, Width, Height);
        var leftScreen = camera.Project(left, Width, Height);

        Assert.NotNull(rightScreen);
        Assert.NotNull(leftScreen);
        Assert.True(rightScreen!.Value.X > leftScreen!.Value.X,
            $"lo de la derecha ({rightScreen.Value.X}) debería salir a la derecha de lo de la izquierda ({leftScreen.Value.X})");
    }
}


