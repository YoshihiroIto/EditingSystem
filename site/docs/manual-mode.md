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

Use manual mode when migrating a legacy setter or when the setter pipeline cannot be expressed as a partial property. Prefer the generated model otherwise, so the ordinary edit and undo/redo paths remain consistent.

## Limiting history

History is unlimited by default. For a long-running editor, set an upper bound:

```csharp
history.MaxUndoCount = 500;
```

## Source generation and NativeAOT

The generator resolves the configured `History`, partial-property implementation, `INotifyPropertyChanged` support, notification method accessibility, and undo/redo setter code at compile time. No runtime reflection or dynamic code generation is required, so the default pattern is suitable for NativeAOT applications.
