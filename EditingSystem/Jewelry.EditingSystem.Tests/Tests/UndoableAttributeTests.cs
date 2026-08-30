using System.Collections.Generic;
using Jewelry.EditingSystem.Tests.TestModels;
using Xunit;

namespace Jewelry.EditingSystem.Tests.Tests;

public sealed class UndoableAttributeTests
{
    [Fact]
    public void PropertyChangeIsUndoableAndRedoable()
    {
        using var history = new History();
        var model = new UndoableAttributeBasicTestModel(history);

        model.Value = 10;
        model.Value = 20;

        Assert.Equal(2, history.UndoCount);
        Assert.Equal(20, model.Value);

        history.Undo();
        Assert.Equal(10, model.Value);

        history.Undo();
        Assert.Equal(0, model.Value);

        history.Redo();
        Assert.Equal(10, model.Value);

        history.Redo();
        Assert.Equal(20, model.Value);
    }

    [Fact]
    public void EqualValueDoesNotCreateHistory()
    {
        using var history = new History();
        var model = new UndoableAttributeBasicTestModel(history);

        model.Value = 10;
        model.Value = 10;

        Assert.Equal(1, history.UndoCount);
    }

    [Fact]
    public void InpcNotificationMethodIsUsedForNormalUndoAndRedoChanges()
    {
        using var history = new History();
        var model = new UndoableAttributeBasicTestModel(history);
        var notifications = new List<string?>();

        model.PropertyChanged += (_, e) => notifications.Add(e.PropertyName);

        model.Value = 10;
        history.Undo();
        history.Redo();

        Assert.Equal([nameof(model.Value), nameof(model.Value), nameof(model.Value)], notifications);
    }

    [Fact]
    public void LocallyDeclaredPropertyChangedEventIsUsedWhenNoNotificationMethodExists()
    {
        using var history = new History();
        var model = new UndoableAttributeDirectEventTestModel(history);
        var notifications = new List<string?>();

        model.PropertyChanged += (_, e) => notifications.Add(e.PropertyName);

        model.Value = 10;
        history.Undo();
        history.Redo();

        Assert.Equal([nameof(model.Value), nameof(model.Value), nameof(model.Value)], notifications);
    }

    [Fact]
    public void InpcIsNotRequired()
    {
        using var history = new History();
        var model = new UndoableAttributePlainTestModel(history);

        model.Value = 10;
        history.Undo();

        Assert.Equal(0, model.Value);
    }

    [Fact]
    public void CoalescingBatchUsesGeneratedPropertyKey()
    {
        using var history = new History();
        var model = new UndoableAttributeBasicTestModel(history);

        using (history.CoalescingBatch())
        {
            model.Value = 1;
            model.Value = 2;
            model.Value = 3;
            model.OtherValue = 10;
            model.OtherValue = 20;
        }

        Assert.Equal(1, history.UndoCount);

        history.Undo();

        Assert.Equal(0, model.Value);
        Assert.Equal(0, model.OtherValue);

        history.Redo();

        Assert.Equal(3, model.Value);
        Assert.Equal(20, model.OtherValue);
    }
}
