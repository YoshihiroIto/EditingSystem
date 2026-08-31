---
title: Recording scopes
---

# Recording scopes

> [日本語版](/ja/docs/recording-scopes/)

All scopes apply changes immediately. They differ in how entries are recorded and what happens when the scope ends.

| Scope | Use it for | Result |
|---|---|---|
| `Transaction()` | An operation that must commit or roll back as a whole | A committed transaction is one undo action; an uncommitted one restores tracked changes. |
| `Batch()` | Several edits that belong to one command | The edits remain applied and become one undo action. |
| `CoalescingBatch()` | Continuous values such as sliders, drags, and color pickers | Repeated writes to a property retain the first old value and final new value. |
| `Pause()` | Loading or synchronization | Changes remain applied but are not entered into history. |

```csharp
using (history.CoalescingBatch())
{
    document.X = 100;
    document.X = 120;
    document.X = 140;
}

history.Undo(); // Restores the value before the gesture.
```

Use `Transaction()` when failure must restore tracked state. Use `Batch()` when grouping is sufficient, and `CoalescingBatch()` when intermediate values are not useful to the user.

## Transactions

Start a transaction for an operation that must either commit all tracked changes as one undo action or restore them all. A transaction is rolled back automatically if it leaves its `using` scope without `Commit()`.

```csharp
using var transaction = history.BeginTransaction();
document.X = 100;
document.Y = 200;
ValidateDocument();
transaction.Commit();
```

Transactions can be nested. A committed inner transaction merges into its parent; rolling an inner transaction back restores only its own changes. The outer transaction also rolls back changes from committed inner transactions. `Batch()` and `CoalescingBatch()` can run inside a transaction, but a transaction cannot begin inside a batch or pause, and recording cannot be paused while a transaction is active.

`TransactionBeginning`, `TransactionCommitting`, `TransactionCommitted`, and `TransactionRolledBack` are raised for the outermost transaction. Changes made by the first two event handlers participate in the transaction; an exception rolls it back. Transactions cover changes recorded by `History`, not untracked fields or external file, database, and network effects.

## Batches and pauses

`Batch()` does not roll back when its body throws: the changes remain applied and are finalized as one undo action on disposal. Use a transaction when failure must restore state.

`CoalescingBatch()` has the same non-rollback behavior. Collection changes and explicitly pushed actions are ordering boundaries, so property changes never coalesce across them.

`Pause()` is for initialization or synchronization. It is not a rollback mechanism: changes remain applied, but `History` has no action with which to undo them.
