using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Jewelry.EditingSystem.Tests.TestModels;
using Xunit;
using static Jewelry.EditingSystem.Tests.TestModels.TestModelCreator;

namespace Jewelry.EditingSystem.Tests.Tests;

public sealed class DirtyHistoryTests
{
    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void Real_property_setters_follow_the_saved_position(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateBasicTestModel(testModelKind, history);
        var dirtyNotifications = 0;
        history.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(History.IsDirty))
                ++dirtyNotifications;
        };

        Assert.False(history.IsDirty);

        model.IntValue = 1;
        Assert.True(history.IsDirty);

        history.MarkSaved();
        Assert.False(history.IsDirty);

        model.IntValue = 2;
        Assert.True(history.IsDirty);

        history.Undo();
        Assert.Equal(1, model.IntValue);
        Assert.False(history.IsDirty);

        history.Undo();
        Assert.Equal(0, model.IntValue);
        Assert.True(history.IsDirty);

        history.Redo();
        Assert.False(history.IsDirty);
        history.Redo();
        Assert.True(history.IsDirty);
        Assert.Equal(7, dirtyNotifications);
    }

    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void Editing_after_undo_creates_a_distinct_branch(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateBasicTestModel(testModelKind, history);

        model.IntValue = 1;
        history.MarkSaved();
        model.IntValue = 2;
        history.Undo();

        Assert.False(history.IsDirty);

        model.IntValue = 3;

        Assert.True(history.IsDirty);
        Assert.False(history.CanRedo);
        history.Undo();
        Assert.Equal(1, model.IntValue);
        Assert.False(history.IsDirty);
    }

    [Fact]
    public void MarkDirty_is_a_latch_until_MarkSaved()
    {
        using var history = new History();
        var value = 0;
        var dirtyNotifications = new List<bool>();
        history.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(History.IsDirty))
                dirtyNotifications.Add(history.IsDirty);
        };

        history.Push(() => value = 0, () => value = 1);
        value = 1;
        history.MarkSaved();
        history.MarkDirty();
        history.MarkDirty();

        history.Undo();
        Assert.Equal(0, value);
        Assert.True(history.IsDirty);
        history.Redo();
        Assert.Equal(1, value);
        Assert.True(history.IsDirty);

        history.MarkSaved();

        Assert.False(history.IsDirty);
        Assert.Equal([true, false, true, false], dirtyNotifications);
    }

    [Fact]
    public void Clear_discards_history_without_changing_dirty_state()
    {
        using var history = new History();
        history.Push(static () => { }, static () => { });

        history.Clear();

        Assert.True(history.IsDirty);
        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);

        history.MarkSaved();
        history.Clear();

        Assert.False(history.IsDirty);
    }

    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void Paused_changes_do_not_mark_history_dirty(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateBasicTestModel(testModelKind, history);

        using (history.Pause())
            model.IntValue = 1;

        Assert.Equal(1, model.IntValue);
        Assert.False(history.IsDirty);
        Assert.False(history.CanUndo);
    }

    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void MaxUndoCount_zero_still_marks_a_real_property_change_dirty(TestModelKinds testModelKind)
    {
        using var history = new History { MaxUndoCount = 0 };
        var model = CreateBasicTestModel(testModelKind, history);

        model.IntValue = 1;

        Assert.True(history.IsDirty);
        Assert.False(history.CanUndo);
    }

    [Fact]
    public void Trimming_away_the_saved_position_does_not_make_the_current_state_clean()
    {
        using var history = new History();
        var value = 0;

        SetValue(1);
        history.MarkSaved();
        SetValue(2);
        SetValue(3);
        history.MaxUndoCount = 1;

        history.Undo();

        Assert.Equal(2, value);
        Assert.True(history.IsDirty);
        Assert.False(history.CanUndo);

        void SetValue(int newValue)
        {
            var oldValue = value;
            history.Push(() => value = oldValue, () => value = newValue);
            value = newValue;
        }
    }

    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void Batch_and_coalescing_batch_advance_dirty_only_when_finalized(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateBasicTestModel(testModelKind, history);

        history.BeginBatch();
        history.BeginBatch();
        model.IntValue = 1;
        Assert.False(history.IsDirty);
        history.EndBatch();
        Assert.False(history.IsDirty);
        history.EndBatch();
        Assert.True(history.IsDirty);

        history.MarkSaved();
        history.BeginCoalescingBatch();
        model.IntValue = 2;
        model.IntValue = 1;
        Assert.False(history.IsDirty);
        history.EndCoalescingBatch();
        Assert.False(history.IsDirty);
    }

    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void Transaction_commit_advances_dirty_but_rollback_and_empty_commit_do_not(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateBasicTestModel(testModelKind, history);

        using (var transaction = history.BeginTransaction())
        {
            model.IntValue = 1;
            Assert.False(history.IsDirty);
            transaction.Commit();
        }

        Assert.True(history.IsDirty);
        history.MarkSaved();

        using (history.BeginTransaction())
            model.IntValue = 2;

        Assert.Equal(1, model.IntValue);
        Assert.False(history.IsDirty);

        using (var transaction = history.BeginTransaction())
            transaction.Commit();

        Assert.False(history.IsDirty);

        using (var transaction = history.BeginTransaction())
        {
            model.IntValue = 2;
            history.MarkDirty();
            transaction.Rollback();
        }

        Assert.Equal(1, model.IntValue);
        Assert.True(history.IsDirty);
        history.MarkSaved();
        Assert.False(history.IsDirty);
    }

    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void Nested_transaction_changes_advance_dirty_only_with_the_outer_commit(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateBasicTestModel(testModelKind, history);

        using (var outer = history.BeginTransaction())
        {
            model.IntValue = 1;
            using (var inner = history.BeginTransaction())
            {
                model.StringValue = "committed";
                inner.Commit();
            }

            Assert.False(history.IsDirty);
            outer.Commit();
        }

        Assert.True(history.IsDirty);
        history.MarkSaved();

        using (var outer = history.BeginTransaction())
        {
            model.IntValue = 2;
            using (var inner = history.BeginTransaction())
            {
                model.StringValue = "rolled back";
                inner.Commit();
            }

            outer.Rollback();
        }

        Assert.Equal(1, model.IntValue);
        Assert.Equal("committed", model.StringValue);
        Assert.False(history.IsDirty);
    }

    [Fact]
    public void MarkSaved_is_rejected_during_batch_and_transaction()
    {
        using var history = new History();

        using (history.Batch())
            Assert.Throws<InvalidOperationException>(() => history.MarkSaved());

        using var transaction = history.BeginTransaction();
        Assert.Throws<InvalidOperationException>(() => history.MarkSaved());
        transaction.Rollback();
    }

    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void Failed_transaction_commit_rolls_back_without_changing_dirty(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateBasicTestModel(testModelKind, history);
        history.TransactionCommitting += static (_, _) => throw new InvalidOperationException("validation failed");

        using var transaction = history.BeginTransaction();
        model.IntValue = 1;

        Assert.Throws<InvalidOperationException>(() => transaction.Commit());
        Assert.Equal(0, model.IntValue);
        Assert.False(history.IsDirty);
    }

    [Fact]
    public void Failed_undo_does_not_move_the_dirty_position()
    {
        using var history = new History();
        history.Push(
            static () => throw new InvalidOperationException("undo failed"),
            static () => { });
        history.MarkSaved();

        Assert.Throws<InvalidOperationException>(() => history.Undo());

        Assert.False(history.IsDirty);
        Assert.True(history.CanUndo);
    }

    [Fact]
    public void Failed_redo_does_not_move_the_dirty_position()
    {
        using var history = new History();
        history.Push(
            static () => { },
            static () => throw new InvalidOperationException("redo failed"));
        history.MarkSaved();
        history.Undo();
        Assert.True(history.IsDirty);

        Assert.Throws<InvalidOperationException>(() => history.Redo());

        Assert.True(history.IsDirty);
        Assert.True(history.CanRedo);
    }

    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void Observed_collection_changes_follow_the_saved_position(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateBasicTestModel(testModelKind, history);
        using (history.Pause())
            model.IntCollection = new ObservableCollection<int>([1]);
        history.MarkSaved();

        model.IntCollection.Add(2);
        Assert.True(history.IsDirty);

        history.Undo();

        Assert.Equal([1], model.IntCollection);
        Assert.False(history.IsDirty);
    }
}
