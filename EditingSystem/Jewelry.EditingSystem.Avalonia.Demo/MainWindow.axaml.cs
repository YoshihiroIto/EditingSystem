using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Jewelry.EditingSystem.Avalonia.Demo;

public sealed partial class MainWindow : Window
{
    private const double MinObjectSize = 32d;

    private readonly Dictionary<DemoObject, Point> _moveStartPositions = [];
    private Point _pointerStart;
    private Control? _capturedControl;
    private DemoObject? _pointerOperationObject;
    private DemoObject? _resizeObject;
    private ResizeDirection _resizeDirection;
    private Rect _resizeStartBounds;
    private bool _pointerContinuousEdit;
    private bool _inspectorContinuousEdit;

    public MainWindow()
    {
        InitializeComponent();

        var opacityEditor = this.FindControl<Slider>("OpacityEditor")!;
        var colorEditor = this.FindControl<ColorView>("ColorEditor")!;

        opacityEditor.AddHandler(PointerPressedEvent, Inspector_PointerPressed, RoutingStrategies.Tunnel, true);
        colorEditor.AddHandler(PointerPressedEvent, Inspector_PointerPressed, RoutingStrategies.Tunnel, true);
        EditorSurface.PointerMoved += EditorSurface_PointerMoved;
        EditorSurface.PointerCaptureLost += EditorSurface_PointerCaptureLost;
        AddHandler(PointerReleasedEvent, Window_PointerReleased, RoutingStrategies.Bubble, true);
    }

    private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext!;
    
    private void Window_Closed(object? sender, EventArgs e)
    {
        ViewModel.Dispose();
    }

    private void EditorSurface_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(EditorSurface).Properties.IsLeftButtonPressed)
            return;

        CommitNameEdit();
        ViewModel.ClearSelection();
    }

    private void Object_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control { DataContext: DemoObject item } control)
            return;
        if (!e.GetCurrentPoint(control).Properties.IsLeftButtonPressed)
            return;

        CommitNameEdit();

        if ((e.KeyModifiers & KeyModifiers.Control) is not 0)
        {
            ViewModel.ToggleSelection(item);
            e.Handled = true;
            return;
        }

        if (!item.IsSelected)
            ViewModel.SelectOnly(item);

        _pointerStart = e.GetPosition(EditorSurface);
        _moveStartPositions.Clear();
        foreach (var selected in ViewModel.GetSelectedObjects())
            _moveStartPositions[selected] = new Point(selected.X, selected.Y);

        _pointerOperationObject = item;
        _capturedControl = control;
        e.Pointer.Capture(EditorSurface);
        e.Handled = true;
    }

    private void Object_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (_capturedControl is null || _moveStartPositions.Count is 0)
            return;
        if (!e.GetCurrentPoint(EditorSurface).Properties.IsLeftButtonPressed)
        {
            EndPointerOperation(e.Pointer);
            return;
        }

        var delta = e.GetPosition(EditorSurface) - _pointerStart;
        if (delta.X == 0d && delta.Y == 0d)
            return;

        EnsurePointerContinuousEdit();
        foreach (var (item, start) in _moveStartPositions)
        {
            item.X = start.X + delta.X;
            item.Y = start.Y + delta.Y;
        }

        e.Handled = true;
    }

    private void Object_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_moveStartPositions.Count == 0)
            return;

        EndPointerOperation(e.Pointer);
        e.Handled = true;
    }

    private void ResizeHandle_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control { DataContext: DemoObject item } control)
            return;
        if (!e.GetCurrentPoint(control).Properties.IsLeftButtonPressed)
            return;
        if (control.Tag is not string directionText || !Enum.TryParse(directionText, out ResizeDirection direction))
            return;

        _resizeObject = item;
        _resizeDirection = direction;
        _resizeStartBounds = new Rect(item.X, item.Y, item.Width, item.Height);
        _pointerStart = e.GetPosition(EditorSurface);
        _pointerOperationObject = item;
        _capturedControl = control;
        e.Pointer.Capture(EditorSurface);
        e.Handled = true;
    }

    private void ResizeHandle_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (_resizeObject is null || _capturedControl is null)
            return;
        if (!e.GetCurrentPoint(EditorSurface).Properties.IsLeftButtonPressed)
        {
            EndPointerOperation(e.Pointer);
            return;
        }

        var delta = e.GetPosition(EditorSurface) - _pointerStart;
        if (delta.X == 0d && delta.Y == 0d)
            return;

        EnsurePointerContinuousEdit();
        ApplyResize(_resizeObject, _resizeDirection, _resizeStartBounds, delta);
        e.Handled = true;
    }

    private void ResizeHandle_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_resizeObject is null)
            return;

        EndPointerOperation(e.Pointer);
        e.Handled = true;
    }

    private void EditorSurface_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (_resizeObject is not null)
            ResizeHandle_PointerMoved(sender, e);
        else if (_moveStartPositions.Count > 0)
            Object_PointerMoved(sender, e);
    }

    private void EditorSurface_PointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (_capturedControl is not null)
            CompletePointerOperation();
    }

    private void EnsurePointerContinuousEdit()
    {
        if (_pointerContinuousEdit)
            return;

        ViewModel.BeginContinuousEdit();
        if (_pointerOperationObject is not null)
            ViewModel.BringToFront(_pointerOperationObject);
        _pointerContinuousEdit = true;
    }

    private static void ApplyResize(DemoObject item, ResizeDirection direction, Rect start, Vector delta)
    {
        var resizeLeft = direction is ResizeDirection.West or ResizeDirection.NorthWest or ResizeDirection.SouthWest;
        var resizeRight = direction is ResizeDirection.East or ResizeDirection.NorthEast or ResizeDirection.SouthEast;
        var resizeTop = direction is ResizeDirection.North or ResizeDirection.NorthEast or ResizeDirection.NorthWest;
        var resizeBottom = direction is ResizeDirection.South or ResizeDirection.SouthEast or ResizeDirection.SouthWest;

        var x = start.X;
        var y = start.Y;
        var width = start.Width;
        var height = start.Height;

        if (resizeLeft)
        {
            width = Math.Max(MinObjectSize, start.Width - delta.X);
            x = start.Right - width;
        }
        else if (resizeRight)
        {
            width = Math.Max(MinObjectSize, start.Width + delta.X);
        }

        if (resizeTop)
        {
            height = Math.Max(MinObjectSize, start.Height - delta.Y);
            y = start.Bottom - height;
        }
        else if (resizeBottom)
        {
            height = Math.Max(MinObjectSize, start.Height + delta.Y);
        }

        item.X = x;
        item.Y = y;
        item.Width = width;
        item.Height = height;
    }

    private void Inspector_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_inspectorContinuousEdit || sender is not Control control)
            return;
        if (!e.GetCurrentPoint(control).Properties.IsLeftButtonPressed)
            return;

        CommitNameEdit();
        _inspectorContinuousEdit = true;
        ViewModel.BeginContinuousEdit();
    }

    private void Window_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_capturedControl is not null)
            EndPointerOperation(e.Pointer);

        if (_inspectorContinuousEdit)
        {
            _inspectorContinuousEdit = false;
            ViewModel.EndContinuousEdit();
        }
    }

    private void CommitNameEdit()
    {
        if (!NameEditor.IsKeyboardFocusWithin)
            return;

        BindingOperations.GetBindingExpressionBase(NameEditor, TextBox.TextProperty)?.UpdateSource();
    }

    private void EndPointerOperation(IPointer pointer)
    {
        CompletePointerOperation();

        if (ReferenceEquals(pointer.Captured, EditorSurface))
            pointer.Capture(null);
    }

    private void CompletePointerOperation()
    {
        _capturedControl = null;
        _moveStartPositions.Clear();
        _pointerOperationObject = null;
        _resizeObject = null;

        if (_pointerContinuousEdit)
        {
            _pointerContinuousEdit = false;
            ViewModel.EndContinuousEdit();
        }
    }
}
