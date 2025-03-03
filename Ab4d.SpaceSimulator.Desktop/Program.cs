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
            // Ab4d.SharpEngine license must be activated from the entry assembly (otherwise an SDK license is needed).
            //
            // Ab4d.SharpEngine Samples License can be used only for Ab4d.SharpEngine samples.
            // To use Ab4d.SharpEngine in your project, get a license from ab4d.com/trial or ab4d.com/purchase 
            Ab4d.SharpEngine.Licensing.SetLicense(licenseOwner: "AB4D",
                                                  licenseType: "SamplesLicense",
                                                  platforms: "All",
                                                  license: "5B53-8A17-DAEB-5B03-3B90-DD5B-958B-CA4D-0B88-CE79-FBB4-6002-D9C9-19C2-AFF8-1662-B2B2");

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
