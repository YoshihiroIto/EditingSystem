using System;
using System.Collections.Generic;

namespace Jewelry.EditingSystem;

public static class SetExtensions
{
    public static void UnionWithEx<T>(this ISet<T> self, IEnumerable<T> other, History history)
    {
        ExecuteInBatch(history, () => self.UnionWith(other));
    }

    public static void IntersectWithEx<T>(this ISet<T> self, IEnumerable<T> other, History history)
    {
        ExecuteInBatch(history, () => self.IntersectWith(other));
    }

    public static void ExceptWithEx<T>(this ISet<T> self, IEnumerable<T> other, History history)
    {
        ExecuteInBatch(history, () => self.ExceptWith(other));
    }

    public static void SymmetricExceptWithEx<T>(this ISet<T> self, IEnumerable<T> other, History history)
    {
        ExecuteInBatch(history, () => self.SymmetricExceptWith(other));
    }

    public static int RemoveWhereEx<T>(this ISet<T> self, Predicate<T> match, History history)
    {
        if (match is null)
            throw new ArgumentNullException(nameof(match));

        var removedCount = 0;
        var items = new List<T>(self);

        ExecuteInBatch(history, () =>
        {
            foreach (var item in items)
            {
                if (match(item) && self.Remove(item))
                    ++removedCount;
            }
        });

        return removedCount;
    }

    private static void ExecuteInBatch(History history, Action action)
    {
        history.BeginBatch();

        try
        {
            action();
        }
        finally
        {
            history.EndBatch();
        }
    }
}
