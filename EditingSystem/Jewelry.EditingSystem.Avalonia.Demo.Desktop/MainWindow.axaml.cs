using Avalonia.Controls;

namespace Jewelry.EditingSystem.Avalonia.Demo.Desktop;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = MainView.DataContext;
    }

    private void Window_Closed(object? sender, EventArgs e)
        => ((MainViewViewModel)DataContext!).Dispose();
}
