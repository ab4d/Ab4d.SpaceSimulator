using System;
using _Microsoft.Android.Resource.Designer;
using Ab4d.SharpEngine;
using Ab4d.SpaceSimulator.Shared;
using Android.App;
using Android.Content.PM;
using Android.Media;
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
            // This free open source license is valid only for the open source project at the following URL:
            // https://github.com/ab4d/Ab4d.SpaceSimulator
            // Assembly name: 'Ab4d.SpaceSimulator'
            Ab4d.SharpEngine.Licensing.SetLicense(licenseOwner: "AB4D", 
                                                  licenseType: "OpenSourceLicense", 
                                                  platforms: "All", 
                                                  license: "6177-EC3F-9ECB-28D9-1FF8-A057-FB36-AEA4-E99B-F2E3-A557-AA35-AFBA-B2A0-DE67-9554-D0F5-F474-01D0-D460-2395-45C7-F91D-4A63-3E01-D1F8-7E1C");

            return base.CustomizeAppBuilder(builder)
                .WithInterFont()
                .UseReactiveUI();
        }
    }
}
