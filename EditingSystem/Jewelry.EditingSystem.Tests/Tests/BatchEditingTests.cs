using System;
using Jewelry.EditingSystem.Tests.TestModels;
using Xunit;
using static Jewelry.EditingSystem.Tests.TestModels.TestModelCreator;

namespace Jewelry.EditingSystem.Tests.Tests;

public sealed class BatchEditingTests
{
    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void Basic(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateBasicTestModel(testModelKind, history);

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
        var model = CreateBasicTestModel(testModelKind, history);

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
        var model = CreateBasicTestModel(testModelKind, history);

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
        var model = CreateBasicTestModel(testModelKind, history);

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
        var model = CreateBasicTestModel(testModelKind, history);

        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);

        model.IntValue = 999;
        model.StringValue = "XYZ";

        Assert.Throws<InvalidOperationException>(() =>
            history.EndBatch()
        );
    }

    [Fact]
    public void Batch_scope_is_balanced_when_an_exception_is_thrown()
    {
        using var history = new History();

        Assert.Throws<InvalidOperationException>((Action)(() =>
        {
            using (history.Batch())
            {
                Assert.True(history.IsInBatch);
                throw new InvalidOperationException();
            }
        }));

        Assert.False(history.IsInBatch);
        Assert.Equal(0, history.BatchDepth);
    }

    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void Regular_batch_still_replays_every_intermediate_change(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateBasicTestModel(testModelKind, history);

        using (history.Batch())
        {
            model.IntValue = 1;
            model.IntValue = 2;
            model.IntValue = 3;
        }

        var notificationCount = 0;
        model.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(model.IntValue))
                ++notificationCount;
        };

        history.Undo();
        Assert.Equal(0, model.IntValue);
        Assert.Equal(3, notificationCount);

        notificationCount = 0;
        history.Redo();
        Assert.Equal(3, model.IntValue);
        Assert.Equal(3, notificationCount);
    }

    [Fact]
    public void Failed_batch_undo_resumes_without_replaying_actions_that_already_succeeded()
    {
        using var history = new History();
        var first = 1;
        var second = 1;
        var third = 1;
        var thirdUndoCount = 0;
        var failSecondUndo = true;

        using (history.Batch())
        {
            history.Push(() => first = 0, () => first = 1);
            history.Push(
                () =>
                {
                    if (failSecondUndo)
                    {
                        failSecondUndo = false;
                        throw new InvalidOperationException("expected");
                    }
                    second = 0;
                },
                () => second = 1);
            history.Push(
                () =>
                {
                    ++thirdUndoCount;
                    third = 0;
                },
                () => third = 1);
        }

        Assert.Throws<InvalidOperationException>(() => history.Undo());
        Assert.Equal(1, first);
        Assert.Equal(1, second);
        Assert.Equal(0, third);
        Assert.Equal(1, thirdUndoCount);

        history.Undo();
        Assert.Equal(0, first);
        Assert.Equal(0, second);
        Assert.Equal(0, third);
        Assert.Equal(1, thirdUndoCount);
    }

    [Fact]
    public void Failed_batch_redo_resumes_without_replaying_actions_that_already_succeeded()
    {
        using var history = new History();
        var first = 1;
        var second = 1;
        var third = 1;
        var firstRedoCount = 0;
        var failSecondRedo = false;

        using (history.Batch())
        {
            history.Push(() => first = 0, () =>
            {
                ++firstRedoCount;
                first = 1;
            });
            history.Push(() => second = 0, () =>
            {
                if (failSecondRedo)
                {
                    failSecondRedo = false;
                    throw new InvalidOperationException("expected");
                }
                second = 1;
            });
            history.Push(() => third = 0, () => third = 1);
        }

        history.Undo();
        failSecondRedo = true;

        Assert.Throws<InvalidOperationException>(() => history.Redo());
        Assert.Equal(1, first);
        Assert.Equal(0, second);
        Assert.Equal(0, third);
        Assert.Equal(1, firstRedoCount);

        history.Redo();
        Assert.Equal(1, first);
        Assert.Equal(1, second);
        Assert.Equal(1, third);
        Assert.Equal(1, firstRedoCount);
    }
}
