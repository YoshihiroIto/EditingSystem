using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Jewelry.EditingSystem;

public static class SetExtensions
{
    public static void UnionWithEx<T>(this ISet<T> self, IEnumerable<T> other, History history)
    {
        Validate(self, other, history);

        ExecuteDelta(self, history, (_, addedItems) =>
        {
            foreach (var item in other)
            {
                if (self.Add(item))
                    addedItems.Add(item);
            }
        });
    }

    public static void IntersectWithEx<T>(this ISet<T> self, IEnumerable<T> other, History history)
    {
        Validate(self, other, history);

        if (!TryCreateLookupSet(self, other, out var otherSet))
        {
            ExecuteAsSingleAction(self, history, () => self.IntersectWith(other));
            return;
        }

        var removedItems = new List<T>();
        foreach (var item in self)
        {
            if (!otherSet.Contains(item))
                removedItems.Add(item);
        }

        ExecuteDelta(self, history, (removed, _) =>
        {
            foreach (var item in removedItems)
            {
                if (!self.Remove(item))
                    throw new InvalidOperationException("The item to remove was not found in the set.");
                removed.Add(item);
            }
        });
    }

    public static void ExceptWithEx<T>(this ISet<T> self, IEnumerable<T> other, History history)
    {
        Validate(self, other, history);

        if (!TryCreateLookupSet(self, other, out var otherSet))
        {
            ExecuteAsSingleAction(self, history, () => self.ExceptWith(other));
            return;
        }

        ExecuteDelta(self, history, (removedItems, _) =>
        {
            foreach (var candidate in otherSet)
            {
                if (!TryGetActualItem(self, candidate, out var actualItem))
                    continue;

                if (!self.Remove(candidate))
                    throw new InvalidOperationException("The item to remove was not found in the set.");
                removedItems.Add(actualItem);
            }
        });
    }

    public static void SymmetricExceptWithEx<T>(this ISet<T> self, IEnumerable<T> other, History history)
    {
        Validate(self, other, history);

        if (!TryCreateLookupSet(self, other, out var otherSet))
        {
            ExecuteAsSingleAction(self, history, () => self.SymmetricExceptWith(other));
            return;
        }

        ExecuteDelta(self, history, (removedItems, addedItems) =>
        {
            foreach (var candidate in otherSet)
            {
                if (TryGetActualItem(self, candidate, out var actualItem))
                {
                    if (!self.Remove(candidate))
                        throw new InvalidOperationException("The item to remove was not found in the set.");
                    removedItems.Add(actualItem);
                }
                else if (self.Add(candidate))
                    addedItems.Add(candidate);
            }
        });
    }

    public static int RemoveWhereEx<T>(this ISet<T> self, Predicate<T> match, History history)
    {
        if (self is null)
            throw new ArgumentNullException(nameof(self));
        if (match is null)
            throw new ArgumentNullException(nameof(match));
        if (history is null)
            throw new ArgumentNullException(nameof(history));

        var itemsToRemove = new List<T>();
        foreach (var item in self)
        {
            if (match(item))
                itemsToRemove.Add(item);
        }

        ExecuteDelta(self, history, (removedItems, _) =>
        {
            foreach (var item in itemsToRemove)
            {
                if (!self.Remove(item))
                    throw new InvalidOperationException("The item to remove was not found in the set.");

                removedItems.Add(item);
            }
        });

        return itemsToRemove.Count;
    }

    private static void Validate<T>(ISet<T> self, IEnumerable<T> other, History history)
    {
        if (self is null)
            throw new ArgumentNullException(nameof(self));
        if (other is null)
            throw new ArgumentNullException(nameof(other));
        if (history is null)
            throw new ArgumentNullException(nameof(history));
    }

    private static void ExecuteDelta<T>(
        ISet<T> self,
        History history,
        Action<List<T>, List<T>> action)
    {
        var removedItems = new List<T>();
        var addedItems = new List<T>();

        using (history.Pause())
        {
            try
            {
                action(removedItems, addedItems);
            }
            catch (Exception applyException)
            {
                RollBack(() => RollBackDelta(self, removedItems, addedItems), applyException);
                throw;
            }
        }

        if (removedItems.Count is 0 && addedItems.Count is 0)
            return;

        var state = new DeltaHistoryState<T>(self, [.. removedItems], [.. addedItems]);
        history.Push(state.Undo, state.Redo);
    }

    private static void RollBackDelta<T>(ISet<T> self, List<T> removedItems, List<T> addedItems)
    {
        for (var i = addedItems.Count - 1; i >= 0; --i)
        {
            if (!self.Remove(addedItems[i]))
                throw new InvalidOperationException("The item added by the failed set operation could not be removed during rollback.");
        }

        for (var i = removedItems.Count - 1; i >= 0; --i)
        {
            if (!self.Add(removedItems[i]))
                throw new InvalidOperationException("The item removed by the failed set operation could not be restored during rollback.");
        }
    }

    private static bool TryCreateLookupSet<T>(ISet<T> self, IEnumerable<T> other, out ISet<T> lookup)
    {
#if NET8_0_OR_GREATER
        switch (self)
        {
            case HashSet<T> hashSet:
                lookup = new HashSet<T>(other, hashSet.Comparer);
                return true;
            case SortedSet<T> sortedSet:
                lookup = new SortedSet<T>(other, sortedSet.Comparer);
                return true;
        }
#endif
        lookup = null!;
        return false;
    }

    private static bool TryGetActualItem<T>(ISet<T> self, T equalValue, out T actualValue)
    {
#if NET8_0_OR_GREATER
        switch (self)
        {
            case HashSet<T> hashSet:
                return hashSet.TryGetValue(equalValue, out actualValue!);
            case SortedSet<T> sortedSet:
                return sortedSet.TryGetValue(equalValue, out actualValue!);
        }
#endif
        actualValue = default!;
        return false;
    }

    private static void ExecuteAsSingleAction<T>(ISet<T> self, History history, Action action)
    {
        var oldItems = new List<T>(self);
        using var changeRecorder = SetChangeRecorder<T>.TryCreate(self);

        using (history.Pause())
        {
            try
            {
                action();
            }
            catch (Exception applyException)
            {
                changeRecorder?.Stop();
                RollBack(() => RestoreSnapshot(self, oldItems, notifyItems: false), applyException);
                throw;
            }
        }

        changeRecorder?.Stop();

        if (self.SetEquals(oldItems))
            return;

        if (changeRecorder is { } &&
            TryCreateRecordedDelta(self, oldItems, changeRecorder, out var recordedRemoved, out var recordedAdded))
        {
            var state = new DeltaHistoryState<T>(self, recordedRemoved, recordedAdded);
            history.Push(state.Undo, state.Redo);
            return;
        }

        if (TryCreateDelta(self, oldItems, out var removedItems, out var addedItems))
        {
            var state = new DeltaHistoryState<T>(self, removedItems, addedItems);
            history.Push(state.Undo, state.Redo);
            return;
        }

        var newItems = new List<T>(self).ToArray();
        var snapshotState = new SnapshotHistoryState<T>(self, [.. oldItems], newItems);
        history.Push(snapshotState.Undo, snapshotState.Redo);
    }

    private static bool TryCreateRecordedDelta<T>(
        ISet<T> self,
        IReadOnlyList<T> oldItems,
        SetChangeRecorder<T> recorder,
        out T[] removedItems,
        out T[] addedItems)
    {
        if (!recorder.CanCreateDelta || !recorder.HasChanges)
        {
            removedItems = [];
            addedItems = [];
            return false;
        }

        if (recorder.HasRemovals)
        {
            var removed = new List<T>();
            foreach (var item in oldItems)
            {
                if (!self.Contains(item))
                    removed.Add(item);
            }

            removedItems = [.. removed];
        }
        else
            removedItems = [];

        var added = new List<T>();
        foreach (var item in recorder.AddedItems)
        {
            if (self.Contains(item))
                added.Add(item);
        }

        addedItems = [.. added];
        return removedItems.Length > 0 || addedItems.Length > 0;
    }

    private static bool TryCreateDelta<T>(
        ISet<T> self,
        IReadOnlyList<T> oldItems,
        out T[] removedItems,
        out T[] addedItems)
    {
        ISet<T>? oldSet = self switch
        {
            HashSet<T> hashSet => new HashSet<T>(oldItems, hashSet.Comparer),
            SortedSet<T> sortedSet => new SortedSet<T>(oldItems, sortedSet.Comparer),
            _ => null
        };

        if (oldSet is null)
        {
            removedItems = [];
            addedItems = [];
            return false;
        }

        var removed = new List<T>();
        foreach (var item in oldItems)
        {
            if (!self.Contains(item))
                removed.Add(item);
        }

        var added = new List<T>();
        foreach (var item in self)
        {
            if (!oldSet.Contains(item))
                added.Add(item);
        }

        removedItems = [.. removed];
        addedItems = [.. added];
        return true;
    }

    private static void ApplyDelta<T>(
        ISet<T> self,
        IReadOnlyList<T> removedItems,
        IReadOnlyList<T> addedItems)
    {
        foreach (var item in removedItems)
        {
            if (!self.Remove(item))
                throw new InvalidOperationException("The item to remove while replaying a set history action was not found.");
        }

        foreach (var item in addedItems)
        {
            if (!self.Add(item))
                throw new InvalidOperationException("The item to add while replaying a set history action already exists.");
        }

        NotifyItems(removedItems, CollectionItemChangedInfo.Remove);
        NotifyItems(addedItems, CollectionItemChangedInfo.Add);
    }

    private static void RestoreSnapshot<T>(ISet<T> self, IReadOnlyList<T> items, bool notifyItems)
    {
        List<T>? removedItems = notifyItems ? new List<T>(self) : null;
        var currentItems = new List<T>(self);
        foreach (var item in currentItems)
            self.Remove(item);
        foreach (var item in items)
            self.Add(item);

        if (removedItems is { })
        {
            NotifyItems(removedItems, CollectionItemChangedInfo.Remove);
            NotifyItems(items, CollectionItemChangedInfo.Add);
        }
    }

    private static void NotifyItems<T>(IEnumerable<T> items, in CollectionItemChangedInfo info)
    {
        foreach (var item in items)
        {
            if (item is ICollectionItem collectionItem)
                collectionItem.Changed(info);
        }
    }

    private static void RollBack(Action rollback, Exception applyException)
    {
        try
        {
            rollback();
        }
        catch (Exception rollbackException)
        {
            throw new AggregateException(applyException, rollbackException);
        }
    }

    private sealed class SetChangeRecorder<T> : IDisposable
    {
        public static SetChangeRecorder<T>? TryCreate(ISet<T> self)
        {
            return self is INotifyCollectionChanged source ? new SetChangeRecorder<T>(source) : null;
        }

        private SetChangeRecorder(INotifyCollectionChanged source)
        {
            _source = source;
            _source.CollectionChanged += OnCollectionChanged;
        }

        public bool CanCreateDelta { get; private set; } = true;
        public bool HasChanges { get; private set; }
        public bool HasRemovals { get; private set; }
        public IReadOnlyList<T> AddedItems => _addedItems;

        public void Stop()
        {
            if (_isStopped)
                return;

            _source.CollectionChanged -= OnCollectionChanged;
            _isStopped = true;
        }

        public void Dispose()
        {
            Stop();
        }

        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (!CanCreateDelta)
                return;

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    HasChanges = true;
                    AppendAddedItems(e.NewItems);
                    break;

                case NotifyCollectionChangedAction.Remove:
                    HasChanges = true;
                    HasRemovals = true;
                    if (e.OldItems is null)
                        DisableDelta();
                    break;

                default:
                    DisableDelta();
                    break;
            }
        }

        private void AppendAddedItems(IList? items)
        {
            if (items is null)
            {
                DisableDelta();
                return;
            }

            try
            {
                foreach (var t in items)
                    _addedItems.Add((T)t!);
            }
            catch (InvalidCastException)
            {
                DisableDelta();
            }
            catch (NullReferenceException)
            {
                DisableDelta();
            }
        }

        private void DisableDelta()
        {
            CanCreateDelta = false;
            _addedItems.Clear();
        }

        private readonly INotifyCollectionChanged _source;
        private readonly List<T> _addedItems = new();
        private bool _isStopped;
    }

    private sealed class DeltaHistoryState<T>(ISet<T> self, T[] removedItems, T[] addedItems)
    {
        public void Undo()
        {
            ApplyDelta(self, addedItems, removedItems);
        }

        public void Redo()
        {
            ApplyDelta(self, removedItems, addedItems);
        }
    }

    private sealed class SnapshotHistoryState<T>(ISet<T> self, T[] oldItems, T[] newItems)
    {
        public void Undo()
        {
            RestoreSnapshot(self, oldItems, notifyItems: true);
        }

        public void Redo()
        {
            RestoreSnapshot(self, newItems, notifyItems: true);
        }
    }
}
