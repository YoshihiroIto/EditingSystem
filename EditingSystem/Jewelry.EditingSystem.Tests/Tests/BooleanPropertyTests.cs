using Jewelry.EditingSystem.Tests.TestModels;
using Xunit;
using static Jewelry.EditingSystem.Tests.TestModels.TestModelCreator;

namespace Jewelry.EditingSystem.Tests.Tests;

public sealed class BooleanPropertyTests
{
    [Fact]
    public void Throwing_setter_does_not_leave_phantom_history()
    {
        using var history = new History();
        var model = new ThrowingBooleanModel(history);

        Assert.Throws<System.InvalidOperationException>(() => model.IsEnabled = true);

        Assert.False(model.IsEnabled);
        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);
    }

    [Theory]
    [InlineData(TestModelKinds.EditableModel)]
    [InlineData(TestModelKinds.Direct)]
    public void Basic_boolean_history(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateFlagTestModel(testModelKind, history);

        Assert.False(model.IsA);
        Assert.False(model.IsB);
        Assert.False(model.IsC);
        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);

        model.IsA = true;
        model.IsB = true;
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
        history.Redo();
        history.Redo();

        Assert.True(model.IsA);
        Assert.True(model.IsB);
        Assert.True(model.IsC);
    }

    [Theory]
    [InlineData(TestModelKinds.EditableModel)]
    [InlineData(TestModelKinds.Direct)]
    public void Reassigning_the_same_boolean_does_not_create_history(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateFlagTestModel(testModelKind, history);

        model.IsA = true;
        history.Clear();
        var changingCount = model.ChangingCount;

        model.IsA = true;

        Assert.Equal(changingCount, model.ChangingCount);
        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);
    }

    [Theory]
    [InlineData(TestModelKinds.EditableModel)]
    [InlineData(TestModelKinds.Direct)]
    public void Coalescing_batch_drops_net_no_op_boolean_change(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateFlagTestModel(testModelKind, history);

        using (history.CoalescingBatch())
        {
            model.IsA = true;
            model.IsA = false;
        }

        Assert.False(model.IsA);
        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);
    }

    private sealed class ThrowingBooleanModel(History history) : EditableModelBase(history)
    {
        public bool IsEnabled
        {
            get => false;
            set => SetEditableProperty(
                _ => throw new System.InvalidOperationException(),
                false,
                value);
        }
    }
}
