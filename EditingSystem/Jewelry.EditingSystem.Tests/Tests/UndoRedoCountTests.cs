using Jewelry.EditingSystem.Tests.TestModels;
using Xunit;
using static Jewelry.EditingSystem.Tests.TestModels.TestModelCreator;

namespace Jewelry.EditingSystem.Tests;

public sealed class UndoRedoCountTests
{
    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void Basic(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateTestModel(testModelKind, history);

        Assert.Equal(0, history.UndoCount);
        Assert.Equal(0, history.RedoCount);

        model.IntValue = 123;
        Assert.Equal(1, history.UndoCount);
        Assert.Equal(0, history.RedoCount);

        model.IntValue = 456;
        Assert.Equal(2, history.UndoCount);
        Assert.Equal(0, history.RedoCount);

        history.Undo();
        Assert.Equal(1, history.UndoCount);
        Assert.Equal(1, history.RedoCount);

        history.Clear();
        Assert.Equal(0, history.UndoCount);
        Assert.Equal(0, history.RedoCount);
    }
}