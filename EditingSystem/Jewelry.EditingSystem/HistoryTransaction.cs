using System;

namespace Jewelry.EditingSystem;

/// <summary>
/// Represents a set of recorded model changes that can be committed as one undo action or rolled
/// back before it reaches the history stacks.
/// </summary>
public sealed class HistoryTransaction : IDisposable
{
    internal HistoryTransaction(History history)
    {
        _history = history;
    }

    /// <summary>
    /// Commits the transaction. Nested transactions are merged into their parent transaction.
    /// </summary>
    public void Commit()
    {
        GetActiveHistory().CommitTransaction(this);
    }

    /// <summary>
    /// Rolls back the changes recorded by this transaction.
    /// </summary>
    public void Rollback()
    {
        GetActiveHistory().RollbackTransaction(this);
    }

    /// <summary>
    /// Rolls back the transaction if it has not already been committed or rolled back.
    /// </summary>
    public void Dispose()
    {
        _history?.RollbackTransaction(this);
    }

    internal void Complete()
    {
        _history = null;
    }

    private History GetActiveHistory()
    {
        return _history ?? throw new InvalidOperationException("The transaction has already completed.");
    }

    private History? _history;
}
