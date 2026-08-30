using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Jewelry.EditingSystem.Avalonia.Demo.Tests;

public sealed class ZOrderInteractionTests
{
    [AvaloniaFact]
    public void Drag_does_not_change_z_order()
    {
        var window = ShowWindow();
        try
        {
            var viewModel = (MainWindowViewModel)window.DataContext!;
            var target = viewModel.Objects[1];
            var originalOrder = viewModel.Objects.ToArray();
            var originalX = target.X;
            var body = FindObjectBody(window, target);
            var start = GetCenterInWindow(body, window);
            var end = start + new Vector(40, 0);

            window.MouseMove(start);
            window.MouseDown(start, MouseButton.Left);
            Dispatcher.UIThread.RunJobs();
            window.MouseMove(end, RawInputModifiers.LeftMouseButton);
            Dispatcher.UIThread.RunJobs();
            window.MouseUp(end, MouseButton.Left);
            Dispatcher.UIThread.RunJobs();

            Assert.True(target.X > originalX);
            Assert.Equal(originalOrder, viewModel.Objects);
            Assert.Equal(1, viewModel.History.UndoCount);

            viewModel.UndoCommand.Execute(null);
            Assert.Equal(originalX, target.X);
            Assert.Equal(originalOrder, viewModel.Objects);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Bring_selected_to_front_preserves_relative_order_and_undoes_as_one_entry()
    {
        var window = ShowWindow();
        try
        {
            var viewModel = (MainWindowViewModel)window.DataContext!;
            var a = viewModel.Objects[0];
            var b = viewModel.Objects[1];
            var c = viewModel.Objects[2];

            viewModel.ToggleSelection(c);
            Assert.Equal(0, viewModel.History.UndoCount);

            viewModel.BringSelectedToFrontCommand.Execute(null);

            Assert.Equal([b, a, c], viewModel.Objects);
            Assert.Equal(1, viewModel.History.UndoCount);

            viewModel.UndoCommand.Execute(null);
            Assert.Equal([a, b, c], viewModel.Objects);

            viewModel.RedoCommand.Execute(null);
            Assert.Equal([b, a, c], viewModel.Objects);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Send_selected_to_back_preserves_relative_order_and_undoes_as_one_entry()
    {
        var window = ShowWindow();
        try
        {
            var viewModel = (MainWindowViewModel)window.DataContext!;
            var a = viewModel.Objects[0];
            var b = viewModel.Objects[1];
            var c = viewModel.Objects[2];

            viewModel.ClearSelection();
            viewModel.ToggleSelection(b);
            viewModel.ToggleSelection(c);
            Assert.Equal(0, viewModel.History.UndoCount);

            viewModel.SendSelectedToBackCommand.Execute(null);

            Assert.Equal([b, c, a], viewModel.Objects);
            Assert.Equal(1, viewModel.History.UndoCount);

            viewModel.UndoCommand.Execute(null);
            Assert.Equal([a, b, c], viewModel.Objects);

            viewModel.RedoCommand.Execute(null);
            Assert.Equal([b, c, a], viewModel.Objects);
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
        window.FindControl<Grid>("EditorSurface")!.Focus();
        Dispatcher.UIThread.RunJobs();
        return window;
    }
}
