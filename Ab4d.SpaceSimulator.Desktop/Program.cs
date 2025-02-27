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
            SharpEngineLicenseHelper.ActivateLicense();

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
