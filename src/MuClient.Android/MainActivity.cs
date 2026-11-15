using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Microsoft.Xna.Framework;
using Mu.Client;
using Mu.Core.Assets;

namespace Mu.Client.Android;

[Activity(
    Label = "Mu Android",
    MainLauncher = true,
    ScreenOrientation = ScreenOrientation.Landscape,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AndroidGameActivity
{
    private MuGame _game = null!;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // La partida se guarda en la carpeta privada de la app, dentro del
        // propio móvil. Es lo único que se escribe fuera del APK, y hace falta
        // para no perder el personaje al cerrar el juego.
        var profileFolder = System.IO.Path.Combine(FilesDir!.AbsolutePath, "Profile");
        System.IO.Directory.CreateDirectory(profileFolder);
        System.Environment.SetEnvironmentVariable(DataLocator.ProfileEnvironmentVariable, profileFolder);

        _game = new MuGame(OpenPackagedAssets);
        var view = (View)_game.Services.GetService(typeof(View))!;
        SetContentView(view);
        _game.Run();
    }

    /// <summary>
    /// Abre los datos que van dentro del APK. Si no se empaquetaron, devuelve
    /// null y el juego enseña el aviso en pantalla en vez de quedarse en negro.
    /// </summary>
    private IAssetSource? OpenPackagedAssets()
    {
        var assets = Assets;
        if (assets is null)
        {
            return null;
        }

        try
        {
            var source = new AndroidAssetSource(assets);
            Console.Error.WriteLine($"[Mu] recursos dentro del APK: {source.Count}");
            return source.Count > 0 ? source : null;
        }
        catch (Java.IO.IOException error)
        {
            Console.Error.WriteLine($"[Mu] no se pudieron leer los recursos del APK: {error.Message}");
            return null;
        }
    }
}
