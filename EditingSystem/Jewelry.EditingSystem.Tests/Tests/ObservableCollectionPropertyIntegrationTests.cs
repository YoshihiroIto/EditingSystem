using System.Collections.Generic;
using Jewelry.Collections;
using Jewelry.EditingSystem.Tests.TestModels;
using Xunit;
using static Jewelry.EditingSystem.Tests.TestModels.TestModelCreator;

namespace Jewelry.EditingSystem.Tests.Tests;

public sealed class ObservableCollectionPropertyIntegrationTests
{
    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void ObservableHashSet_property_follows_property_undo_and_redo(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateBasicTestModel(testModelKind, history);
        var original = model.IntSet;
        var replacement = new ObservableHashSet<int>([10]);

        model.IntSet = replacement;
        Assert.Same(replacement, model.IntSet);

        history.Undo();
        Assert.Same(original, model.IntSet);

        history.Redo();
        Assert.Same(replacement, model.IntSet);
        history.Clear();

        original.Add(99);
        Assert.False(history.CanUndo);

        replacement.Add(20);
        Assert.Equal(1, history.UndoCount);

        history.Undo();
        Assert.True(replacement.SetEquals([10]));

        history.Redo();
        Assert.True(replacement.SetEquals([10, 20]));
    }

    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void ObservableDictionary_property_follows_property_undo_and_redo(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateBasicTestModel(testModelKind, history);
        var original = model.IntDictionary;
        var replacement = new ObservableDictionary<string, int>(
            new Dictionary<string, int> { ["one"] = 1 });

        model.IntDictionary = replacement;
        Assert.Same(replacement, model.IntDictionary);

        history.Undo();
        Assert.Same(original, model.IntDictionary);

        history.Redo();
        Assert.Same(replacement, model.IntDictionary);
        history.Clear();

        original["detached"] = 99;
        Assert.False(history.CanUndo);

        replacement["one"] = 10;
        Assert.Equal(1, history.UndoCount);

        history.Undo();
        Assert.Equal(1, replacement["one"]);

        history.Redo();
        Assert.Equal(10, replacement["one"]);
    }
}
