---
title: Collections and notifications
---

# Collections and notifications

> [日本語版](/ja/docs/collections-and-notifications/)

Assigning an `INotifyCollectionChanged` collection to an `[Undoable]` property attaches collection-change tracking. Adds, removals, moves, and clears then take part in the same history as property edits.

```csharp
using System.Collections.ObjectModel;

[EditingHistory(nameof(history))]
public sealed partial class Document(History history)
{
    [Undoable]
    public partial ObservableCollection<string> Items { get; set; } = [];
}

document.Items.Add("A");
history.Undo(); // Items is empty again.
```

`INotifyPropertyChanged` is optional. When a target implements it, the generator resolves a supported notification method at compile time and raises notifications for ordinary changes, undo, and redo. Use `[EditingPropertyChanged]` when the notification method has a custom name.

## Reflection scope

EditingSystem remains NativeAOT-friendly for collection replay. For an arbitrary collection that implements `ICollection<T>` but not non-generic `IList`, its adapter deliberately inspects the interface and invokes `Add`/`Remove` through reflection; the NativeAOT path preserves the required metadata. `ObservableCollection<T>` follows the `IList` path.

## Operations and notifications

| Operation | Initial notification | Undo notification | Redo notification |
|---|---|---|---|
| `Add` / `Insert` | `Add` | `Remove` | `Add` |
| `Remove` / `RemoveAt` | `Remove` | `Add` | `Remove` |
| `Move` | `Move` | `Move` | `Move` |
| `Clear` | `Reset` | one `Add` per item | one `Remove` per item |
| `ClearEx(history)` | one `Remove` per item | one `Add` per item | one `Remove` per item |

`ClearEx`, `UnionWithEx`, `IntersectWithEx`, `ExceptWithEx`, `SymmetricExceptWithEx`, and `RemoveWhereEx` explicitly record changes even when a collection is not assigned to an observed property.

```csharp
var values = new List<int> { 1, 2, 3 };
values.ClearEx(history);
history.Undo(); // [1, 2, 3]
```

Set operations can also become one undo/redo action:

```csharp
using Jewelry.Collections;

var values = new ObservableHashSet<int>([10]);
values.UnionWithEx([20, 30], history);
history.Undo(); // [10]
history.Redo(); // [10, 20, 30]
```
