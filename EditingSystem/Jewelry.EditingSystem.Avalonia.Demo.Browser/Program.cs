using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Browser;

[assembly: SupportedOSPlatform("browser")]

namespace Jewelry.EditingSystem.Avalonia.Demo.Browser;

internal static class Program
{
    private static Task Main(string[] args)
        => BuildAvaloniaApp().StartBrowserAppAsync("out");

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<BrowserApp>();
}