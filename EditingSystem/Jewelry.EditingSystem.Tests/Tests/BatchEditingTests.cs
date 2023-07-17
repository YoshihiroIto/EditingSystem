using Jewelry.EditingSystem.Tests.TestModels;
using System;
using Xunit;
using static Jewelry.EditingSystem.Tests.TestModels.TestModelCreator;

namespace Jewelry.EditingSystem.Tests;

public sealed class BatchEditingTests
{
    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void Basic(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateTestModel(testModelKind, history);

        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);

        model.IntValue = 999;
        model.StringValue = "XYZ";

        history.BeginBatch();
        {
            model.IntValue = 10;
            model.IntValue = 11;
            model.IntValue = 12;

            model.StringValue = "A";
            model.StringValue = "B";
            model.StringValue = "C";
        }
        history.EndBatch();

        history.Undo();

        Assert.Equal(999, model.IntValue);
        Assert.Equal("XYZ", model.StringValue);

        history.Redo();
        Assert.Equal(12, model.IntValue);
        Assert.Equal("C", model.StringValue);
    }

    [Fact]
    public void Empty()
    {
        using var history = new History();

        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);
        Assert.Equal(0, history.UndoCount);
        Assert.Equal(0, history.RedoCount);

        history.BeginBatch();
        {
        }
        history.EndBatch();

        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);
        Assert.Equal(0, history.UndoCount);
        Assert.Equal(0, history.RedoCount);
    }

    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void NestingBatch(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateTestModel(testModelKind, history);

        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);

        model.IntValue = 999;
        model.StringValue = "XYZ";

        history.BeginBatch();
        {
            model.IntValue = 10;

            history.BeginBatch();
            {
                model.IntValue = 11;

                history.BeginBatch();
                {
                    model.IntValue = 12;
                    model.StringValue = "A";
                }
                history.EndBatch();

                model.StringValue = "B";
            }
            history.EndBatch();

            model.StringValue = "C";
        }
        history.EndBatch();

        history.Undo();

        Assert.Equal(999, model.IntValue);
        Assert.Equal("XYZ", model.StringValue);

        history.Redo();
        Assert.Equal(12, model.IntValue);
        Assert.Equal("C", model.StringValue);
    }

    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void Cannot_call_undo_during_batch_recording(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateTestModel(testModelKind, history);

        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);

        model.IntValue = 999;
        model.StringValue = "XYZ";

        history.BeginBatch();

        Assert.Throws<InvalidOperationException>(() =>
            history.Undo()
        );
    }

    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void Cannot_call_redo_during_batch_recording(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateTestModel(testModelKind, history);

        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);

        model.IntValue = 999;
        model.StringValue = "XYZ";

        history.BeginBatch();

        Assert.Throws<InvalidOperationException>(() =>
            history.Redo()
        );
    }

    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void Batch_recording_has_not_begun(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateTestModel(testModelKind, history);

        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);

        model.IntValue = 999;
        model.StringValue = "XYZ";

        Assert.Throws<InvalidOperationException>(() =>
            history.EndBatch()
        );
    }
}