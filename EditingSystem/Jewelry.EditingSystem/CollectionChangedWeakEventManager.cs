using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

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

            if (e.Action is NotifyCollectionChangedAction.Reset)
            {
                _resetSnapshots.Push(_snapshot);
                _snapshot = CreateSnapshot(source);
            }
            else
            {
                try
                {
                    ApplyChange(_snapshot, e);
                }
                catch
                {
                    _snapshot = CreateSnapshot(source);
                }
            }

            try
            {
                if (_handler.TryGetTarget(out var handler))
                    handler(sender, e);
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

        private static void ApplyChange(List<object?> snapshot, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    InsertItems(snapshot, e.NewItems ?? throw new InvalidOperationException(), e.NewStartingIndex);
                    break;

                case NotifyCollectionChangedAction.Remove:
                    RemoveItems(snapshot, e.OldItems ?? throw new InvalidOperationException(), e.OldStartingIndex);
                    break;

                case NotifyCollectionChangedAction.Move:
                {
                    var items = new List<object?>();
                    var count = (e.OldItems ?? throw new InvalidOperationException()).Count;
                    for (var i = 0; i < count; ++i)
                    {
                        items.Add(snapshot[e.OldStartingIndex]);
                        snapshot.RemoveAt(e.OldStartingIndex);
                    }

                    for (var i = 0; i < items.Count; ++i)
                        snapshot.Insert(e.NewStartingIndex + i, items[i]);
                    break;
                }

                case NotifyCollectionChangedAction.Replace:
                    RemoveItems(snapshot, e.OldItems ?? throw new InvalidOperationException(), e.OldStartingIndex);
                    InsertItems(snapshot, e.NewItems ?? throw new InvalidOperationException(), e.NewStartingIndex);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
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

        private static void RemoveItems(List<object?> snapshot, IList items, int index)
        {
            if (index >= 0)
            {
                for (var i = 0; i < items.Count; ++i)
                    snapshot.RemoveAt(index);
                return;
            }

            if (items.Count is 1)
            {
                _ = snapshot.Remove(items[0]);
                return;
            }

            var removedItems = new HashSet<object?>();
            foreach (var item in items)
                removedItems.Add(item);
            snapshot.RemoveAll(item => removedItems.Contains(item));
        }
    }
}
