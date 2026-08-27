using System.ComponentModel;
using Jewelry.EditingSystem.Tests.TestModels;
using Xunit;
using static Jewelry.EditingSystem.Tests.TestModels.TestModelCreator;

namespace Jewelry.EditingSystem.Tests.Tests;

public sealed class SinglePropertyTests
{
    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void Basic(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateBasicTestModel(testModelKind, history);

        Assert.Equal(0, model.IntValue);
        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);

        //------------------------------------------------
        model.IntValue = 123;
        Assert.Equal(123, model.IntValue);
        Assert.True(history.CanUndo);
        Assert.False(history.CanRedo);

        model.IntValue = 456;
        Assert.Equal(456, model.IntValue);
        Assert.True(history.CanUndo);
        Assert.False(history.CanRedo);

        model.IntValue = 789;
        Assert.Equal(789, model.IntValue);
        Assert.True(history.CanUndo);
        Assert.False(history.CanRedo);

        history.Undo();
        Assert.Equal(456, model.IntValue);
        Assert.True(history.CanUndo);
        Assert.True(history.CanRedo);

        history.Undo();
        Assert.Equal(123, model.IntValue);
        Assert.True(history.CanUndo);
        Assert.True(history.CanRedo);

        history.Undo();
        Assert.Equal(0, model.IntValue);
        Assert.False(history.CanUndo);
        Assert.True(history.CanRedo);

        //------------------------------------------------
        history.Redo();
        Assert.Equal(123, model.IntValue);
        Assert.True(history.CanUndo);
        Assert.True(history.CanRedo);

        history.Redo();
        Assert.Equal(456, model.IntValue);
        Assert.True(history.CanUndo);
        Assert.True(history.CanRedo);

        history.Redo();
        Assert.Equal(789, model.IntValue);
        Assert.True(history.CanUndo);
        Assert.False(history.CanRedo);

        //------------------------------------------------
        history.Undo();
        history.Undo();
        history.Undo();

        Assert.False(history.CanUndo);
        Assert.True(history.CanRedo);


        model.IntValue = 111;
        Assert.True(history.CanUndo);
        Assert.False(history.CanRedo);
    }

    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void PropertyChanged(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateBasicTestModel(testModelKind, history);

        var count = 0;

        model.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(model.IntValue))
                ++count;
        };

        Assert.Equal(0, count);

        model.IntValue = 123;
        Assert.Equal(1, count);

        model.IntValue = 456;
        Assert.Equal(2, count);

        history.Undo();
        Assert.Equal(3, count);

        history.Redo();
        Assert.Equal(4, count);
    }

    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void PropertyChanged_CanUndo_CanRedo_CanClear(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateBasicTestModel(testModelKind, history);

        var canUndoCount = 0;
        var canRedoCount = 0;
        var canClearCount = 0;

        void HistoryOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "CanUndo") ++canUndoCount;
            if (e.PropertyName == "CanRedo") ++canRedoCount;
            if (e.PropertyName == "CanClear") ++canClearCount;
        }

        history.PropertyChanged += HistoryOnPropertyChanged;

        model.IntValue = 123;
        Assert.Equal(1, canUndoCount);
        Assert.Equal(0, canRedoCount);
        Assert.Equal(1, canClearCount);

        model.IntValue = 456;
        Assert.Equal(1, canUndoCount);
        Assert.Equal(0, canRedoCount);
        Assert.Equal(1, canClearCount);

        history.Undo();
        Assert.Equal(1, canUndoCount);
        Assert.Equal(1, canRedoCount);
        Assert.Equal(1, canClearCount);

        history.Undo();
        Assert.Equal(2, canUndoCount);
        Assert.Equal(1, canRedoCount);
        Assert.Equal(1, canClearCount);
    }

    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void Clear(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateBasicTestModel(testModelKind, history);

        Assert.Equal(0, model.IntValue);
        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);
        Assert.False(history.CanClear);

        //------------------------------------------------
        model.IntValue = 123;
        Assert.Equal(123, model.IntValue);
        Assert.True(history.CanUndo);
        Assert.False(history.CanRedo);
        Assert.True(history.CanClear);

        history.Clear();
        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);
        Assert.False(history.CanClear);

        //------------------------------------------------
        model.IntValue = 456;
        Assert.Equal(456, model.IntValue);
        Assert.True(history.CanUndo);
        Assert.False(history.CanRedo);
        Assert.True(history.CanClear);

        history.Undo();
        Assert.False(history.CanUndo);
        Assert.True(history.CanRedo);
        Assert.True(history.CanClear);

        history.Clear();
        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);
        Assert.False(history.CanClear);
    }
}