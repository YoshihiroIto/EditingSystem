using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;

namespace Jewelry.EditingSystem;

internal sealed class CollectionChangedWeakEventManager : IDisposable
{
    private ConditionalWeakTable<INotifyCollectionChanged, Registration> _lookup = new();
    private readonly List<WeakReference<Registration>> _registrations = new();
    private int _staleRegistrationCount;

    public void AddWeakEventListener(INotifyCollectionChanged source, NotifyCollectionChangedEventHandler handler)
    {
        if (_lookup.TryGetValue(source, out var registration))
        {
            ++registration.ReferenceCount;
            return;
        }

        registration = new Registration(source, handler);
        _lookup.Add(source, registration);
        _registrations.Add(new WeakReference<Registration>(registration));
    }

    public void RemoveWeakEventListener(INotifyCollectionChanged source)
    {
        if (_lookup.TryGetValue(source, out var registration) is false)
            return;

        if (registration.ReferenceCount > 1)
        {
            --registration.ReferenceCount;
            return;
        }

        _lookup.Remove(source);
        registration.Dispose();
        ++_staleRegistrationCount;
        CompactRegistrationsIfNeeded();
    }

    public IReadOnlyList<object?> GetSnapshot(INotifyCollectionChanged source)
    {
        if (_lookup.TryGetValue(source, out var registration))
            return registration.Listener.Snapshot;

        throw new InvalidOperationException("The collection is not registered.");
    }

    public void Dispose()
    {
        foreach (var weakRegistration in _registrations)
        {
            if (weakRegistration.TryGetTarget(out var registration))
                registration.Dispose();
        }

        _registrations.Clear();
        _lookup = new ConditionalWeakTable<INotifyCollectionChanged, Registration>();
        _staleRegistrationCount = 0;
    }

    private void CompactRegistrationsIfNeeded()
    {
        const int minStaleRegistrationCount = 32;
        if (_staleRegistrationCount < minStaleRegistrationCount ||
            _staleRegistrationCount * 2 < _registrations.Count)
            return;

        var writeIndex = 0;
        for (var readIndex = 0; readIndex < _registrations.Count; ++readIndex)
        {
            var weakRegistration = _registrations[readIndex];
            if (weakRegistration.TryGetTarget(out var registration) is false || registration.IsActive is false)
                continue;

            _registrations[writeIndex++] = weakRegistration;
        }

        if (writeIndex < _registrations.Count)
            _registrations.RemoveRange(writeIndex, _registrations.Count - writeIndex);

        _staleRegistrationCount = 0;
    }

    private sealed class Registration(INotifyCollectionChanged source, NotifyCollectionChangedEventHandler handler) : IDisposable
    {
        public CollectionChangedWeakEventListener Listener { get; } = new(source, handler);

        // Keep the weak listener's delegate alive for as long as this registration is active.
        public NotifyCollectionChangedEventHandler Handler { get; } = handler;
        public int ReferenceCount { get; set; } = 1;
        public bool IsActive { get; private set; } = true;

        public void Dispose()
        {
            if (IsActive is false)
                return;

            IsActive = false;
            Listener.Dispose();
        }
    }

    private sealed class CollectionChangedWeakEventListener : IDisposable
    {
        public bool IsAlive => _handler.TryGetTarget(out _) && _source.TryGetTarget(out _);
        public object? Source => _source.TryGetTarget(out var source) ? source : default;

        public IReadOnlyList<object?> Snapshot => _resetSnapshots.Count > 0
            ? _resetSnapshots.Peek()
            : _snapshot;

        private readonly WeakReference<INotifyCollectionChanged> _source;
        private readonly WeakReference<NotifyCollectionChangedEventHandler> _handler;
        private readonly Stack<List<object?>> _resetSnapshots = new();
        private List<object?> _snapshot;

        public CollectionChangedWeakEventListener(INotifyCollectionChanged source, NotifyCollectionChangedEventHandler handler)
        {
            _source = new WeakReference<INotifyCollectionChanged>(source);
            _handler = new WeakReference<NotifyCollectionChangedEventHandler>(handler);
            _snapshot = CreateSnapshot(source);

            source.CollectionChanged += HandleEvent;
        }

        private void HandleEvent(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (_source.TryGetTarget(out var source) is false)
                return;

            var forwardedEventArgs = e;
            if (e.Action is NotifyCollectionChangedAction.Reset)
            {
                _resetSnapshots.Push(_snapshot);
                _snapshot = CreateSnapshot(source);
            }
            else
            {
                try
                {
                    forwardedEventArgs = ApplyChange(source, e);
                }
                catch
                {
                    _snapshot = CreateSnapshot(source);
                }
            }

            try
            {
                if (_handler.TryGetTarget(out var handler))
                    handler(sender, forwardedEventArgs);
                else
                    Dispose();
            }
            finally
            {
                if (e.Action is NotifyCollectionChangedAction.Reset)
                    _resetSnapshots.Pop();
            }
        }

        public void Dispose()
        {
            if (_source.TryGetTarget(out var source))
                source.CollectionChanged -= HandleEvent;
        }

        private static List<object?> CreateSnapshot(INotifyCollectionChanged source)
        {
            if (source is not IEnumerable enumerable)
                throw new NotSupportedException(
                    $"Collection type '{source.GetType()}' must implement IEnumerable.");

            var snapshot = source is ICollection collection
                ? new List<object?>(collection.Count)
                : new List<object?>();
            foreach (var item in enumerable)
                snapshot.Add(item);

            return snapshot;
        }

        private NotifyCollectionChangedEventArgs ApplyChange(
            INotifyCollectionChanged source,
            NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    return ApplyAdd(source, e);

                case NotifyCollectionChangedAction.Remove:
                    return ApplyRemove(source, e);

                case NotifyCollectionChangedAction.Move:
                    {
                        var count = (e.OldItems ?? throw new InvalidOperationException()).Count;
                        if (count is 1)
                        {
                            var item = _snapshot[e.OldStartingIndex];
                            _snapshot.RemoveAt(e.OldStartingIndex);
                            _snapshot.Insert(e.NewStartingIndex, item);
                            return e;
                        }

                        var items = _snapshot.GetRange(e.OldStartingIndex, count);
                        _snapshot.RemoveRange(e.OldStartingIndex, count);
                        _snapshot.InsertRange(e.NewStartingIndex, items);
                        return e;
                    }

                case NotifyCollectionChangedAction.Replace:
                    return ApplyReplace(source, e);

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private NotifyCollectionChangedEventArgs ApplyAdd(
            INotifyCollectionChanged source,
            NotifyCollectionChangedEventArgs e)
        {
            var newItems = e.NewItems ?? throw new InvalidOperationException();
            if (e.NewStartingIndex >= 0 || source is not IList)
            {
                InsertItems(_snapshot, newItems, e.NewStartingIndex);
                return e;
            }

            var newSnapshot = CreateSnapshot(source);
            if (TryFindInsertionIndex(_snapshot, newSnapshot, newItems.Count, out var index))
            {
                var actualNewItems = newSnapshot.GetRange(index, newItems.Count);
                _snapshot = newSnapshot;
                return new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Add,
                    actualNewItems,
                    index);
            }

            _snapshot = newSnapshot;
            return e;
        }

        private NotifyCollectionChangedEventArgs ApplyRemove(
            INotifyCollectionChanged source,
            NotifyCollectionChangedEventArgs e)
        {
            var oldItems = e.OldItems ?? throw new InvalidOperationException();
            if (e.OldStartingIndex >= 0)
            {
                RemoveItemsAt(_snapshot, oldItems.Count, e.OldStartingIndex);
                return e;
            }

            if (source is IList)
            {
                var listSnapshot = CreateSnapshot(source);
                if (TryFindRemovalIndex(_snapshot, listSnapshot, oldItems.Count, out var index))
                {
                    var actualOldItems = _snapshot.GetRange(index, oldItems.Count);
                    _snapshot = listSnapshot;
                    return new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Remove,
                        actualOldItems,
                        index);
                }
            }

            if (TryRemoveItemsByExactIdentity(_snapshot, oldItems))
                return e;

            // Some unordered collections report the comparer-equal value passed to Remove rather
            // than the actual stored value. Rebuild only on that uncommon mismatch and forward the
            // actual removed instances to History.
            var newSnapshot = CreateSnapshot(source);
            var actualRemovedItems = FindExactDifference(_snapshot, newSnapshot);
            _snapshot = newSnapshot;

            return actualRemovedItems.Count == oldItems.Count
                ? CreateRemoveEventArgs(actualRemovedItems)
                : e;
        }

        private static bool TryFindInsertionIndex(
            IReadOnlyList<object?> oldItems,
            IReadOnlyList<object?> newItems,
            int addedCount,
            out int index)
        {
            if (addedCount <= 0 || newItems.Count != oldItems.Count + addedCount)
            {
                index = -1;
                return false;
            }

            index = 0;
            while (index < oldItems.Count && ItemsExactlyEqual(oldItems[index], newItems[index]))
                ++index;

            for (var oldIndex = index; oldIndex < oldItems.Count; ++oldIndex)
            {
                if (!ItemsExactlyEqual(oldItems[oldIndex], newItems[oldIndex + addedCount]))
                {
                    index = -1;
                    return false;
                }
            }

            return true;
        }

        private static bool TryFindRemovalIndex(
            IReadOnlyList<object?> oldItems,
            IReadOnlyList<object?> newItems,
            int removedCount,
            out int index)
        {
            if (removedCount <= 0 || oldItems.Count != newItems.Count + removedCount)
            {
                index = -1;
                return false;
            }

            index = 0;
            while (index < newItems.Count && ItemsExactlyEqual(oldItems[index], newItems[index]))
                ++index;

            for (var newIndex = index; newIndex < newItems.Count; ++newIndex)
            {
                if (!ItemsExactlyEqual(oldItems[newIndex + removedCount], newItems[newIndex]))
                {
                    index = -1;
                    return false;
                }
            }

            return true;
        }

        private static bool ItemsExactlyEqual(object? left, object? right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (left is null || right is null)
                return false;

            var type = left.GetType();
            return type.IsValueType && type == right.GetType() && left.Equals(right);
        }

        private NotifyCollectionChangedEventArgs ApplyReplace(
            INotifyCollectionChanged source,
            NotifyCollectionChangedEventArgs e)
        {
            var oldItems = e.OldItems ?? throw new InvalidOperationException();
            var newItems = e.NewItems ?? throw new InvalidOperationException();

            if (e.OldStartingIndex >= 0)
            {
                RemoveItemsAt(_snapshot, oldItems.Count, e.OldStartingIndex);
                InsertItems(_snapshot, newItems, e.NewStartingIndex);
                return e;
            }

            if (TryRemoveItemsByExactIdentity(_snapshot, oldItems))
            {
                InsertItems(_snapshot, newItems, e.NewStartingIndex);
                return e;
            }

            // Dictionary-like collections may preserve the originally stored key while their
            // notification contains a comparer-equal key supplied by the caller. Recover both
            // sides from the snapshots so undo/redo keeps the real key object/representation.
            var newSnapshot = CreateSnapshot(source);
            var actualOldItems = FindExactDifference(_snapshot, newSnapshot);
            var actualNewItems = FindExactDifference(newSnapshot, _snapshot);
            _snapshot = newSnapshot;

            return actualOldItems.Count == oldItems.Count && actualNewItems.Count == newItems.Count
                ? CreateReplaceEventArgs(actualNewItems, actualOldItems)
                : e;
        }

        private static void InsertItems(List<object?> snapshot, IList items, int index)
        {
            if (index < 0)
            {
                foreach (var item in items)
                    snapshot.Add(item);
                return;
            }

            for (var i = 0; i < items.Count; ++i)
                snapshot.Insert(index + i, items[i]);
        }

        private static void RemoveItemsAt(List<object?> snapshot, int count, int index)
        {
            for (var i = 0; i < count; ++i)
                snapshot.RemoveAt(index);
        }

        private static bool TryRemoveItemsByExactIdentity(List<object?> snapshot, IList items)
        {
            if (items.Count is 1)
            {
                var index = FindExactIndex(snapshot, items[0]);
                if (index < 0)
                    return false;

                snapshot.RemoveAt(index);
                return true;
            }

            var updatedSnapshot = new List<object?>(snapshot);
            foreach (var item in items)
            {
                var index = FindExactIndex(updatedSnapshot, item);
                if (index < 0)
                    return false;

                updatedSnapshot.RemoveAt(index);
            }

            snapshot.Clear();
            snapshot.AddRange(updatedSnapshot);
            return true;
        }

        private static int FindExactIndex(IReadOnlyList<object?> items, object? target)
        {
            var targetKey = new ExactItemKey(target);
            for (var i = 0; i < items.Count; ++i)
            {
                if (targetKey.Equals(new ExactItemKey(items[i])))
                    return i;
            }

            return -1;
        }

        private static List<object?> FindExactDifference(
            IReadOnlyList<object?> source,
            IReadOnlyList<object?> other)
        {
            var counts = new Dictionary<ExactItemKey, int>();
            for (var i = 0; i < other.Count; ++i)
            {
                var key = new ExactItemKey(other[i]);
                counts.TryGetValue(key, out var count);
                counts[key] = count + 1;
            }

            var difference = new List<object?>();
            for (var i = 0; i < source.Count; ++i)
            {
                var item = source[i];
                var key = new ExactItemKey(item);
                if (counts.TryGetValue(key, out var count) && count > 0)
                {
                    if (count is 1)
                        counts.Remove(key);
                    else
                        counts[key] = count - 1;
                }
                else
                    difference.Add(item);
            }

            return difference;
        }

        private static NotifyCollectionChangedEventArgs CreateRemoveEventArgs(IList items)
        {
            return items.Count is 1
                ? new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, items[0])
                : new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, items);
        }

        private static NotifyCollectionChangedEventArgs CreateReplaceEventArgs(IList newItems, IList oldItems)
        {
            return newItems.Count is 1 && oldItems.Count is 1
                ? new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newItems[0], oldItems[0])
                : new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newItems, oldItems);
        }

        private readonly struct ExactItemKey(object? item) : IEquatable<ExactItemKey>
        {
            public bool Equals(ExactItemKey other)
            {
                if (ReferenceEquals(_item, other._item))
                    return true;
                if (_item is null || other._item is null)
                    return false;

                var type = _item.GetType();
                return type.IsValueType && type == other._item.GetType() && _item.Equals(other._item);
            }

            public override bool Equals(object? obj)
            {
                return obj is ExactItemKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                if (_item is null)
                    return 0;

                return _item.GetType().IsValueType
                    ? _item.GetHashCode()
                    : RuntimeHelpers.GetHashCode(_item);
            }

            private readonly object? _item = item;
        }
    }
}
