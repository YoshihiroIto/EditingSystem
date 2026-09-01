using Avalonia;

namespace Jewelry.EditingSystem.Avalonia.Demo.Desktop;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
        => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<DesktopApp>()
            .UsePlatformDetect()
            .LogToTrace();
}
