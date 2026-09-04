using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
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
    public void Pause_and_batch_state_changes_raise_property_changed()
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
                nameof(History.IsInPaused),
                nameof(History.IsInPaused),
                nameof(History.IsInBatch),
                nameof(History.IsInBatch)
            ],
            propertyNames);
    }

    [Fact]
    public void Dispose_releases_all_event_subscribers()
    {
        var history = new History();
        var subscriberReference = AddSubscriber(history);

        history.Dispose();
        CollectGarbage();

        Assert.False(subscriberReference.IsAlive);
    }

    [Fact]
    public void Pause_and_batch_depth_are_not_public_api()
    {
        const System.Reflection.BindingFlags publicInstance =
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public;

        Assert.Null(typeof(History).GetProperty(nameof(History.PauseDepth), publicInstance));
        Assert.Null(typeof(History).GetProperty(nameof(History.BatchDepth), publicInstance));
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

    [Fact]
    public void Reducing_MaxUndoCount_releases_excess_history_array_capacity()
    {
        using var history = new History();
        for (var i = 0; i < 1_024; ++i)
            history.Push(static () => { }, static () => { });

        Assert.True(GetUndoCapacity(history) >= 1_024);

        history.MaxUndoCount = 8;

        Assert.Equal(8, history.UndoCount);
        Assert.Equal(8, GetUndoCapacity(history));
    }

    private static int GetUndoCapacity(History history)
    {
        var undoStackField = typeof(History).GetField(
            "_undoStack",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?? throw new System.InvalidOperationException("History._undoStack was not found.");
        var stack = undoStackField.GetValue(history)
            ?? throw new System.InvalidOperationException("History._undoStack was null.");
        var itemsField = stack.GetType().GetField(
            "_items",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?? throw new System.InvalidOperationException("HistoryStack._items was not found.");
        var items = itemsField.GetValue(stack) as System.Array
            ?? throw new System.InvalidOperationException("HistoryStack._items was not an array.");
        return items.Length;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference AddSubscriber(History history)
    {
        var subscriber = new HistoryEventSubscriber();
        history.PropertyChanged += subscriber.OnPropertyChanged;
        history.TransactionBeginning += subscriber.OnTransaction;
        history.TransactionCommitting += subscriber.OnTransaction;
        history.TransactionCommitted += subscriber.OnTransaction;
        history.TransactionRolledBack += subscriber.OnTransaction;
        return new WeakReference(subscriber);
    }

    private static void CollectGarbage()
    {
        for (var i = 0; i < 3; ++i)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }

    private sealed class HistoryEventSubscriber
    {
        public void OnPropertyChanged(object? sender, PropertyChangedEventArgs e) { }
        public void OnTransaction(object? sender, EventArgs e) { }
    }
}
