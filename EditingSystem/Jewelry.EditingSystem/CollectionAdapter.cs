using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace Jewelry.EditingSystem;

internal interface ICollectionAdapter
{
    void Add(object? item);
    void Remove(object? item);
    bool TryMove(int oldIndex, int newIndex);
}

internal static class CollectionAdapter
{
    public static ICollectionAdapter Create(object collection)
    {
        if (collection is null)
            throw new ArgumentNullException(nameof(collection));

        return Adapters.GetValue(collection, CreateCore);
    }

    public static bool TryMove(object collection, int oldIndex, int newIndex)
    {
        foreach (var interfaceType in collection.GetType().GetInterfaces())
        {
            if (interfaceType.IsGenericType &&
                interfaceType.GetGenericTypeDefinition() == typeof(ICollection<>))
                return Create(collection).TryMove(oldIndex, newIndex);
        }

        return false;
    }

    private static ICollectionAdapter CreateCore(object collection)
    {
        var factory = Factories.GetOrAdd(collection.GetType(), CreateFactory);
        return factory.Create(collection);
    }

    private static ICollectionAdapterFactory CreateFactory(Type collectionType)
    {
        foreach (var interfaceType in collectionType.GetInterfaces())
        {
            if (interfaceType.IsGenericType is false ||
                interfaceType.GetGenericTypeDefinition() != typeof(ICollection<>))
                continue;

            var itemType = interfaceType.GetGenericArguments()[0];
            var factoryType = typeof(GenericCollectionAdapterFactory<>).MakeGenericType(itemType);
            return (ICollectionAdapterFactory)(Activator.CreateInstance(factoryType) ??
                throw new InvalidOperationException("Failed to create a collection adapter factory."));
        }

        throw new NotSupportedException(
            $"Collection type '{collectionType}' must implement IList or ICollection<T>.");
    }

    private interface ICollectionAdapterFactory
    {
        ICollectionAdapter Create(object collection);
    }

    private sealed class GenericCollectionAdapterFactory<T> : ICollectionAdapterFactory
    {
        public ICollectionAdapter Create(object collection)
        {
            return new GenericCollectionAdapter<T>(collection);
        }
    }

    private sealed class GenericCollectionAdapter<T>(object collection) : ICollectionAdapter
    {
        public void Add(object? item)
        {
            _collection.Add((T)item!);
        }

        public void Remove(object? item)
        {
            if (_collection.Remove((T)item!) is false)
                throw new InvalidOperationException("The item to remove was not found in the collection.");
        }

        public bool TryMove(int oldIndex, int newIndex)
        {
            if (_collection is not ObservableCollection<T> observableCollection)
                return false;

            observableCollection.Move(oldIndex, newIndex);
            return true;
        }

        private readonly ICollection<T> _collection = (ICollection<T>)collection;
    }

    private static readonly ConditionalWeakTable<object, ICollectionAdapter> Adapters = new();
    private static readonly ConcurrentDictionary<Type, ICollectionAdapterFactory> Factories = new();
}
