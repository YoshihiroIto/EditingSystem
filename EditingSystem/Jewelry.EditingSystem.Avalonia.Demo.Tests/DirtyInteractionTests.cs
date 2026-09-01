using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;

namespace Jewelry.EditingSystem.Avalonia.Demo.Tests;

public sealed class DirtyInteractionTests
{
    [AvaloniaFact]
    public void Mark_Saved_button_and_undo_redo_update_visible_dirty_state()
    {
        var window = ShowWindow();
        try
        {
            var viewModel = (MainWindowViewModel)window.DataContext!;
            var view = window.FindControl<MainView>("MainView")!;
            var markSavedButton = view.FindControl<Button>("MarkSavedButton")!;
            var saveStateText = view.FindControl<TextBlock>("SaveStateText")!;

            Assert.False(viewModel.History.IsDirty);
            Assert.Equal("EditingSystem Avalonia Demo", window.Title);
            Assert.Equal("Saved", saveStateText.Text);
            Assert.False(markSavedButton.IsEnabled);

            viewModel.AddObjectCommand.Execute(null);
            Dispatcher.UIThread.RunJobs();

            Assert.True(viewModel.History.IsDirty);
            Assert.Equal("EditingSystem Avalonia Demo *", window.Title);
            Assert.Equal("Unsaved changes", saveStateText.Text);
            Assert.True(markSavedButton.IsEnabled);

            Click(window, markSavedButton);
            Dispatcher.UIThread.RunJobs();

            Assert.False(viewModel.History.IsDirty);
            Assert.Equal("EditingSystem Avalonia Demo", window.Title);
            Assert.Equal("Saved", saveStateText.Text);
            Assert.False(markSavedButton.IsEnabled);

            viewModel.AddObjectCommand.Execute(null);
            Dispatcher.UIThread.RunJobs();
            Assert.True(viewModel.History.IsDirty);

            window.KeyPressQwerty(PhysicalKey.Z, RawInputModifiers.Control);
            Dispatcher.UIThread.RunJobs();
            Assert.False(viewModel.History.IsDirty);
            Assert.Equal("EditingSystem Avalonia Demo", window.Title);
            Assert.Equal("Saved", saveStateText.Text);

            window.KeyPressQwerty(PhysicalKey.Y, RawInputModifiers.Control);
            Dispatcher.UIThread.RunJobs();
            Assert.True(viewModel.History.IsDirty);
            Assert.Equal("EditingSystem Avalonia Demo *", window.Title);
            Assert.Equal("Unsaved changes", saveStateText.Text);
        }
        finally
        {
            window.Close();
        }
    }

    private static void Click(Window window, Control target)
    {
        var point = target.TranslatePoint(new Point(target.Bounds.Width / 2, target.Bounds.Height / 2), window);
        Assert.NotNull(point);

        window.MouseMove(point.Value);
        window.MouseDown(point.Value, MouseButton.Left);
        window.MouseUp(point.Value, MouseButton.Left);
    }

    private static MainWindow ShowWindow()
    {
        var window = new MainWindow();
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }
}
