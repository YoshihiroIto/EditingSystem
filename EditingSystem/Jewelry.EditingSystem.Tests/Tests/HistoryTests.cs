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

    [Fact]
    public void RecordPropertyChange_does_not_record_again_during_undo_or_redo()
    {
        using var history = new History();
        var value = 0;

        void SetValue(int newValue)
        {
            if (history.RecordPropertyChange(SetValue, value, newValue))
                value = newValue;
        }

        SetValue(1);

        history.Undo();
        Assert.Equal(0, value);
        Assert.Equal(0, history.UndoCount);
        Assert.Equal(1, history.RedoCount);

        history.Redo();
        Assert.Equal(1, value);
        Assert.Equal(1, history.UndoCount);
        Assert.Equal(0, history.RedoCount);
    }

    [Fact]
    public void TryUndo_and_TryRedo_report_whether_an_action_was_applied()
    {
        using var history = new History();
        var value = 1;

        Assert.False(history.TryUndo());
        Assert.False(history.TryRedo());

        history.Push(() => value = 0, () => value = 1);

        Assert.True(history.TryUndo());
        Assert.Equal(0, value);
        Assert.False(history.TryUndo());

        Assert.True(history.TryRedo());
        Assert.Equal(1, value);
        Assert.False(history.TryRedo());
    }

    [Fact]
    public void Push_rejects_null_actions_immediately()
    {
        using var history = new History();

        Assert.Throws<System.ArgumentNullException>(() => history.Push(null!, () => { }));
        Assert.Throws<System.ArgumentNullException>(() => history.Push(() => { }, null!));
    }

    [Fact]
    public void MaxUndoCount_discards_the_oldest_actions()
    {
        using var history = new History { MaxUndoCount = 2 };
        var value = 0;

        void SetValue(int newValue)
        {
            var oldValue = value;
            history.Push(() => value = oldValue, () => value = newValue);
            value = newValue;
        }

        SetValue(1);
        SetValue(2);
        SetValue(3);

        Assert.Equal(2, history.UndoCount);
        Assert.True(history.TryUndo());
        Assert.Equal(2, value);
        Assert.True(history.TryUndo());
        Assert.Equal(1, value);
        Assert.False(history.TryUndo());

        Assert.True(history.TryRedo());
        Assert.True(history.TryRedo());
        Assert.Equal(3, value);
    }

    [Fact]
    public void Reducing_MaxUndoCount_trims_existing_history()
    {
        using var history = new History();
        history.Push(() => { }, () => { });
        history.Push(() => { }, () => { });
        history.Push(() => { }, () => { });

        history.MaxUndoCount = 1;

        Assert.Equal(1, history.UndoCount);
        Assert.Throws<System.ArgumentOutOfRangeException>(() => history.MaxUndoCount = -1);
    }
}
