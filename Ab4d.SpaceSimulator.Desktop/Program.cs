using System;
using Ab4d.SpaceSimulator.Shared;
using Avalonia;
using Avalonia.ReactiveUI;

namespace Ab4d.SpaceSimulator.Desktop
{
    internal sealed class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args) => BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
        {
            // This is just temporal trial license and will be replaced by a free open source license:
            // Ab4d.SharpEngine Trial License can be used for testing the Ab4d.SharpEngine and is valid until April 15, 2025.
            Ab4d.SharpEngine.Licensing.SetLicense(licenseOwner: "AB4D",
                                                  licenseType: "TrialLicense",
                                                  platforms: "All",
                                                  license: "4045-4200-4577-BCD0-4186-154D-0519-2487-6A96-1677-6027-87D1-C2DF-D1D5-3064-F1B1-34EA");

            return AppBuilder.Configure<App>()
                             .UsePlatformDetect()
                             .WithInterFont()
                             .LogToTrace()
                             .UseReactiveUI();
        }
    }
}
