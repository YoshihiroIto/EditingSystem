using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Jewelry.EditingSystem.CommunityToolkit.Mvvm;

namespace Jewelry.EditingSystem.Avalonia.Demo;

[EditingHistory(nameof(history))]
public sealed partial class DemoObject(
    History history,
    string name,
    double x,
    double y,
    double width,
    double height,
    Color color)
    : ObservableObject
{
    [ObservableProperty]
    [Undoable]
    public partial string Name { get; set; } = name;

    [ObservableProperty]
    [Undoable]
    public partial double X { get; set; } = x;

    [ObservableProperty]
    [Undoable]
    public partial double Y { get; set; } = y;

    [ObservableProperty]
    [Undoable]
    public partial double Width { get; set; } = width;

    [ObservableProperty]
    [Undoable]
    public partial double Height { get; set; } = height;

    [ObservableProperty]
    [Undoable]
    public partial Color Color { get; set; } = color;

    [ObservableProperty]
    [Undoable]
    public partial double Opacity { get; set; } = 1d;

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public IBrush Brush => new SolidColorBrush(Color);

    partial void OnColorChanged(Color oldValue, Color newValue)
        => OnPropertyChanged(nameof(Brush));
}