using System.ComponentModel;
using Jewelry.EditingSystem;
using Jewelry.EditingSystem.Annotations;

using var history = new History();
var model = new Model(history);
var notified = 0;
model.PropertyChanged += (_, e) =>
{
    if (e.PropertyName == nameof(Model.Value))
        ++notified;
};

model.Value = 42;
if (model.Value != 42 || history.UndoCount != 1)
    throw new InvalidOperationException("Core Undoable package smoke failed while recording.");

history.Undo();
if (model.Value != 0)
    throw new InvalidOperationException("Core Undoable package smoke failed while undoing.");

history.Redo();
if (model.Value != 42)
    throw new InvalidOperationException("Core Undoable package smoke failed while redoing.");

if (notified != 3)
    throw new InvalidOperationException("Core Undoable package smoke failed to preserve PropertyChanged notifications.");

Console.WriteLine("Core package smoke passed.");

[EditingHistory(nameof(_history))]
[EditingPropertyChanged(nameof(NotifyPropertyChanged))]
internal sealed partial class Model : INotifyPropertyChanged
{
    private readonly History _history;

    public Model(History history)
    {
        _history = history;
    }

    [Undoable]
    public partial int Value { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void NotifyPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
