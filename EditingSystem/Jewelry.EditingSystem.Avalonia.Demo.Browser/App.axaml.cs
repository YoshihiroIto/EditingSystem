using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

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
                Dispatcher.UIThread.Post(() => InitializeBrowserView(mainView), DispatcherPriority.Loaded);
            singleView.MainView = mainView;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void InitializeBrowserView(MainView mainView)
    {
        mainView.Focus();

        if (TopLevel.GetTopLevel(mainView) is not { } topLevel ||
            mainView.DataContext is not MainViewViewModel viewModel)
            return;

        topLevel.KeyBindings.Add(new KeyBinding
        {
            Gesture = new KeyGesture(Key.Z, KeyModifiers.Control),
            Command = viewModel.UndoCommand
        });
        topLevel.KeyBindings.Add(new KeyBinding
        {
            Gesture = new KeyGesture(Key.Y, KeyModifiers.Control),
            Command = viewModel.RedoCommand
        });
    }
}
