using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Jewelry.EditingSystem.Avalonia.Demo.Tests;

public sealed class SelectionInteractionTests
{
    [AvaloniaFact]
    public void Click_selection_does_not_create_undo_history_or_change_z_order()
    {
        var window = ShowWindow();
        try
        {
            var viewModel = (MainWindowViewModel)window.DataContext!;
            var target = viewModel.Objects[1];
            var originalOrder = viewModel.Objects.ToArray();

            Click(window, FindObjectBody(window, target), RawInputModifiers.None);
            Dispatcher.UIThread.RunJobs();

            Assert.True(target.IsSelected);
            Assert.Equal(1, viewModel.SelectionCount);
            Assert.Equal(0, viewModel.History.UndoCount);
            Assert.Equal(originalOrder, viewModel.Objects);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Control_click_selection_does_not_create_undo_history_or_change_z_order()
    {
        var window = ShowWindow();
        try
        {
            var viewModel = (MainWindowViewModel)window.DataContext!;
            var initiallySelected = viewModel.Objects[0];
            var target = viewModel.Objects[1];
            var originalOrder = viewModel.Objects.ToArray();

            Click(window, FindObjectBody(window, target), RawInputModifiers.Control);
            Dispatcher.UIThread.RunJobs();

            Assert.True(initiallySelected.IsSelected);
            Assert.True(target.IsSelected);
            Assert.Equal(2, viewModel.SelectionCount);
            Assert.Equal(0, viewModel.History.UndoCount);
            Assert.Equal(originalOrder, viewModel.Objects);
        }
        finally
        {
            window.Close();
        }
    }

    private static Border FindObjectBody(MainWindow window, DemoObject item)
        => Assert.Single(
            window.GetVisualDescendants().OfType<Border>(),
            x => ReferenceEquals(x.DataContext, item)
                 && x.IsHitTestVisible
                 && x.Tag is null
                 && !x.Classes.Contains("resize-handle"));

    private static void Click(Window window, Control target, RawInputModifiers modifiers)
    {
        var point = target.TranslatePoint(new Point(target.Bounds.Width / 2, target.Bounds.Height / 2), window);
        Assert.NotNull(point);

        window.MouseMove(point.Value, modifiers);
        window.MouseDown(point.Value, MouseButton.Left, modifiers);
        window.MouseUp(point.Value, MouseButton.Left, modifiers);
    }

    private static MainWindow ShowWindow()
    {
        var window = new MainWindow();
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.FindControl<MainView>("MainView")!.FindControl<Grid>("EditorSurface")!.Focus();
        Dispatcher.UIThread.RunJobs();
        return window;
    }
}
