using System;
using Jewelry.EditingSystem.Tests.TestModels;
using Xunit;
using static Jewelry.EditingSystem.Tests.TestModels.TestModelCreator;

namespace Jewelry.EditingSystem.Tests.Tests;

public sealed class PauseEditingTests
{
    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void Basic(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateBasicTestModel(testModelKind, history);

        history.BeginPause();
        {
            model.IntValue = 10;
            model.IntValue = 11;
            model.IntValue = 12;

            model.StringValue = "A";
            model.StringValue = "B";
            model.StringValue = "C";
        }
        history.EndPause();

        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);
    }

    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void NestingPause(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateBasicTestModel(testModelKind, history);

        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);

        history.BeginPause();
        {
            model.IntValue = 10;

            history.BeginPause();
            {
                model.IntValue = 11;

                history.BeginPause();
                {
                    model.IntValue = 12;
                    model.StringValue = "A";
                }
                history.EndPause();

                model.StringValue = "B";
            }
            history.EndPause();

            model.StringValue = "C";
        }
        history.EndPause();

        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);
    }

    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void Cannot_call_undo_during_pause(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateBasicTestModel(testModelKind, history);

        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);

        model.IntValue = 999;
        model.StringValue = "XYZ";

        history.BeginPause();

        Assert.Throws<InvalidOperationException>(() =>
            history.Undo()
        );
    }

    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void Cannot_call_redo_during_pause(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateBasicTestModel(testModelKind, history);

        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);

        model.IntValue = 999;
        model.StringValue = "XYZ";

        history.BeginPause();

        Assert.Throws<InvalidOperationException>(() =>
            history.Redo()
        );
    }

    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void Pause_has_not_begun(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateBasicTestModel(testModelKind, history);

        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);

        model.IntValue = 999;
        model.StringValue = "XYZ";

        Assert.Throws<InvalidOperationException>(() =>
            history.EndPause()
        );
    }
}