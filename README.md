# EditingSystem

[![Jewelry.EditingSystem NuGet package](https://img.shields.io/nuget/v/Jewelry.EditingSystem)](https://www.nuget.org/packages/Jewelry.EditingSystem) [![Build status](https://ci.appveyor.com/api/projects/status/x42th0lpkuldqhg8?svg=true)](https://ci.appveyor.com/project/YoshihiroIto/editingsystem) [![MIT License](http://img.shields.io/badge/license-MIT-lightgray)](LICENSE)

[日本語](README.ja.md)

<p align="center">
  <img src="site/img/icon.svg" alt="EditingSystem" width="160">
</p>

> Full documentation is also available at [yoshihiroito.github.io/EditingSystem](https://yoshihiroito.github.io/EditingSystem/).

EditingSystem is a NativeAOT-friendly undo/redo library for .NET. Property edits, continuous edits, batches, and collection changes can share the same `History`. The generator emits property setters at compile time, and the collection adapter includes a NativeAOT-safe path for arbitrary `ICollection<T>` implementations.

[Try the demo in your browser.](https://yoshihiroito.github.io/EditingSystem/demo/)

## Install

```text
PM> Install-Package Jewelry.EditingSystem
```

## Primary pattern: `[Undoable]`

For ordinary models, declare each undoable value as a C# partial property and annotate it with `[Undoable]`.

```cs
using Jewelry.EditingSystem;
using Jewelry.EditingSystem.Annotations;

[EditingHistory(nameof(history))]
public sealed partial class Document(History history)
{
    [Undoable]
    public partial string? Name { get; set; }

    [Undoable]
    public partial double X { get; set; }

    [Undoable]
    public partial double Y { get; set; }
}
```

The source generator implements the property and records its changes.

```cs
using var history = new History();
var document = new Document(history);

document.X = 100;
document.X = 200;

history.Undo(); // X == 100
history.Undo(); // X == 0
history.Redo(); // X == 100
```

`EditingHistory` can name a non-null `History` field, property, or primary-constructor parameter.

### `INotifyPropertyChanged`

`INotifyPropertyChanged` is optional. When the target type implements it, the source generator resolves a notification path at compile time and preserves notifications for normal changes, undo, and redo.

```cs
using System.ComponentModel;
using Jewelry.EditingSystem;
using Jewelry.EditingSystem.Annotations;

[EditingHistory(nameof(history))]
public sealed partial class Document(History history) : INotifyPropertyChanged
{
    [Undoable]
    public partial string? Name { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
```

#### Explicit notification method

If your notification method is not named `RaisePropertyChanged`, `OnPropertyChanged`, or `NotifyPropertyChanged`, use `[EditingPropertyChanged]` to select it explicitly.

```cs
[EditingHistory(nameof(history))]
[EditingPropertyChanged(nameof(InvokePropertyChanged))]
public sealed partial class Document(History history) : INotifyPropertyChanged
{
    [Undoable]
    public partial string? Name { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void InvokePropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
```

The method selected by `EditingPropertyChanged` takes precedence over automatic discovery and is called for normal changes, undo, and redo. It must be an accessible instance `void` method with exactly one `string` or `PropertyChangedEventArgs` parameter. Generic methods are not supported.

If the configured method does not exist, has an unsupported signature, or is inaccessible, the generator reports `JES006` as an error.

When `EditingPropertyChanged` is not specified, the generator looks for these notification paths in order:

1. accessible `RaisePropertyChanged(string)`
2. accessible `OnPropertyChanged(string)`
3. accessible `NotifyPropertyChanged(string)`
4. accessible `RaisePropertyChanged(PropertyChangedEventArgs)`
5. accessible `OnPropertyChanged(PropertyChangedEventArgs)`
6. accessible `NotifyPropertyChanged(PropertyChangedEventArgs)`
7. a `PropertyChanged` event declared by the target partial class itself

Protected methods on base classes are supported. Roslyn performs the accessibility check at compile time. If the type implements `INotifyPropertyChanged` but no supported path is available, undo/redo remains enabled and diagnostic `JES005` is reported as a warning.

## CommunityToolkit.Mvvm integration

CommunityToolkit.Mvvm integration remains a first-class usage pattern, and the existing API continues to work unchanged.

```text
PM> Install-Package Jewelry.EditingSystem.CommunityToolkit.Mvvm
```

```cs
using CommunityToolkit.Mvvm.ComponentModel;
using Jewelry.EditingSystem;
using Jewelry.EditingSystem.CommunityToolkit.Mvvm;

[EditingHistory(nameof(_history))]
public sealed partial class SampleViewModel : ObservableObject
{
    private readonly History _history;

    public SampleViewModel(History history)
    {
        _history = history;
    }

    [Undoable, ObservableProperty]
    private string? name;

    [Undoable, ObservableProperty]
    public partial int Count { get; set; }
}
```

This integration reuses the setter generated by CommunityToolkit.Mvvm during undo and redo, preserving its `PropertyChanged` / `PropertyChanging`, validation, command notification, and recipient-broadcast pipeline.

`ObservableObject` inheritance is optional. CommunityToolkit.Mvvm's `[INotifyPropertyChanged]` attribute can be used when a view model already has another base class.

```cs
[INotifyPropertyChanged]
[EditingHistory(nameof(history))]
public sealed partial class DerivedViewModel(History history) : ExistingViewModelBase
{
    [Undoable, ObservableProperty]
    public partial int Count { get; set; }
}
```

The integration reserves the one-parameter `OnXChanging(T value)` and `OnXChanged(T value)` hooks. The two-parameter hooks remain available for application-specific behavior.

> Attributes in `Jewelry.EditingSystem.Annotations` are for standalone partial properties. Attributes in `Jewelry.EditingSystem.CommunityToolkit.Mvvm` are for CommunityToolkit.Mvvm `[ObservableProperty]` integration. Existing CommunityToolkit.Mvvm code does not need to change.

## Choosing a recording scope

`Transaction`, `Batch`, `CoalescingBatch`, and `Pause` all affect how `History` records changes, but they solve different problems. Model changes are applied immediately in every scope; the difference is what is recorded and what happens when the scope ends.

| Scope | Purpose | When the scope ends | Result in history | Typical use |
| --- | --- | --- | --- | --- |
| `Transaction` | Make a group of recorded changes atomic | `Commit()` keeps all changes; no `Commit()` or `Rollback()` restores them | One undo action only after commit | An operation that can fail validation or must be cancelled as a whole |
| `Batch` | Group a sequence of changes | Changes remain, including when the scope exits because of an exception | One undo action containing every intermediate change | Move several objects or update several related properties |
| `CoalescingBatch` | Group a continuous edit while removing redundant property history | Changes remain, including when the scope exits because of an exception | One undo action; repeated writes to the same target property keep only the first old value and final new value | Sliders, color pickers, dragging, and gizmos |
| `Pause` | Temporarily stop history recording | Changes remain and cannot be undone through this `History` | No undo action | Initialization, loading, or synchronization that must not enter history |

Choose `Transaction` when rollback is required, `Batch` when grouping alone is enough, `CoalescingBatch` for high-frequency continuous values, and `Pause` when the change must not be undoable.

`Batch()` and `CoalescingBatch()` can run inside a transaction. A transaction already commits as one undo action, so an inner `Batch()` is not required merely for grouping; an inner `CoalescingBatch()` is useful for avoiding replay of intermediate values. A transaction cannot begin inside a batch or pause, and a pause cannot begin inside a transaction.

## Transaction

Use a transaction when an operation must either commit all recorded changes as one undo action or roll them all back. A transaction that leaves its `using` scope without `Commit()` is rolled back automatically.

```cs
using var transaction = history.BeginTransaction();

document.X = 100;
document.Y = 200;
ValidateDocument();

transaction.Commit();
```

Transactions can be nested. Committing an inner transaction merges it into its parent; rolling it back restores only the changes made since that inner transaction began. Rolling back the outer transaction also restores changes from committed inner transactions.

`Batch()` and `CoalescingBatch()` can be used inside a transaction. Begin and end those scopes before committing or rolling back the transaction. A transaction cannot begin inside a batch or while recording is paused, and recording cannot be paused while a transaction is active.

`TransactionBeginning`, `TransactionCommitting`, `TransactionCommitted`, and `TransactionRolledBack` are raised for the outermost transaction. Changes made by `TransactionBeginning` and `TransactionCommitting` handlers are part of the transaction; an exception from either handler rolls it back.

Transactions cover changes recorded by `History`, including generated properties, manual actions, and observed collections. They do not roll back untracked fields or external effects such as file, database, or network operations.

## Batch

Use `Batch()` to group multiple changes into a single undo action.

```cs
using (history.Batch())
{
    document.X = 100;
    document.Y = 200;
}
```

`Batch()` does not provide rollback. If the body throws, changes made before the exception remain applied and are finalized as one undo action when the scope is disposed. Use `Transaction` instead when failure must restore the previous state.

## CoalescingBatch

Use `CoalescingBatch()` for sliders, color pickers, dragging, 3D gizmos, and other continuous gestures.

```cs
using (history.CoalescingBatch())
{
    document.X = 100;
    document.X = 120;
    document.X = 140;
}
```

Repeated changes to the same target property retain the first old value and final new value. Generated `[Undoable]` properties automatically provide the target and property key.

Like `Batch()`, `CoalescingBatch()` groups history but does not roll changes back when its body throws. Collection changes and explicitly pushed actions remain ordering boundaries and are not coalesced with property changes across those boundaries.

## Pause

Use `Pause()` for initialization that should not be recorded.

```cs
using (history.Pause())
{
    LoadInitialValues();
}
```

`Pause()` is not a rollback mechanism. Changes made while paused remain applied, but `History` has no action with which to undo them.

## Dirty state and save points

`History.IsDirty` reports whether the current history position differs from the last successfully saved position. Call `MarkSaved()` only after saving succeeds:

```csharp
await SaveDocumentAsync(document);
history.MarkSaved();
```

Immediately after `MarkSaved()`, `IsDirty` is false. A subsequent edit makes it true; undoing exactly back to the saved position makes it false, and redoing away from that position makes it true again. Changes recorded inside a batch become dirty when the outermost batch ends; transaction changes become dirty only after the outermost transaction commits. Empty operations and rolled-back transactions do not change the dirty state.

Use `MarkDirty()` for changes that are not represented by undo history, such as an external document-side effect. This explicit dirty state remains true across Undo/Redo until `MarkSaved()` is called. `Clear()` only discards Undo/Redo entries and does not imply that the document was saved. Changes made during `Pause()` are intentionally excluded from both history and automatic dirty tracking.

`History` raises `PropertyChanged` for `IsDirty` only when the value changes. `MarkSaved()` cannot be called while a batch or transaction is active.

## Undoable collection operations

Collections observed through `INotifyCollectionChanged` can participate in undo/redo. Assigning a collection to an `[Undoable]` property automatically attaches collection-change tracking.

```cs
using System.Collections.ObjectModel;
using Jewelry.EditingSystem;
using Jewelry.EditingSystem.Annotations;

[EditingHistory(nameof(_history))]
public sealed partial class CollectionDocument
{
    private readonly History _history;

    public CollectionDocument(History history)
    {
        _history = history;
        using (history.Pause())
            Items = [];
    }

    [Undoable]
    public partial ObservableCollection<string> Items { get; set; }
}

using var history = new History();
var document = new CollectionDocument(history);

document.Items.Add("A");
document.Items.Add("B");

history.Undo(); // ["A"]
history.Redo(); // ["A", "B"]

document.Items.Move(1, 0); // ["B", "A"]
history.Undo();             // ["A", "B"]

document.Items.Clear();
history.Undo(); // ["A", "B"]
```

The main operations and notifications are:

| Operation | Initial notification | Undo notification | Redo notification |
| --- | --- | --- | --- |
| `Add` / `Insert` | `Add` | `Remove` | `Add` |
| `Remove` / `RemoveAt` | `Remove` | `Add` | `Remove` |
| `Move` | `Move` | `Move` | `Move` |
| `Clear` | `Reset` | one `Add` per item | one `Remove` per item |
| `ClearEx(history)` | one `Remove` per item | one `Add` per item | one `Remove` per item |

`ClearEx`, `UnionWithEx`, `IntersectWithEx`, `ExceptWithEx`, `SymmetricExceptWithEx`, and `RemoveWhereEx` can explicitly record changes even when the collection is not currently assigned to an observed property.

```cs
var values = new List<int> { 1, 2, 3 };

values.ClearEx(history);
// values == []

history.Undo();
// values == [1, 2, 3]

history.Redo();
// values == []
```

Set operations can also be recorded as a single undo/redo action.

```cs
using Jewelry.Collections;

var values = new ObservableHashSet<int>([10]);

values.UnionWithEx([20, 30], history);
// values == [10, 20, 30]

history.Undo();
// values == [10]

history.Redo();
// values == [10, 20, 30]
```

## Limiting history

History is unlimited by default. Long-running editors can set a bound.

```cs
history.MaxUndoCount = 500;
```

## Manual mode: low-level optional API

For new code, `[Undoable]` is the recommended default.

The existing `EditableModelBase`, `SetEditableProperty`, `RecordPropertyChange`, and `RecordAppliedPropertyChange` APIs remain available for legacy integration and cases that need complete control over the setter pipeline.

```cs
public sealed class ManualModel : EditableModelBase
{
    private int _value;

    public ManualModel(History history) : base(history)
    {
    }

    public int Value
    {
        get => _value;
        set => SetEditableProperty(v => _value = v, _value, value);
    }
}
```

### Direct mode without `EditableModelBase`

If the model must inherit another base class, implement `INotifyPropertyChanged` and use the `SetEditableProperty` extension with its `History`. The callback must apply the value and raise the usual notification because the same callback is used for the ordinary edit, Undo, and Redo.

```cs
public sealed class ExistingModel : SomeExistingBase, INotifyPropertyChanged
{
    private readonly History _history;
    private int _value;

    public ExistingModel(History history) => _history = history;

    public event PropertyChangedEventHandler? PropertyChanged;

    public int Value
    {
        get => _value;
        set => this.SetEditableProperty(_history, ApplyValue, _value, value);
    }

    private void ApplyValue(int value)
    {
        _value = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
    }
}
```

For a setter that records before applying the value, call `history.RecordPropertyChange(this, nameof(Value), ApplyValue, _value, value)` and apply only when it returns `true`. For an already-applied post-change hook, use `RecordAppliedPropertyChange`. In either case, use a callback that restores the field and notifications for Undo and Redo.

Manual mode remains supported for compatibility, but ordinary properties should prefer `[Undoable]`.

## Source generation and NativeAOT

The generator resolves the following at compile time:

- the configured `History`
- partial-property implementation
- `INotifyPropertyChanged` detection
- notification methods explicitly selected with `[EditingPropertyChanged]`
- inherited notification methods
- notification-method accessibility
- undo/redo setter code

Generated properties are NativeAOT-friendly: their setter and notification code is emitted at compile time. The collection adapter also has a NativeAOT-safe path for arbitrary `ICollection<T>` implementations; see the documentation for its reflection boundary.

## Avalonia demo

[`EditingSystem/Jewelry.EditingSystem.Avalonia.Demo`](EditingSystem/Jewelry.EditingSystem.Avalonia.Demo) contains an Avalonia 12 demo covering CommunityToolkit.Mvvm integration, undo/redo, `Batch`, `CoalescingBatch`, multi-object editing, and undoable Z-order operations.

![demo](site/img/demo00.png)

## Author

Yoshihiro Ito  
Twitter: [https://twitter.com/yoiyoi322](https://twitter.com/yoiyoi322)  
Email: yo.i.jewelry.bab@gmail.com

## License

MIT
