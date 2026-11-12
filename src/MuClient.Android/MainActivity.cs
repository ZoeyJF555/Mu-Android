using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Microsoft.Xna.Framework;
using Mu.Client;

namespace Mu.Client.Android;

[Activity(
    Label = "Mu Android",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AndroidGameActivity
{
    private MuGame _game = null!;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        _game = new MuGame();
        var view = (View)_game.Services.GetService(typeof(View))!;
        SetContentView(view);
        _game.Run();
    }
}
