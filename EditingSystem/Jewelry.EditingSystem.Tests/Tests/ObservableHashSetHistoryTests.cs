using System.Collections.Generic;
using System.Collections.Specialized;
using Jewelry.Collections;
using Xunit;

namespace Jewelry.EditingSystem.Tests.Tests;

public sealed class ObservableHashSetHistoryTests
{
    [Fact]
    public void Add_can_be_undone_and_redone()
    {
        using var history = new History();
        var set = Observe(history, new ObservableHashSet<int>());

        Assert.True(set.Add(10));
        Assert.True(set.SetEquals([10]));
        AssertHistory(history, undoCount: 1, redoCount: 0);

        history.Undo();
        Assert.Empty(set);
        AssertHistory(history, undoCount: 0, redoCount: 1);

        history.Redo();
        Assert.True(set.SetEquals([10]));
        AssertHistory(history, undoCount: 1, redoCount: 0);
    }

    [Fact]
    public void Remove_can_be_undone_and_redone()
    {
        using var history = new History();
        var set = Observe(history, new ObservableHashSet<int>([10, 20]));

        Assert.True(set.Remove(10));
        Assert.True(set.SetEquals([20]));
        AssertHistory(history, undoCount: 1, redoCount: 0);

        history.Undo();
        Assert.True(set.SetEquals([10, 20]));
        AssertHistory(history, undoCount: 0, redoCount: 1);

        history.Redo();
        Assert.True(set.SetEquals([20]));
        AssertHistory(history, undoCount: 1, redoCount: 0);
    }

    [Fact]
    public void No_op_changes_are_not_recorded()
    {
        using var history = new History();
        var set = Observe(history, new ObservableHashSet<int>([10]));

        Assert.False(set.Add(10));
        Assert.False(set.Remove(20));

        Assert.True(set.SetEquals([10]));
        AssertHistory(history, undoCount: 0, redoCount: 0);
    }

    [Fact]
    public void ClearEx_can_be_undone_and_redone_as_one_action()
    {
        using var history = new History();
        var set = Observe(history, new ObservableHashSet<int>([10, 20, 30]));

        set.ClearEx(history);
        Assert.Empty(set);
        AssertHistory(history, undoCount: 1, redoCount: 0);

        history.Undo();
        Assert.True(set.SetEquals([10, 20, 30]));
        AssertHistory(history, undoCount: 0, redoCount: 1);

        history.Redo();
        Assert.Empty(set);
        AssertHistory(history, undoCount: 1, redoCount: 0);
    }

    [Fact]
    public void Clear_can_be_undone_and_redone()
    {
        using var history = new History();
        var set = Observe(history, new ObservableHashSet<int>([10, 20, 30]));

        set.Clear();
        Assert.Empty(set);
        AssertHistory(history, undoCount: 1, redoCount: 0);

        history.Undo();
        Assert.True(set.SetEquals([10, 20, 30]));
        AssertHistory(history, undoCount: 0, redoCount: 1);

        history.Redo();
        Assert.Empty(set);
        AssertHistory(history, undoCount: 1, redoCount: 0);
    }

    [Fact]
    public void UnionWithEx_can_be_undone_and_redone_as_one_action()
    {
        using var history = new History();
        var set = Observe(history, new ObservableHashSet<int>([10]));

        set.UnionWithEx([20, 30], history);
        Assert.True(set.SetEquals([10, 20, 30]));
        AssertHistory(history, undoCount: 1, redoCount: 0);

        history.Undo();
        Assert.True(set.SetEquals([10]));
        AssertHistory(history, undoCount: 0, redoCount: 1);

        history.Redo();
        Assert.True(set.SetEquals([10, 20, 30]));
        AssertHistory(history, undoCount: 1, redoCount: 0);
    }

    [Fact]
    public void UnionWithEx_undo_redo_replays_only_the_delta()
    {
        using var history = new History();
        var initialItems = new int[100_000];
        for (var i = 0; i < initialItems.Length; ++i)
            initialItems[i] = i;

        var set = Observe(history, new ObservableHashSet<int>(initialItems));
        set.UnionWithEx([100_000], history);

        var addCount = 0;
        var removeCount = 0;
        var otherCount = 0;
        set.CollectionChanged += (_, e) =>
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    ++addCount;
                    break;
                case NotifyCollectionChangedAction.Remove:
                    ++removeCount;
                    break;
                default:
                    ++otherCount;
                    break;
            }
        };

        history.Undo();
        Assert.Equal(0, addCount);
        Assert.Equal(1, removeCount);
        Assert.Equal(0, otherCount);
        Assert.Equal(100_000, set.Count);

        addCount = 0;
        removeCount = 0;
        otherCount = 0;

        history.Redo();
        Assert.Equal(1, addCount);
        Assert.Equal(0, removeCount);
        Assert.Equal(0, otherCount);
        Assert.Equal(100_001, set.Count);
    }

    [Fact]
    public void Set_extension_is_undoable_without_property_observation()
    {
        using var history = new History();
        var set = new ObservableHashSet<int>([10]);

        set.UnionWithEx([20, 30], history);
        Assert.True(set.SetEquals([10, 20, 30]));
        AssertHistory(history, undoCount: 1, redoCount: 0);

        history.Undo();
        Assert.True(set.SetEquals([10]));

        history.Redo();
        Assert.True(set.SetEquals([10, 20, 30]));
    }

    [Fact]
    public void IntersectWithEx_can_be_undone_and_redone_as_one_action()
    {
        using var history = new History();
        var set = Observe(history, new ObservableHashSet<int>([10, 20, 30]));

        set.IntersectWithEx([20], history);
        Assert.True(set.SetEquals([20]));
        AssertHistory(history, undoCount: 1, redoCount: 0);

        history.Undo();
        Assert.True(set.SetEquals([10, 20, 30]));
        AssertHistory(history, undoCount: 0, redoCount: 1);

        history.Redo();
        Assert.True(set.SetEquals([20]));
        AssertHistory(history, undoCount: 1, redoCount: 0);
    }

    [Fact]
    public void ExceptWithEx_can_be_undone_and_redone_as_one_action()
    {
        using var history = new History();
        var set = Observe(history, new ObservableHashSet<int>([10, 20, 30]));

        set.ExceptWithEx([20, 30], history);
        Assert.True(set.SetEquals([10]));
        AssertHistory(history, undoCount: 1, redoCount: 0);

        history.Undo();
        Assert.True(set.SetEquals([10, 20, 30]));
        AssertHistory(history, undoCount: 0, redoCount: 1);

        history.Redo();
        Assert.True(set.SetEquals([10]));
        AssertHistory(history, undoCount: 1, redoCount: 0);
    }

    [Fact]
    public void SymmetricExceptWithEx_can_be_undone_and_redone_as_one_action()
    {
        using var history = new History();
        var set = Observe(history, new ObservableHashSet<int>([10, 20]));

        set.SymmetricExceptWithEx([20, 30], history);
        Assert.True(set.SetEquals([10, 30]));
        AssertHistory(history, undoCount: 1, redoCount: 0);

        history.Undo();
        Assert.True(set.SetEquals([10, 20]));
        AssertHistory(history, undoCount: 0, redoCount: 1);

        history.Redo();
        Assert.True(set.SetEquals([10, 30]));
        AssertHistory(history, undoCount: 1, redoCount: 0);
    }

    [Fact]
    public void SymmetricExceptWithEx_preserves_actual_items_with_custom_comparer()
    {
        using var history = new History();
        var original = new SetItem(1, "original");
        var equivalent = new SetItem(1, "equivalent");
        var added = new SetItem(2, "added");
        var set = Observe(
            history,
            new ObservableHashSet<SetItem>([original], SetItemComparer.Instance));

        set.SymmetricExceptWithEx([equivalent, added], history);
        Assert.False(set.TryGetValue(original, out _));
        Assert.True(set.TryGetValue(added, out var actualAdded));
        Assert.Same(added, actualAdded);

        history.Undo();
        Assert.True(set.TryGetValue(equivalent, out var restored));
        Assert.Same(original, restored);
        Assert.False(set.TryGetValue(added, out _));

        history.Redo();
        Assert.False(set.TryGetValue(original, out _));
        Assert.True(set.TryGetValue(added, out actualAdded));
        Assert.Same(added, actualAdded);
    }

    [Fact]
    public void HashSet_delta_uses_the_sets_custom_comparer()
    {
        using var history = new History();
        var original = new SetItem(1, "original");
        var equivalent = new SetItem(1, "equivalent");
        var added = new SetItem(2, "added");
        var set = new HashSet<SetItem>([original], SetItemComparer.Instance);

        set.SymmetricExceptWithEx([equivalent, added], history);
        Assert.False(set.TryGetValue(original, out _));
        Assert.True(set.TryGetValue(added, out var actualAdded));
        Assert.Same(added, actualAdded);

        history.Undo();
        Assert.True(set.TryGetValue(equivalent, out var restored));
        Assert.Same(original, restored);
        Assert.False(set.TryGetValue(added, out _));

        history.Redo();
        Assert.False(set.TryGetValue(original, out _));
        Assert.True(set.TryGetValue(added, out actualAdded));
        Assert.Same(added, actualAdded);
    }

    [Fact]
    public void RemoveWhereEx_can_be_undone_and_redone_as_one_action()
    {
        using var history = new History();
        var set = Observe(history, new ObservableHashSet<int>([10, 11, 20, 21]));

        var removedCount = set.RemoveWhereEx(item => item % 10 is 0, history);
        Assert.Equal(2, removedCount);
        Assert.True(set.SetEquals([11, 21]));
        AssertHistory(history, undoCount: 1, redoCount: 0);

        history.Undo();
        Assert.True(set.SetEquals([10, 11, 20, 21]));
        AssertHistory(history, undoCount: 0, redoCount: 1);

        history.Redo();
        Assert.True(set.SetEquals([11, 21]));
        AssertHistory(history, undoCount: 1, redoCount: 0);
    }

    [Fact]
    public void No_op_set_operations_are_not_recorded()
    {
        using var history = new History();
        var set = Observe(history, new ObservableHashSet<int>([10, 20]));

        set.UnionWithEx([10, 20], history);
        set.IntersectWithEx([10, 20], history);
        set.ExceptWithEx([], history);
        set.SymmetricExceptWithEx([], history);
        Assert.Equal(0, set.RemoveWhereEx(_ => false, history));

        Assert.True(set.SetEquals([10, 20]));
        AssertHistory(history, undoCount: 0, redoCount: 0);
    }

    private static T Observe<T>(History history, T collection)
    {
        history.RecordPropertyChange<T>(_ => { }, default!, collection);
        history.Clear();
        return collection;
    }

    private static void AssertHistory(History history, int undoCount, int redoCount)
    {
        Assert.Equal(undoCount, history.UndoCount);
        Assert.Equal(redoCount, history.RedoCount);
    }

    private sealed class SetItem
    {
        public SetItem(int key, string name)
        {
            Key = key;
            Name = name;
        }

        public int Key { get; }
        public string Name { get; }
    }

    private sealed class SetItemComparer : IEqualityComparer<SetItem>
    {
        public static SetItemComparer Instance { get; } = new();

        public bool Equals(SetItem? x, SetItem? y)
        {
            return ReferenceEquals(x, y) || x is { } && y is { } && x.Key == y.Key;
        }

        public int GetHashCode(SetItem obj)
        {
            return obj.Key;
        }
    }
}
