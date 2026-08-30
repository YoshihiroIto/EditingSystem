using Avalonia.Media;
using Avalonia.Media.Immutable;
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
    [Undoable, ObservableProperty]
    public partial string Name { get; set; } = name;

    [Undoable, ObservableProperty]
    public partial double X { get; set; } = x;

    [Undoable, ObservableProperty]
    public partial double Y { get; set; } = y;

    [Undoable, ObservableProperty]
    public partial double Width { get; set; } = width;

    [Undoable, ObservableProperty]
    public partial double Height { get; set; } = height;

    [Undoable, ObservableProperty]
    public partial Color Color { get; set; } = color;

    [Undoable, ObservableProperty]
    public partial double Opacity { get; set; } = 1d;

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public IBrush Brush => new ImmutableSolidColorBrush(Color);

    partial void OnColorChanged(Color oldValue, Color newValue)
        => OnPropertyChanged(nameof(Brush));
}