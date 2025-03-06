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
            // This free open source license is valid only for the open source project at the following URL:
            // https://github.com/ab4d/Ab4d.SpaceSimulator
            // Assembly name: 'Ab4d.SpaceSimulator'
            Ab4d.SharpEngine.Licensing.SetLicense(licenseOwner: "AB4D", 
                                                  licenseType: "OpenSourceLicense", 
                                                  platforms: "All", 
                                                  license: "6177-EC3F-9ECB-28D9-1FF8-A057-FB36-AEA4-E99B-F2E3-A557-AA35-AFBA-B2A0-DE67-9554-D0F5-F474-01D0-D460-2395-45C7-F91D-4A63-3E01-D1F8-7E1C");

            return AppBuilder.Configure<App>()
                             .UsePlatformDetect()
#if VULKAN_BACKEND
                             .With(new Win32PlatformOptions
                             {
                                 RenderingMode = new[]
                                 {
                                     Win32RenderingMode.Vulkan
                                 }
                             })
                             .With(new X11PlatformOptions
                             {
                                 RenderingMode = new[]
                                 {
                                     X11RenderingMode.Vulkan
                                 }
                             })
                             .With(new Avalonia.Vulkan.VulkanOptions()
                             {
                                 VulkanInstanceCreationOptions = new Avalonia.Vulkan.VulkanInstanceCreationOptions()
                                 {
                                     UseDebug = true
                                 }
                             })
#endif
                             .WithInterFont()
                             .LogToTrace()
                             .UseReactiveUI();
        }
    }
}
