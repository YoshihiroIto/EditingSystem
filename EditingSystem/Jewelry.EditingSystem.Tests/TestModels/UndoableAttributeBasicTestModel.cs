using System.ComponentModel;
using Jewelry.EditingSystem.Annotations;

namespace Jewelry.EditingSystem.Tests.TestModels;

[EditingHistory(nameof(_history))]
public sealed partial class UndoableAttributeBasicTestModel : INotifyPropertyChanged
{
    private readonly History _history;

    public UndoableAttributeBasicTestModel(History history)
    {
        _history = history;
    }

    [Undoable]
    public partial int Value { get; set; }

    [Undoable]
    public partial int OtherValue { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

[EditingHistory(nameof(_history))]
public sealed partial class UndoableAttributeDirectEventTestModel : INotifyPropertyChanged
{
    private readonly History _history;

    public UndoableAttributeDirectEventTestModel(History history)
    {
        _history = history;
    }

    [Undoable]
    public partial int Value { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;
}

[EditingHistory(nameof(_history))]
public sealed partial class UndoableAttributePlainTestModel
{
    private readonly History _history;

    public UndoableAttributePlainTestModel(History history)
    {
        _history = history;
    }

    [Undoable]
    public partial int Value { get; set; }
}
