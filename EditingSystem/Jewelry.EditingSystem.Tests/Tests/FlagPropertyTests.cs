using Jewelry.EditingSystem.Tests.TestModels;
using Xunit;
using static Jewelry.EditingSystem.Tests.TestModels.TestModelCreator;

namespace Jewelry.EditingSystem.Tests.Tests;

public sealed class FlagPropertyTests
{
    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void BasicByte(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateFlagTestModel(testModelKind, history);

        Assert.False(model.IsA);
        Assert.False(model.IsB);
        Assert.False(model.IsC);
        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);

        model.IsA = true;
        Assert.True(model.IsA);
        Assert.False(model.IsB);
        Assert.False(model.IsC);

        model.IsB = true;
        Assert.True(model.IsA);
        Assert.True(model.IsB);
        Assert.False(model.IsC);

        model.IsC = true;
        Assert.True(model.IsA);
        Assert.True(model.IsB);
        Assert.True(model.IsC);

        history.Undo();
        Assert.True(model.IsA);
        Assert.True(model.IsB);
        Assert.False(model.IsC);

        history.Undo();
        Assert.True(model.IsA);
        Assert.False(model.IsB);
        Assert.False(model.IsC);

        history.Undo();
        Assert.False(model.IsA);
        Assert.False(model.IsB);
        Assert.False(model.IsC);

        history.Redo();
        Assert.True(model.IsA);
        Assert.False(model.IsB);
        Assert.False(model.IsC);

        history.Redo();
        Assert.True(model.IsA);
        Assert.True(model.IsB);
        Assert.False(model.IsC);

        history.Redo();
        Assert.True(model.IsA);
        Assert.True(model.IsB);
        Assert.True(model.IsC);
    }

    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void BasicUint(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateFlagTestModel(testModelKind, history);

        Assert.False(model.IsA);
        Assert.False(model.IsB);
        Assert.False(model.IsC);
        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);

        model.IsA = true;
        Assert.True(model.IsA);
        Assert.False(model.IsB);
        Assert.False(model.IsC);

        model.IsB = true;
        Assert.True(model.IsA);
        Assert.True(model.IsB);
        Assert.False(model.IsC);

        model.IsC = true;
        Assert.True(model.IsA);
        Assert.True(model.IsB);
        Assert.True(model.IsC);

        history.Undo();
        Assert.True(model.IsA);
        Assert.True(model.IsB);
        Assert.False(model.IsC);

        history.Undo();
        Assert.True(model.IsA);
        Assert.False(model.IsB);
        Assert.False(model.IsC);

        history.Undo();
        Assert.False(model.IsA);
        Assert.False(model.IsB);
        Assert.False(model.IsC);

        history.Redo();
        Assert.True(model.IsA);
        Assert.False(model.IsB);
        Assert.False(model.IsC);

        history.Redo();
        Assert.True(model.IsA);
        Assert.True(model.IsB);
        Assert.False(model.IsC);

        history.Redo();
        Assert.True(model.IsA);
        Assert.True(model.IsB);
        Assert.True(model.IsC);
    }

    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void BasicUlong(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateFlagTestModel(testModelKind, history);

        Assert.False(model.IsA);
        Assert.False(model.IsB);
        Assert.False(model.IsC);
        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);

        model.IsA = true;
        Assert.True(model.IsA);
        Assert.False(model.IsB);
        Assert.False(model.IsC);

        model.IsB = true;
        Assert.True(model.IsA);
        Assert.True(model.IsB);
        Assert.False(model.IsC);

        model.IsC = true;
        Assert.True(model.IsA);
        Assert.True(model.IsB);
        Assert.True(model.IsC);

        history.Undo();
        Assert.True(model.IsA);
        Assert.True(model.IsB);
        Assert.False(model.IsC);

        history.Undo();
        Assert.True(model.IsA);
        Assert.False(model.IsB);
        Assert.False(model.IsC);

        history.Undo();
        Assert.False(model.IsA);
        Assert.False(model.IsB);
        Assert.False(model.IsC);

        history.Redo();
        Assert.True(model.IsA);
        Assert.False(model.IsB);
        Assert.False(model.IsC);

        history.Redo();
        Assert.True(model.IsA);
        Assert.True(model.IsB);
        Assert.False(model.IsC);

        history.Redo();
        Assert.True(model.IsA);
        Assert.True(model.IsB);
        Assert.True(model.IsC);
    }
    
    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void ChangingCount(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateFlagTestModel(testModelKind, history);

        model.IsA = true;
        Assert.Equal(1, model.ChangingCount);

        model.IsB = true;
        Assert.Equal(2, model.ChangingCount);

        model.IsB = true;
        Assert.Equal(2, model.ChangingCount);

        model.IsC = true;
        Assert.Equal(3, model.ChangingCount);
    }
}