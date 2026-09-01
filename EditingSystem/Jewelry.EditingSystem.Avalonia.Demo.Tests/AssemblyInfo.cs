global using Xunit;
global using Avalonia.Headless.XUnit;
using Avalonia;
using Avalonia.Headless;
using Jewelry.EditingSystem.Avalonia.Demo;

[assembly: AvaloniaTestApplication(typeof(HeadlessTestApp))]
[assembly: CollectionBehavior(DisableTestParallelization = true)]

public sealed class HeadlessTestApp
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<Jewelry.EditingSystem.Avalonia.Demo.Desktop.DesktopApp>()
        .UseSkia()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions
        {
            UseHeadlessDrawing = false
        });
}
