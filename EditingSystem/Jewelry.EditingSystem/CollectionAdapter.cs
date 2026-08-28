using System;
using System.Collections.Generic;

namespace Jewelry.EditingSystem;

internal interface ICollectionAdapter
{
    void Add(object? item);
    void Remove(object? item);
}

internal static class CollectionAdapter
{
    public static ICollectionAdapter Create(object collection)
    {
        foreach (var interfaceType in collection.GetType().GetInterfaces())
        {
            if (interfaceType.IsGenericType is false ||
                interfaceType.GetGenericTypeDefinition() != typeof(ICollection<>))
                continue;

            var itemType = interfaceType.GetGenericArguments()[0];
            var adapterType = typeof(GenericCollectionAdapter<>).MakeGenericType(itemType);
            return (ICollectionAdapter)(Activator.CreateInstance(adapterType, collection) ??
                throw new InvalidOperationException("Failed to create a collection adapter."));
        }

        throw new NotSupportedException(
            $"Collection type '{collection.GetType()}' must implement IList or ICollection<T>.");
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

        private readonly ICollection<T> _collection = (ICollection<T>)collection;
    }
}
