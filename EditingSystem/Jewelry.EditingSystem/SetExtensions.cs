using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Jewelry.EditingSystem;

public static class SetExtensions
{
    public static void UnionWithEx<T>(this ISet<T> self, IEnumerable<T> other, History history)
    {
        if (other is null)
            throw new ArgumentNullException(nameof(other));
        ExecuteAsSingleAction(self, history, () => self.UnionWith(other));
    }

    public static void IntersectWithEx<T>(this ISet<T> self, IEnumerable<T> other, History history)
    {
        if (other is null)
            throw new ArgumentNullException(nameof(other));
        ExecuteAsSingleAction(self, history, () => self.IntersectWith(other));
    }

    public static void ExceptWithEx<T>(this ISet<T> self, IEnumerable<T> other, History history)
    {
        if (other is null)
            throw new ArgumentNullException(nameof(other));
        ExecuteAsSingleAction(self, history, () => self.ExceptWith(other));
    }

    public static void SymmetricExceptWithEx<T>(this ISet<T> self, IEnumerable<T> other, History history)
    {
        if (other is null)
            throw new ArgumentNullException(nameof(other));
        ExecuteAsSingleAction(self, history, () => self.SymmetricExceptWith(other));
    }

    public static int RemoveWhereEx<T>(this ISet<T> self, Predicate<T> match, History history)
    {
        if (match is null)
            throw new ArgumentNullException(nameof(match));

        var removedCount = 0;
        var items = new List<T>(self ?? throw new ArgumentNullException(nameof(self)));
        ExecuteAsSingleAction(self, history, () =>
        {
            foreach (var item in items)
            {
                if (match(item) && self.Remove(item))
                    ++removedCount;
            }
        });

        return removedCount;
    }

    private static void ExecuteAsSingleAction<T>(ISet<T> self, History history, Action action)
    {
        if (self is null)
            throw new ArgumentNullException(nameof(self));
        if (history is null)
            throw new ArgumentNullException(nameof(history));

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

        if (changeRecorder is not null &&
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
        var snapshotState = new SnapshotHistoryState<T>(self, oldItems.ToArray(), newItems);
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

            removedItems = removed.ToArray();
        }
        else
            removedItems = [];

        var added = new List<T>();
        foreach (var item in recorder.AddedItems)
        {
            if (self.Contains(item))
                added.Add(item);
        }

        addedItems = added.ToArray();
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

        removedItems = removed.ToArray();
        addedItems = added.ToArray();
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

        if (removedItems is not null)
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
                for (var i = 0; i < items.Count; ++i)
                    _addedItems.Add((T)items[i]!);
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

    private sealed class DeltaHistoryState<T>
    {
        public DeltaHistoryState(ISet<T> self, T[] removedItems, T[] addedItems)
        {
            _self = self;
            _removedItems = removedItems;
            _addedItems = addedItems;
        }

        public void Undo()
        {
            ApplyDelta(_self, _addedItems, _removedItems);
        }

        public void Redo()
        {
            ApplyDelta(_self, _removedItems, _addedItems);
        }

        private readonly ISet<T> _self;
        private readonly T[] _removedItems;
        private readonly T[] _addedItems;
    }

    private sealed class SnapshotHistoryState<T>
    {
        public SnapshotHistoryState(ISet<T> self, T[] oldItems, T[] newItems)
        {
            _self = self;
            _oldItems = oldItems;
            _newItems = newItems;
        }

        public void Undo()
        {
            RestoreSnapshot(_self, _oldItems, notifyItems: true);
        }

        public void Redo()
        {
            RestoreSnapshot(_self, _newItems, notifyItems: true);
        }

        private readonly ISet<T> _self;
        private readonly T[] _oldItems;
        private readonly T[] _newItems;
    }
}
