using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Jewelry.EditingSystem.Avalonia.Demo;

namespace Jewelry.EditingSystem.Avalonia.Demo.Browser;

public sealed class BrowserApp : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            var mainView = new MainView { Focusable = true };
            mainView.AttachedToVisualTree += (_, _) =>
                Dispatcher.UIThread.Post(() => mainView.Focus(), DispatcherPriority.Loaded);
            singleView.MainView = mainView;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
