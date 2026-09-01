---
title: Limiting history
---

# Limiting history

> [日本語版](/ja/docs/history-limits/)

History is unlimited by default. For a long-running editor, set a maximum number of undo entries to bound memory use:

```csharp
history.MaxUndoCount = 500;
```

Choose the limit according to the size and frequency of your application's edits. The limit applies to recorded undo entries; it does not change the model's current state.
