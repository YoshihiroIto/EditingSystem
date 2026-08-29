using System;
using System.Collections.Generic;

namespace Jewelry.EditingSystem;

public static class ListExtensions
{
    public static void ClearEx<T>(this IList<T> self, History history)
    {
        if (self is null)
            throw new ArgumentNullException(nameof(self));
        if (history is null)
            throw new ArgumentNullException(nameof(history));
        if (self.Count is 0)
            return;

        var oldItems = new List<T>(self);

        using (history.Pause())
        {
            try
            {
                RemoveAll(self);
            }
            catch (Exception applyException)
            {
                RollBack(() => Restore(self, oldItems), applyException);
                throw;
            }
        }

        history.Push(
            () =>
            {
                Restore(self, oldItems);
                NotifyItems(oldItems, CollectionItemChangedInfo.Add);
            },
            () =>
            {
                RemoveAll(self);
                NotifyItems(oldItems, CollectionItemChangedInfo.Remove);
            });
    }

    public static void ClearEx<T>(this ICollection<T> self, History history)
    {
        if (self is null)
            throw new ArgumentNullException(nameof(self));
        if (history is null)
            throw new ArgumentNullException(nameof(history));
        if (self.Count is 0)
            return;

        var oldItems = new List<T>(self);

        using (history.Pause())
        {
            try
            {
                RemoveAll(self);
            }
            catch (Exception applyException)
            {
                RollBack(() => Restore(self, oldItems), applyException);
                throw;
            }
        }

        history.Push(
            () =>
            {
                Restore(self, oldItems);
                NotifyItems(oldItems, CollectionItemChangedInfo.Add);
            },
            () =>
            {
                RemoveAll(self);
                NotifyItems(oldItems, CollectionItemChangedInfo.Remove);
            });
    }

    private static void RemoveAll<T>(IList<T> collection)
    {
        while (collection.Count > 0)
            collection.RemoveAt(collection.Count - 1);
    }

    private static void RemoveAll<T>(ICollection<T> collection)
    {
        var items = new List<T>(collection);
        foreach (var item in items)
        {
            if (collection.Remove(item) is false)
                throw new InvalidOperationException("The item to remove was not found in the collection.");
        }
    }

    private static void Restore<T>(IList<T> collection, IReadOnlyList<T> items)
    {
        RemoveAll(collection);
        for (var i = 0; i < items.Count; ++i)
            collection.Insert(i, items[i]);
    }

    private static void Restore<T>(ICollection<T> collection, IReadOnlyList<T> items)
    {
        RemoveAll(collection);
        foreach (var item in items)
            collection.Add(item);
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
}
