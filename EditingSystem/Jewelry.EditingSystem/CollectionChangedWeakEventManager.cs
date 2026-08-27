using System;
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

        private readonly WeakReference<INotifyCollectionChanged> _source;
        private readonly WeakReference<NotifyCollectionChangedEventHandler> _handler;

        public CollectionChangedWeakEventListener(INotifyCollectionChanged source, NotifyCollectionChangedEventHandler handler)
        {
            _source = new WeakReference<INotifyCollectionChanged>(source);
            _handler = new WeakReference<NotifyCollectionChangedEventHandler>(handler);

            source.CollectionChanged += HandleEvent;
        }

        private void HandleEvent(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (_handler.TryGetTarget(out var handler))
                handler(sender, e);
            else
                Dispose();
        }

        public void Dispose()
        {
            if (_source.TryGetTarget(out var source))
                source.CollectionChanged -= HandleEvent;
        }
    }
}
