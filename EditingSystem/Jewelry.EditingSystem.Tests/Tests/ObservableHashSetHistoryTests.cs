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
}
