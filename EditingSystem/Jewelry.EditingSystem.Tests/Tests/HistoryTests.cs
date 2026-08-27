using Xunit;

namespace Jewelry.EditingSystem.Tests.Tests;

public sealed class HistoryTests
{
    [Fact]
    public void Undoable_if_CanUndo_is_false()
    {
        using var history = new History();

        Assert.False(history.CanUndo);
        history.Undo();
    }

    [Fact]
    public void Redoable_if_CanRedo_is_false()
    {
        using var history = new History();

        Assert.False(history.CanRedo);
        history.Redo();
    }

    [Fact]
    public void Failed_undo_remains_available()
    {
        using var history = new History();
        history.Push(
            () => throw new System.InvalidOperationException("Undo failed."),
            () => { });

        Assert.Throws<System.InvalidOperationException>(() => history.Undo());

        Assert.Equal(1, history.UndoCount);
        Assert.Equal(0, history.RedoCount);
        Assert.True(history.CanUndo);
    }

    [Fact]
    public void Failed_redo_remains_available()
    {
        using var history = new History();
        history.Push(
            () => { },
            () => throw new System.InvalidOperationException("Redo failed."));
        history.Undo();

        Assert.Throws<System.InvalidOperationException>(() => history.Redo());

        Assert.Equal(0, history.UndoCount);
        Assert.Equal(1, history.RedoCount);
        Assert.True(history.CanRedo);
    }

    [Fact]
    public void Pause_and_batch_depth_changes_raise_property_changed()
    {
        using var history = new History();
        var propertyNames = new System.Collections.Generic.List<string?>();
        history.PropertyChanged += (_, e) => propertyNames.Add(e.PropertyName);

        history.BeginPause();
        history.EndPause();
        history.BeginBatch();
        history.EndBatch();

        Assert.Equal(
            [
                nameof(History.PauseDepth),
                nameof(History.IsInPaused),
                nameof(History.PauseDepth),
                nameof(History.IsInPaused),
                nameof(History.BatchDepth),
                nameof(History.IsInBatch),
                nameof(History.BatchDepth),
                nameof(History.IsInBatch)
            ],
            propertyNames);
    }
}
