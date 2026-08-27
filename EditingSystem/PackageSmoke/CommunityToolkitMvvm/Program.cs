using CommunityToolkit.Mvvm.ComponentModel;
using Jewelry.EditingSystem;
using Jewelry.EditingSystem.CommunityToolkit.Mvvm;

using var history = new History();
var model = new PackageModel(history);

model.Name = "field";
model.Count = 42;

history.Undo();
if (model.Count != 0)
    throw new InvalidOperationException("Partial property undo failed.");

history.Undo();
if (model.Name is not null)
    throw new InvalidOperationException("Field property undo failed.");

history.Redo();
history.Redo();
if (model.Name != "field" || model.Count != 42)
    throw new InvalidOperationException("Redo failed.");

[EditingHistory(nameof(_history))]
internal sealed partial class PackageModel : ObservableObject
{
    private readonly History _history;

    public PackageModel(History history)
    {
        _history = history;
    }

    [ObservableProperty]
    [Undoable]
    private string? _name;

    [ObservableProperty]
    [Undoable]
    public partial int Count { get; set; }
}
