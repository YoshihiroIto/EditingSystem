using System.ComponentModel;
using Jewelry.EditingSystem.Annotations;

namespace Jewelry.EditingSystem.Tests.TestModels;

public abstract class UndoableNotificationBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void RaisePropertyChanged(PropertyChangedEventArgs args)
    {
        PropertyChanged?.Invoke(this, args);
    }
}

[EditingHistory(nameof(_history))]
public sealed partial class UndoableAttributeInheritedNotificationTestModel : UndoableNotificationBase
{
    private readonly History _history;

    public UndoableAttributeInheritedNotificationTestModel(History history)
    {
        _history = history;
    }

    [Undoable]
    public partial int Value { get; set; }
}
