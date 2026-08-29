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
        collection.Clear();
    }

    private static void RemoveAll<T>(ICollection<T> collection)
    {
        collection.Clear();
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
