---
title: Dirty state and save points
---

# Dirty state and save points

> [日本語版](/ja/docs/dirty-state/)

`History.IsDirty` tells you whether the current history position differs from the last successfully saved position. Call `MarkSaved()` only after the save operation has completed successfully.

```csharp
await SaveDocumentAsync(document);
history.MarkSaved();
```

Immediately after `MarkSaved()`, `IsDirty` is `false`. A later edit makes it `true`; undoing exactly to the saved position makes it `false` again; redo makes it `true`. A batch becomes dirty when its outermost scope ends, while a transaction becomes dirty only after its outermost commit.

Use `MarkDirty()` for a change that has no undo action, such as an external document-side effect. This explicit dirty state stays set through Undo and Redo until `MarkSaved()` is called. `Clear()` removes undo/redo entries but does not mark the document as saved. Changes made inside `Pause()` are intentionally excluded from both history and automatic dirty tracking.

`IsDirty` raises `PropertyChanged` only when its value changes. `MarkSaved()` is not valid while a batch or transaction is active.
