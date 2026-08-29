using System;
using System.Collections.Generic;

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

        using (history.Pause())
        {
            try
            {
                action();
            }
            catch (Exception applyException)
            {
                RollBack(() => Restore(self, oldItems, notifyItems: false), applyException);
                throw;
            }
        }

        if (self.SetEquals(oldItems))
            return;

        var newItems = new List<T>(self);
        history.Push(
            () => Restore(self, oldItems, notifyItems: true),
            () => Restore(self, newItems, notifyItems: true));
    }

    private static void Restore<T>(ISet<T> self, IReadOnlyList<T> items, bool notifyItems)
    {
        List<T>? removedItems = notifyItems ? new List<T>(self) : null;
        self.Clear();
        self.UnionWith(items);

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
}
