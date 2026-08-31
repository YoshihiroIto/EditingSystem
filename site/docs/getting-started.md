---
title: Getting started
---

# Getting started

> [日本語版](/ja/docs/getting-started/)

Install the core package, give the model a `History`, and mark each generated partial property that should participate in undo/redo.

```shell
dotnet add package Jewelry.EditingSystem
```

```csharp
using Jewelry.EditingSystem;
using Jewelry.EditingSystem.Annotations;

[EditingHistory(nameof(history))]
public sealed partial class Document(History history)
{
    [Undoable]
    public partial string? Name { get; set; }

    [Undoable]
    public partial double X { get; set; }
}

using var history = new History();
var document = new Document(history);

document.X = 100;
document.X = 200;
history.Undo(); // 100
history.Redo(); // 200
```

`EditingHistory` can name a non-null `History` field, property, or primary-constructor parameter. The generator writes the setter implementation and records the normal assignment path, so no reflection or dynamic code is needed at runtime.

Next, choose a [recording scope](recording-scopes.md) for grouped edits.

## `INotifyPropertyChanged`

Notifications are optional. When the target type implements `INotifyPropertyChanged`, the generator resolves a notification path at compile time and preserves it for ordinary changes, undo, and redo.

```csharp
using System.ComponentModel;

[EditingHistory(nameof(history))]
public sealed partial class Document(History history) : INotifyPropertyChanged
{
    [Undoable]
    public partial string? Name { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
```

If the method has another name, select it explicitly with `[EditingPropertyChanged]`. The method must be an accessible instance `void` method with one `string` or `PropertyChangedEventArgs` parameter. An invalid explicit method produces `JES006`.

Without an explicit attribute, the generator tries `RaisePropertyChanged`, `OnPropertyChanged`, and `NotifyPropertyChanged`, first with a `string` parameter and then with `PropertyChangedEventArgs`; it also supports a `PropertyChanged` event declared by the target partial class. Protected methods inherited from a base class are supported. If no path is found, undo/redo still works and the generator reports warning `JES005`.
