using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;

namespace Jewelry.EditingSystem;

internal sealed class CollectionChangedWeakEventManager : IDisposable
{
    private readonly List<Registration> _listeners = new();

    public void AddWeakEventListener(INotifyCollectionChanged source, NotifyCollectionChangedEventHandler handler)
    {
        for (var i = _listeners.Count - 1; i >= 0; --i)
        {
            var registration = _listeners[i];
            if (registration.Listener.IsAlive is false)
            {
                registration.Listener.Dispose();
                _listeners.RemoveAt(i);
                continue;
            }

            if (ReferenceEquals(registration.Listener.Source, source))
            {
                ++registration.ReferenceCount;
                return;
            }
        }

        _listeners.Add(new Registration(source, handler));
    }

    public void RemoveWeakEventListener(INotifyCollectionChanged source)
    {
        for (var i = _listeners.Count - 1; i >= 0; --i)
        {
            var registration = _listeners[i];
            if (registration.Listener.IsAlive is false)
            {
                registration.Listener.Dispose();
                _listeners.RemoveAt(i);
                continue;
            }

            if (ReferenceEquals(registration.Listener.Source, source) is false)
                continue;

            if (registration.ReferenceCount > 1)
                --registration.ReferenceCount;
            else
            {
                registration.Listener.Dispose();
                _listeners.RemoveAt(i);
            }

            return;
        }
    }

    public IReadOnlyList<object?> GetSnapshot(INotifyCollectionChanged source)
    {
        foreach (var registration in _listeners)
        {
            if (ReferenceEquals(registration.Listener.Source, source))
                return registration.Listener.Snapshot;
        }

        throw new InvalidOperationException("The collection is not registered.");
    }

    public void Dispose()
    {
        foreach (var registration in _listeners)
            registration.Listener.Dispose();

        _listeners.Clear();
    }

    private sealed class Registration
    {
        public Registration(INotifyCollectionChanged source, NotifyCollectionChangedEventHandler handler)
        {
            Listener = new CollectionChangedWeakEventListener(source, handler);
            Handler = handler;
        }

        public CollectionChangedWeakEventListener Listener { get; }
        // Keep the weak listener's delegate alive for as long as this registration is active.
        public NotifyCollectionChangedEventHandler Handler { get; }
        public int ReferenceCount { get; set; } = 1;
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

            var snapshot = new List<object?>();
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
                    InsertItems(_snapshot, e.NewItems ?? throw new InvalidOperationException(), e.NewStartingIndex);
                    return e;

                case NotifyCollectionChangedAction.Remove:
                    return ApplyRemove(source, e);

                case NotifyCollectionChangedAction.Move:
                {
                    var items = new List<object?>();
                    var count = (e.OldItems ?? throw new InvalidOperationException()).Count;
                    for (var i = 0; i < count; ++i)
                    {
                        items.Add(_snapshot[e.OldStartingIndex]);
                        _snapshot.RemoveAt(e.OldStartingIndex);
                    }

                    for (var i = 0; i < items.Count; ++i)
                        _snapshot.Insert(e.NewStartingIndex + i, items[i]);
                    return e;
                }

                case NotifyCollectionChangedAction.Replace:
                    return ApplyReplace(source, e);

                default:
                    throw new ArgumentOutOfRangeException();
            }
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
                ? new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Replace,
                    newItems[0],
                    oldItems[0])
                : new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Replace,
                    newItems,
                    oldItems);
        }

        private readonly struct ExactItemKey : IEquatable<ExactItemKey>
        {
            public ExactItemKey(object? item)
            {
                _item = item;
            }

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

            private readonly object? _item;
        }
    }
}
