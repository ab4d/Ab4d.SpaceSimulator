using System;
using Ab4d.SpaceSimulator.Shared;
using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Avalonia.Android;

namespace Ab4d.SpaceSimulator.Android
{
    [Activity(
        Label = "Ab4d.SpaceSimulator.Android",
        Theme = "@style/MyTheme.NoActionBar",
        Icon = "@drawable/icon",
        MainLauncher = true,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
#if AVALONIA_12
    public class MainActivity : AvaloniaMainActivity
    {
        public MainActivity()
        {
        }
    }
    
    [Application]
    public class AndroidApp : AvaloniaAndroidApplication<App>
    {
        protected AndroidApp(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
            // This free open source license is valid only for the open source project at the following URL:
            // https://github.com/ab4d/Ab4d.SpaceSimulator
            // Assembly name: 'Ab4d.SpaceSimulator'
            Ab4d.SharpEngine.Licensing.SetLicense(licenseOwner: "AB4D", 
                                                  licenseType: "OpenSourceLicense", 
                                                  platforms: "All", 
                                                  license: "6177-EC3F-9ECB-28D9-1FF8-A057-FB36-AEA4-E99B-F2E3-A557-AA35-AFBA-B2A0-DE67-9554-D0F5-F474-01D0-D460-2395-45C7-F91D-4A63-3E01-D1F8-7E1C");

        }
    }
#else
    public class MainActivity : AvaloniaMainActivity<App>
    {
        protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
        {
            // Ab4d.SharpEngine license must be activated from the entry assembly (otherwise an SDK license is needed).
            //
            // Ab4d.SharpEngine Samples License can be used only for Ab4d.SharpEngine samples.
            // To use Ab4d.SharpEngine in your project, get a license from ab4d.com/trial or ab4d.com/purchase 
            Ab4d.SharpEngine.Licensing.SetLicense(licenseOwner: "AB4D",
                                                  licenseType: "SamplesLicense",
                                                  license: "5B53-8A17-DAEB-5B03-3B90-DD5B-958B-CA4D-0B88-CE79-FBB4-6002-D9C9-19C2-AFF8-1662-B2B2");

            return base.CustomizeAppBuilder(builder)
                .WithInterFont()
                .UseReactiveUI(uiBuilder => {});
        }
    }
#endif    
}
