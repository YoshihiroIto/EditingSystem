using Avalonia.Controls;
using Avalonia.Threading;
using Jewelry.EditingSystem.Avalonia.Demo;

namespace Jewelry.EditingSystem.Avalonia.Demo.Tests;

public sealed class BrowserUndoRedoBindingsTests
{
    [AvaloniaFact]
    public void Browser_shortcuts_are_not_duplicated_and_are_removed_when_view_detaches()
    {
        var view = new MainView { Focusable = true };
        var bindings = new BrowserUndoRedoBindings(view);
        var window = new Window { Content = view };

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(2, window.KeyBindings.Count);

            window.Content = null;
            Dispatcher.UIThread.RunJobs();
            Assert.Empty(window.KeyBindings);

            window.Content = view;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(2, window.KeyBindings.Count);

            bindings.Dispose();
            window.Content = null;
            Dispatcher.UIThread.RunJobs();
            window.Content = view;
            Dispatcher.UIThread.RunJobs();
            Assert.Empty(window.KeyBindings);
        }
        finally
        {
            bindings.Dispose();
            ((IDisposable)view.DataContext!).Dispose();
            window.Close();
        }
    }
}
