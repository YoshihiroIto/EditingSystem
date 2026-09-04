using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Jewelry.EditingSystem.Avalonia.Demo;

namespace Jewelry.EditingSystem.Avalonia.Demo.Browser;

public sealed class BrowserApp : Application
{
    private BrowserUndoRedoBindings? _undoRedoBindings;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            var mainView = new MainView { Focusable = true };
            _undoRedoBindings?.Dispose();
            _undoRedoBindings = new BrowserUndoRedoBindings(mainView);
            singleView.MainView = mainView;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
