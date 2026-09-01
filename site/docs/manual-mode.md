---
title: Manual mode
---

# Manual mode

> [日本語版](/ja/docs/manual-mode/)

`[Undoable]` is the recommended choice for new properties. `EditableModelBase`, `SetEditableProperty`, `RecordPropertyChange`, and `RecordAppliedPropertyChange` remain available for existing code and setters that need full control.

```csharp
public sealed class ManualDocument : EditableModelBase
{
    private double _x;

    public ManualDocument(History history) : base(history) { }

    public double X
    {
        get => _x;
        set => SetEditableProperty(v => _x = v, _x, value);
    }
}
```

## Direct mode without `EditableModelBase`

When a model already has a base class, it can still participate in history. Implement `INotifyPropertyChanged` and call the `SetEditableProperty` extension with the `History` instance. The callback is used for the regular edit, Undo, and Redo, so it is the right place to update the field and raise the normal notification.

```csharp
using System.ComponentModel;

public sealed class ExistingDocument : SomeExistingBase, INotifyPropertyChanged
{
    private readonly History _history;
    private double _x;

    public ExistingDocument(History history) => _history = history;

    public event PropertyChangedEventHandler? PropertyChanged;

    public double X
    {
        get => _x;
        set => this.SetEditableProperty(_history, ApplyX, _x, value);
    }

    private void ApplyX(double value)
    {
        _x = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(X)));
    }
}
```

`SetEditableProperty` returns `false` when the value has not changed. If the setter has additional work that should run only for an actual edit, perform it only when the method returns `true`.

For a setter pipeline that has to record before applying the value, use `history.RecordPropertyChange(this, nameof(X), ApplyX, _x, value)` and call `ApplyX(value)` only when it returns `true`. For a post-change hook where the value has already been applied, use `RecordAppliedPropertyChange` instead. In both cases, the callback must restore the same field and notifications during Undo and Redo.

Use manual mode when migrating a legacy setter or when the setter pipeline cannot be expressed as a partial property. Prefer the generated model otherwise, so the ordinary edit and undo/redo paths remain consistent.
