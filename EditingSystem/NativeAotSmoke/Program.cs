using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using Jewelry.Collections;
using Jewelry.EditingSystem;
using Jewelry.EditingSystem.CommunityToolkit.Mvvm;

Assert(!RuntimeFeature.IsDynamicCodeSupported, "NativeAOT smoke unexpectedly supports dynamic code.");

RunHistorySmoke();
RunEditableModelSmoke();
RunDirectModeSmoke();
RunCommunityToolkitSmoke();
RunObservableCollectionSmoke();
RunObservableHashSetSmoke();
RunObservableDictionarySmoke();
RunCoalescingSmoke();

Console.WriteLine("NativeAOT smoke tests passed.");

static void RunHistorySmoke()
{
    using var history = new History { MaxUndoCount = 8 };
    var value = 0;
    history.Push(() => value = 0, () => value = 1);
    value = 1;
    history.Undo();
    Assert(value == 0, "History.Undo failed.");
    history.Redo();
    Assert(value == 1, "History.Redo failed.");
}

static void RunEditableModelSmoke()
{
    using var history = new History();
    var model = new EditableSmokeModel(history);

    model.Value = 10;
    history.Undo();
    Assert(model.Value == 0, "EditableModelBase undo failed.");
    history.Redo();
    Assert(model.Value == 10, "EditableModelBase redo failed.");
}

static void RunDirectModeSmoke()
{
    using var history = new History();
    var model = new DirectSmokeModel(history);

    using (history.CoalescingBatch())
    {
        model.Value = 1;
        model.Value = 2;
        model.Value = 3;
    }

    history.Undo();
    Assert(model.Value == 0, "Direct mode coalesced undo failed.");
    history.Redo();
    Assert(model.Value == 3, "Direct mode coalesced redo failed.");
}

static void RunCommunityToolkitSmoke()
{
    using var history = new History();
    var model = new ToolkitSmokeModel(history);

    model.Value = 42;
    history.Undo();
    Assert(model.Value == 0, "CommunityToolkit undo failed.");
    history.Redo();
    Assert(model.Value == 42, "CommunityToolkit redo failed.");
}

static void RunObservableCollectionSmoke()
{
    using var history = new History();
    var model = new EditableSmokeModel(history)
    {
        Items = new ObservableCollection<int> { 10, 20, 30 }
    };
    history.Clear();

    model.Items.Move(0, 2);
    Assert(model.Items.SequenceEqual([20, 30, 10]), "ObservableCollection move failed.");
    history.Undo();
    Assert(model.Items.SequenceEqual([10, 20, 30]), "ObservableCollection move undo failed.");
    history.Redo();
    Assert(model.Items.SequenceEqual([20, 30, 10]), "ObservableCollection move redo failed.");

    model.Items.Clear();
    history.Undo();
    Assert(model.Items.SequenceEqual([20, 30, 10]), "ObservableCollection clear undo failed.");
}

static void RunObservableHashSetSmoke()
{
    using var history = new History();
    var model = new EditableSmokeModel(history)
    {
        Set = new ObservableHashSet<int>()
    };
    history.Clear();

    model.Set.Add(10);
    history.Undo();
    Assert(model.Set.Count == 0, "ObservableHashSet add undo failed.");
    history.Redo();
    Assert(model.Set.SetEquals([10]), "ObservableHashSet add redo failed.");

    model.Set.Clear();
    history.Undo();
    Assert(model.Set.SetEquals([10]), "ObservableHashSet clear undo failed.");
}

static void RunObservableDictionarySmoke()
{
    using var history = new History();
    var model = new EditableSmokeModel(history)
    {
        Dictionary = new ObservableDictionary<string, int>()
    };
    history.Clear();

    model.Dictionary.Add("one", 1);
    history.Undo();
    Assert(model.Dictionary.Count == 0, "ObservableDictionary add undo failed.");
    history.Redo();
    Assert(model.Dictionary["one"] == 1, "ObservableDictionary add redo failed.");

    model.Dictionary["one"] = 2;
    history.Undo();
    Assert(model.Dictionary["one"] == 1, "ObservableDictionary replace undo failed.");
    history.Redo();
    Assert(model.Dictionary["one"] == 2, "ObservableDictionary replace redo failed.");
}

static void RunCoalescingSmoke()
{
    using var history = new History();
    var model = new EditableSmokeModel(history);

    using (history.CoalescingBatch())
    {
        for (var i = 1; i <= 100; ++i)
            model.Value = i;
    }

    Assert(history.UndoCount == 1, "Coalescing batch created more than one undo entry.");
    history.Undo();
    Assert(model.Value == 0, "Coalescing batch undo failed.");
    history.Redo();
    Assert(model.Value == 100, "Coalescing batch redo failed.");
}

static void Assert(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}

internal sealed class EditableSmokeModel(History history) : EditableModelBase(history)
{
    public int Value
    {
        get => _value;
        set => SetEditableProperty(v => _value = v, _value, value);
    }

    public ObservableCollection<int> Items
    {
        get => _items;
        set => SetEditableProperty(v => _items = v, _items, value);
    }

    public ObservableHashSet<int> Set
    {
        get => _set;
        set => SetEditableProperty(v => _set = v, _set, value);
    }

    public ObservableDictionary<string, int> Dictionary
    {
        get => _dictionary;
        set => SetEditableProperty(v => _dictionary = v, _dictionary, value);
    }

    private int _value;
    private ObservableCollection<int> _items = new();
    private ObservableHashSet<int> _set = new();
    private ObservableDictionary<string, int> _dictionary = new();
}

internal sealed class DirectSmokeModel(History history) : INotifyPropertyChanged
{
    public int Value
    {
        get => _value;
        set => this.SetEditableProperty(history, v => SetField(ref _value, v), _value, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private int _value;
}

[EditingHistory(nameof(_history))]
internal sealed partial class ToolkitSmokeModel : ObservableObject
{
    public ToolkitSmokeModel(History history)
    {
        _history = history;
    }

    [ObservableProperty]
    [Undoable]
    private int _value;

    private readonly History _history;
}
