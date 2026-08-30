using System.Collections.ObjectModel;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jewelry.EditingSystem.CommunityToolkit.Mvvm;

namespace Jewelry.EditingSystem.Avalonia.Demo;

[EditingHistory(nameof(_history))]
public sealed partial class MainWindowViewModel : ObservableObject, IDisposable
{
    private static readonly Color[] ObjectColors =
    [
        Colors.CornflowerBlue,
        Colors.MediumSeaGreen,
        Colors.Orange,
        Colors.MediumPurple,
        Colors.OrangeRed,
        Colors.RoyalBlue,
        Colors.Green,
        Colors.Orchid,
        Colors.OliveDrab,
        Colors.IndianRed
    ];

    private readonly History _history = new();
    private bool _isRefreshingInspector;
    private bool _isContinuousEdit;
    private int _nextObjectId = 1;

    [ObservableProperty]
    [Undoable]
    public partial ObservableCollection<DemoObject> Objects { get; set; }

    [ObservableProperty]
    public partial DemoObject? SelectedObject { get; set; }

    [ObservableProperty]
    public partial int SelectionCount { get; set; }

    [ObservableProperty]
    public partial bool HasMixedColor { get; set; }

    [ObservableProperty]
    public partial bool HasMixedOpacity { get; set; }

    public History History => _history;
    public bool HasSelection => SelectionCount > 0;
    public bool HasSingleSelection => SelectionCount is 1;
    public bool HasMultipleSelection => SelectionCount > 1;

    public Color SelectionColor
    {
        get;
        set
        {
            var changed = SetProperty(ref field, value);
            if (_isRefreshingInspector || (!changed && !HasMixedColor))
                return;

            ApplyToSelection(item => item.Color = value);
            HasMixedColor = false;
        }
    } = Colors.CornflowerBlue;

    public double SelectionOpacity
    {
        get;
        set
        {
            value = Math.Clamp(value, 0d, 1d);
            var changed = SetProperty(ref field, value);
            if (_isRefreshingInspector || (!changed && !HasMixedOpacity))
                return;

            ApplyToSelection(item => item.Opacity = value);
            HasMixedOpacity = false;
        }
    } = 1d;

    public MainWindowViewModel()
    {
        using (_history.Pause())
        {
            // Assign through the generated property so collection change tracking is attached,
            // but keep all startup state out of the undo history.
            Objects =
            [
                CreateObject(80d, 70d, 150d, 100d),
                CreateObject(320d, 150d, 170d, 110d),
                CreateObject(170d, 330d, 140d, 120d)
            ];
        }

        SelectOnly(Objects[0]);
    }

    public void Dispose()
    {
        _history.Dispose();
    }

    public IReadOnlyList<DemoObject> GetSelectedObjects()
    {
        return [.. Objects.Where(item => item.IsSelected)];
    }

    public void SelectOnly(DemoObject item)
    {
        foreach (var candidate in Objects)
            candidate.IsSelected = ReferenceEquals(candidate, item);

        UpdateSelectionState();
    }

    public void ToggleSelection(DemoObject item)
    {
        item.IsSelected = !item.IsSelected;
        UpdateSelectionState();
    }

    public void ClearSelection()
    {
        foreach (var item in Objects)
            item.IsSelected = false;

        UpdateSelectionState();
    }

    public void BringToFront(DemoObject item)
    {
        var index = Objects.IndexOf(item);
        if (index < 0 || index == Objects.Count - 1)
            return;

        Objects.Move(index, Objects.Count - 1);
    }

    public void BeginContinuousEdit()
    {
        if (_isContinuousEdit)
            return;

        _isContinuousEdit = true;
        _history.BeginCoalescingBatch();
    }

    public void EndContinuousEdit()
    {
        if (!_isContinuousEdit)
            return;

        _history.EndCoalescingBatch();
        _isContinuousEdit = false;
        RefreshInspector();
    }

    [RelayCommand]
    private void Undo()
    {
        if (_history.TryUndo())
            UpdateSelectionState();
    }

    [RelayCommand]
    private void Redo()
    {
        if (_history.TryRedo())
            UpdateSelectionState();
    }

    [RelayCommand]
    private void AddObject()
    {
        var index = Objects.Count;
        var x = Random.Shared.NextDouble() * 600d + 40d;
        var y = Random.Shared.NextDouble() * 600d + 40d;
        var item = CreateObject(x, y, 140d, 90d);

        Objects.Add(item);
        SelectOnly(item);
    }

    [RelayCommand]
    private void DeleteSelected()
    {
        var selected = GetSelectedObjects();
        if (selected.Count is 0)
            return;

        foreach (var item in selected)
            item.IsSelected = false;

        using (_history.Batch())
        {
            foreach (var item in selected)
                Objects.Remove(item);
        }

        UpdateSelectionState();
    }

    [RelayCommand]
    private void AlignLeft()
    {
        var selected = GetSelectedObjects();
        if (selected.Count < 2)
            return;

        var left = selected.Min(item => item.X);
        using (_history.Batch())
        {
            foreach (var item in selected)
                item.X = left;
        }
    }

    [RelayCommand]
    private void AlignTop()
    {
        var selected = GetSelectedObjects();
        if (selected.Count < 2)
            return;

        var top = selected.Min(item => item.Y);
        using (_history.Batch())
        {
            foreach (var item in selected)
                item.Y = top;
        }
    }

    private DemoObject CreateObject(double x, double y, double width, double height)
    {
        var id = _nextObjectId++;
        return new DemoObject(
            _history,
            $"Object {id}",
            x,
            y,
            width,
            height,
            ObjectColors[(id - 1) % ObjectColors.Length]);
    }

    private void ApplyToSelection(Action<DemoObject> apply)
    {
        var selected = GetSelectedObjects();
        if (selected.Count == 0)
            return;

        if (_history.IsInBatch)
        {
            foreach (var item in selected)
                apply(item);
            return;
        }

        using (_history.Batch())
        {
            foreach (var item in selected)
                apply(item);
        }
    }

    private void UpdateSelectionState()
    {
        var selected = GetSelectedObjects();
        SelectionCount = selected.Count;
        SelectedObject = selected.Count is 1 ? selected[0] : null;
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(HasSingleSelection));
        OnPropertyChanged(nameof(HasMultipleSelection));
        RefreshInspector();
    }

    private void RefreshInspector()
    {
        var selected = GetSelectedObjects();
        _isRefreshingInspector = true;

        try
        {
            if (selected.Count is 0)
            {
                HasMixedColor = false;
                HasMixedOpacity = false;
                return;
            }

            var first = selected[0];
            SelectionColor = first.Color;
            SelectionOpacity = first.Opacity;
            HasMixedColor = selected.Skip(1).Any(item => item.Color != first.Color);
            HasMixedOpacity = selected.Skip(1).Any(item => Math.Abs(item.Opacity - first.Opacity) > 0.0001d);
        }
        finally
        {
            _isRefreshingInspector = false;
        }
    }
}