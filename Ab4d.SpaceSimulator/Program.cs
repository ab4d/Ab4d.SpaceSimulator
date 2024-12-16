using System;
using Avalonia;

namespace Ab4d.SpaceSimulator
{
    internal class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args) => BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
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
                .LogToTrace();
    }
}
