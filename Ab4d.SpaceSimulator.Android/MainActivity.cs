using Ab4d.SpaceSimulator.Shared;
using Android.App;
using Android.Content.PM;
using Avalonia;
using Avalonia.Android;
using Avalonia.ReactiveUI;

namespace Ab4d.SpaceSimulator.Android
{
    [Activity(
        Label = "Ab4d.SpaceSimulator.Android",
        Theme = "@style/MyTheme.NoActionBar",
        Icon = "@drawable/icon",
        MainLauncher = true,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
    public class MainActivity : AvaloniaMainActivity<App>
    {
        protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
        {
            // Ab4d.SharpEngine license must be activated from the entry assembly (otherwise an SDK license is needed).
            SharpEngineLicenseHelper.ActivateLicense();

            return base.CustomizeAppBuilder(builder)
                .WithInterFont()
                .UseReactiveUI();
        }
    }
}
