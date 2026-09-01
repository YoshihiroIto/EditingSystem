using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Jewelry.EditingSystem.Avalonia.Demo;

namespace Jewelry.EditingSystem.Avalonia.Demo.Browser;

public sealed class BrowserApp : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
            singleView.MainView = new MainView();

        base.OnFrameworkInitializationCompleted();
    }
}
