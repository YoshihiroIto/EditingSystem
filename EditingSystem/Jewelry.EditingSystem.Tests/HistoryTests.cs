using Xunit;

namespace Jewelry.EditingSystem.Tests;

public sealed class HistoryTests
{
    [Fact]
    public void Undoable_if_CanUndo_is_false()
    {
        var history = new History();

        Assert.False(history.CanUndo);
        history.Undo();
    }

    [Fact]
    public void Redoable_if_CanRedo_is_false()
    {
        var history = new History();

        Assert.False(history.CanRedo);
        history.Redo();
    }
}