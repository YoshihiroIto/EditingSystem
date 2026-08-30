using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Jewelry.EditingSystem.Avalonia.Demo;

namespace Jewelry.EditingSystem.Avalonia.Demo.Tests;

public sealed class ColorViewInteractionTests
{
    [AvaloniaFact]
    public void Spectrum_edit_commits_pending_name_without_stealing_focus()
    {
        var window = ShowWindow();
        try
        {
            var viewModel = (MainWindowViewModel)window.DataContext!;
            var item = Assert.Single(viewModel.Objects, x => x.IsSelected);
            var initialName = item.Name;
            var initialColor = item.Color;
            var nameEditor = window.FindControl<TextBox>("NameEditor")!;

            nameEditor.Focus();
            nameEditor.Text = "Renamed from spectrum";
            Dispatcher.UIThread.RunJobs();

            Assert.True(nameEditor.IsKeyboardFocusWithin);
            Assert.Equal(initialName, item.Name);
            Assert.Equal(0, viewModel.History.UndoCount);

            var spectrum = Assert.Single(window.GetVisualDescendants().OfType<ColorSpectrum>());
            Click(window, spectrum, new Point(spectrum.Bounds.Width * 0.25, spectrum.Bounds.Height * 0.25));
            Dispatcher.UIThread.RunJobs();

            Assert.True(nameEditor.IsKeyboardFocusWithin);
            Assert.Equal("Renamed from spectrum", item.Name);
            Assert.NotEqual(initialColor, item.Color);
            Assert.Equal(2, viewModel.History.UndoCount);

            Assert.True(viewModel.History.TryUndo());
            Assert.Equal(initialColor, item.Color);
            Assert.Equal("Renamed from spectrum", item.Name);

            Assert.True(viewModel.History.TryUndo());
            Assert.Equal(initialName, item.Name);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Accent_edit_commits_pending_name_without_stealing_focus()
    {
        var window = ShowWindow();
        try
        {
            var viewModel = (MainWindowViewModel)window.DataContext!;
            var item = Assert.Single(viewModel.Objects, x => x.IsSelected);
            var initialName = item.Name;
            var initialColor = item.Color;
            var nameEditor = window.FindControl<TextBox>("NameEditor")!;

            nameEditor.Focus();
            nameEditor.Text = "Renamed from accent";
            Dispatcher.UIThread.RunJobs();

            Assert.True(nameEditor.IsKeyboardFocusWithin);
            Assert.Equal(initialName, item.Name);
            Assert.Equal(0, viewModel.History.UndoCount);

            var accent = Assert.Single(
                window.GetVisualDescendants().OfType<Border>(),
                x => x.Name == "PART_AccentDecrement2Border");

            Click(window, accent, Center(accent));
            Dispatcher.UIThread.RunJobs();

            Assert.True(nameEditor.IsKeyboardFocusWithin);
            Assert.Equal("Renamed from accent", item.Name);
            Assert.NotEqual(initialColor, item.Color);
            Assert.Equal(2, viewModel.History.UndoCount);

            Assert.True(viewModel.History.TryUndo());
            Assert.Equal(initialColor, item.Color);
            Assert.Equal("Renamed from accent", item.Name);

            Assert.True(viewModel.History.TryUndo());
            Assert.Equal(initialName, item.Name);
        }
        finally
        {
            window.Close();
        }
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

    private static Point Center(Control control)
        => new(control.Bounds.Width / 2, control.Bounds.Height / 2);

    private static void Click(Window window, Visual target, Point localPoint)
    {
        var point = target.TranslatePoint(localPoint, window);
        Assert.NotNull(point);

        window.MouseMove(point.Value, RawInputModifiers.None);
        window.MouseDown(point.Value, MouseButton.Left, RawInputModifiers.None);
        window.MouseUp(point.Value, MouseButton.Left, RawInputModifiers.None);
    }
}
