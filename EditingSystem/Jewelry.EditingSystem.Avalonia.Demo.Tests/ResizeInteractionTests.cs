using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Jewelry.EditingSystem.Avalonia.Demo.Tests;

public sealed class ResizeInteractionTests
{
    [AvaloniaFact]
    public void Multi_selection_resize_stops_on_mouse_up()
    {
        var window = ShowWindow();
        try
        {
            var (viewModel, item, handle) = PrepareMultiSelectionResize(window);
            var start = GetCenterInWindow(handle, window);
            var dragPoint = start + new Vector(40, 0);

            window.MouseMove(start);
            window.MouseDown(start, MouseButton.Left);
            Dispatcher.UIThread.RunJobs();

            window.MouseMove(dragPoint, RawInputModifiers.LeftMouseButton);
            Dispatcher.UIThread.RunJobs();
            Assert.True(item.Width > 150);

            window.MouseUp(dragPoint, MouseButton.Left);
            Dispatcher.UIThread.RunJobs();

            var widthAfterRelease = item.Width;
            window.MouseMove(dragPoint + new Vector(40, 0));
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(widthAfterRelease, item.Width);
            Assert.False(viewModel.History.IsInBatch);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Multi_selection_resize_recovers_when_release_event_is_missed()
    {
        var window = ShowWindow();
        try
        {
            var (viewModel, item, handle) = PrepareMultiSelectionResize(window);
            var start = GetCenterInWindow(handle, window);
            var dragPoint = start + new Vector(40, 0);

            window.MouseMove(start);
            window.MouseDown(start, MouseButton.Left);
            Dispatcher.UIThread.RunJobs();

            window.MouseMove(dragPoint, RawInputModifiers.LeftMouseButton);
            Dispatcher.UIThread.RunJobs();
            var widthWhileDragging = item.Width;
            Assert.True(widthWhileDragging > 150);

            // Simulate the platform no longer reporting the button as pressed even though
            // the PointerReleased event itself was missed by the resize handle.
            window.MouseMove(dragPoint + new Vector(40, 0));
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(widthWhileDragging, item.Width);
            Assert.False(viewModel.History.IsInBatch);
        }
        finally
        {
            window.Close();
        }
    }

    private static (MainWindowViewModel ViewModel, DemoObject Item, Border Handle)
        PrepareMultiSelectionResize(MainWindow window)
    {
        var viewModel = (MainWindowViewModel)window.DataContext!;
        var item = viewModel.Objects[0];

        viewModel.ToggleSelection(viewModel.Objects[1]);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(2, viewModel.SelectionCount);

        var handle = Assert.Single(
            window.GetVisualDescendants().OfType<Border>(),
            x => ReferenceEquals(x.DataContext, item) && Equals(x.Tag, "East") && x.IsVisible);

        return (viewModel, item, handle);
    }

    private static Point GetCenterInWindow(Control control, Window window)
    {
        var point = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window);
        Assert.NotNull(point);
        return point.Value;
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
