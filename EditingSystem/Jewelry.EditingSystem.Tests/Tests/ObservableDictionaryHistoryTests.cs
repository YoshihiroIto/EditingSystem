using System.Collections.Generic;
using Jewelry.Collections;
using Xunit;

namespace Jewelry.EditingSystem.Tests.Tests;

public sealed class ObservableDictionaryHistoryTests
{
    [Fact]
    public void Add_can_be_undone_and_redone()
    {
        using var history = new History();
        var dictionary = Observe(history, new ObservableDictionary<string, int>());

        dictionary.Add("one", 1);
        AssertDictionary(dictionary, ("one", 1));
        AssertHistory(history, undoCount: 1, redoCount: 0);

        history.Undo();
        Assert.Empty(dictionary);
        AssertHistory(history, undoCount: 0, redoCount: 1);

        history.Redo();
        AssertDictionary(dictionary, ("one", 1));
        AssertHistory(history, undoCount: 1, redoCount: 0);
    }

    [Fact]
    public void Indexer_add_can_be_undone_and_redone()
    {
        using var history = new History();
        var dictionary = Observe(history, new ObservableDictionary<string, int>());

        dictionary["one"] = 1;
        AssertDictionary(dictionary, ("one", 1));

        history.Undo();
        Assert.Empty(dictionary);
        AssertHistory(history, undoCount: 0, redoCount: 1);

        history.Redo();
        AssertDictionary(dictionary, ("one", 1));
        AssertHistory(history, undoCount: 1, redoCount: 0);
    }

    [Fact]
    public void Indexer_replace_can_be_undone_and_redone()
    {
        using var history = new History();
        var dictionary = Observe(history, new ObservableDictionary<string, int>(
            new Dictionary<string, int> { ["one"] = 1 }));

        dictionary["one"] = 10;
        AssertDictionary(dictionary, ("one", 10));
        AssertHistory(history, undoCount: 1, redoCount: 0);

        history.Undo();
        AssertDictionary(dictionary, ("one", 1));
        AssertHistory(history, undoCount: 0, redoCount: 1);

        history.Redo();
        AssertDictionary(dictionary, ("one", 10));
        AssertHistory(history, undoCount: 1, redoCount: 0);
    }

    [Fact]
    public void Remove_can_be_undone_and_redone()
    {
        using var history = new History();
        var dictionary = Observe(history, new ObservableDictionary<string, int>(
            new Dictionary<string, int> { ["one"] = 1, ["two"] = 2 }));

        Assert.True(dictionary.Remove("one"));
        AssertDictionary(dictionary, ("two", 2));

        history.Undo();
        AssertDictionary(dictionary, ("one", 1), ("two", 2));
        AssertHistory(history, undoCount: 0, redoCount: 1);

        history.Redo();
        AssertDictionary(dictionary, ("two", 2));
        AssertHistory(history, undoCount: 1, redoCount: 0);
    }

    [Fact]
    public void No_op_changes_are_not_recorded()
    {
        using var history = new History();
        var dictionary = Observe(history, new ObservableDictionary<string, int>(
            new Dictionary<string, int> { ["one"] = 1 }));

        Assert.False(dictionary.TryAdd("one", 10));
        Assert.False(dictionary.Remove("missing"));

        AssertDictionary(dictionary, ("one", 1));
        AssertHistory(history, undoCount: 0, redoCount: 0);
    }

    [Fact]
    public void ClearEx_can_be_undone_and_redone_as_one_action()
    {
        using var history = new History();
        var dictionary = Observe(history, new ObservableDictionary<string, int>(
            new Dictionary<string, int> { ["one"] = 1, ["two"] = 2 }));

        dictionary.ClearEx(history);
        Assert.Empty(dictionary);
        AssertHistory(history, undoCount: 1, redoCount: 0);

        history.Undo();
        AssertDictionary(dictionary, ("one", 1), ("two", 2));
        AssertHistory(history, undoCount: 0, redoCount: 1);

        history.Redo();
        Assert.Empty(dictionary);
        AssertHistory(history, undoCount: 1, redoCount: 0);
    }

    private static T Observe<T>(History history, T collection)
    {
        history.RecordPropertyChange<T>(_ => { }, default!, collection);
        history.Clear();
        return collection;
    }

    private static void AssertDictionary(
        ObservableDictionary<string, int> dictionary,
        params (string Key, int Value)[] expected)
    {
        Assert.Equal(expected.Length, dictionary.Count);
        foreach (var (key, value) in expected)
            Assert.Equal(value, dictionary[key]);
    }

    private static void AssertHistory(History history, int undoCount, int redoCount)
    {
        Assert.Equal(undoCount, history.UndoCount);
        Assert.Equal(redoCount, history.RedoCount);
    }
}
