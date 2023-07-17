using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Jewelry.EditingSystem.WeakEvent;

internal sealed class CollectionChangedWeakEventManager : IDisposable
{
    private readonly Dictionary<CollectionChangedWeakEventListener, NotifyCollectionChangedEventHandler> _listeners = new();

    public void AddWeakEventListener(INotifyCollectionChanged source, NotifyCollectionChangedEventHandler handler)
    {
        _listeners.Add(new CollectionChangedWeakEventListener(source, handler), handler);
    }

    public void RemoveWeakEventListener(INotifyCollectionChanged source)
    {
        var toRemoveListeners = ArrayPool<CollectionChangedWeakEventListener>.Shared.Rent(_listeners.Count);

        try
        {
            var count = 0;

            foreach (var listener in _listeners.Keys)
            {
                if (listener.IsAlive == false)
                    toRemoveListeners[count++] = listener;

                else if (listener.Source == source)
                {
                    listener.Dispose();
                    toRemoveListeners[count++] = listener;
                }
            }

            for (var i = 0; i != count; ++i)
                _listeners.Remove(toRemoveListeners[i]);
        }
        finally
        {
            ArrayPool<CollectionChangedWeakEventListener>.Shared.Return(toRemoveListeners);
        }
    }

    public void Dispose()
    {
        foreach (var listener in _listeners.Keys)
        {
            if (listener.IsAlive == false)
                continue;

            listener.Dispose();
        }

        _listeners.Clear();
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