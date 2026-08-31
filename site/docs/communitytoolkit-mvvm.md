---
title: CommunityToolkit.Mvvm
---

# CommunityToolkit.Mvvm integration

> [日本語版](/ja/docs/communitytoolkit-mvvm/)

Install the integration package when your model uses CommunityToolkit.Mvvm's `[ObservableProperty]` generator.

```shell
dotnet add package Jewelry.EditingSystem.CommunityToolkit.Mvvm
```

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using Jewelry.EditingSystem;
using Jewelry.EditingSystem.CommunityToolkit.Mvvm;

[EditingHistory(nameof(_history))]
public sealed partial class DocumentViewModel : ObservableObject
{
    private readonly History _history;

    public DocumentViewModel(History history) => _history = history;

    [Undoable, ObservableProperty]
    private string? name;
}
```

Undo and redo reuse the CommunityToolkit-generated setter pipeline, preserving property notifications, validation, command notifications, and recipient broadcasts. `ObservableObject` is optional: `[INotifyPropertyChanged]` can be used on a type that already has another base class.

The integration reserves the one-parameter `OnXChanging(T value)` and `OnXChanged(T value)` hooks. Use the two-parameter hooks for application-specific behavior. Attributes in `Jewelry.EditingSystem.Annotations` apply to standalone partial properties; attributes in `Jewelry.EditingSystem.CommunityToolkit.Mvvm` apply to `[ObservableProperty]` integration. Existing CommunityToolkit.Mvvm code does not need to change.
