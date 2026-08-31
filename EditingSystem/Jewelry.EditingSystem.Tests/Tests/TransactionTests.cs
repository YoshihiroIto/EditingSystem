using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Jewelry.EditingSystem.Tests.TestModels;
using Xunit;
using static Jewelry.EditingSystem.Tests.TestModels.TestModelCreator;

namespace Jewelry.EditingSystem.Tests.Tests;

public sealed class TransactionTests
{
    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void Commit_records_real_property_setters_as_one_undo_action(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateBasicTestModel(testModelKind, history);

        using (var transaction = history.BeginTransaction())
        {
            model.IntValue = 10;
            model.StringValue = "committed";
            transaction.Commit();
        }

        Assert.Equal(1, history.UndoCount);
        Assert.Equal(0, history.RedoCount);
        history.Undo();
        Assert.Equal(0, model.IntValue);
        Assert.Equal(string.Empty, model.StringValue);
        history.Redo();
        Assert.Equal(10, model.IntValue);
        Assert.Equal("committed", model.StringValue);
    }

    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void Uncommitted_dispose_rolls_back_property_and_collection_changes(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateBasicTestModel(testModelKind, history);
        model.IntCollection = new ObservableCollection<int>([10, 20, 30]);
        history.Clear();

        using (history.BeginTransaction())
        {
            model.IntValue = 42;
            model.IntCollection.Add(40);
            model.IntCollection.Move(0, 2);
        }

        Assert.Equal(0, model.IntValue);
        Assert.Equal([10, 20, 30], model.IntCollection);
        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);
    }

    [Fact]
    public void Explicit_rollback_preserves_the_existing_redo_stack()
    {
        using var history = new History();
        var value = 0;
        SetValue(1);
        history.Undo();

        using (var transaction = history.BeginTransaction())
        {
            SetValue(2);
            transaction.Rollback();
        }

        Assert.Equal(0, value);
        Assert.Equal(0, history.UndoCount);
        Assert.Equal(1, history.RedoCount);
        history.Redo();
        Assert.Equal(1, value);

        void SetValue(int newValue)
        {
            var oldValue = value;
            history.Push(() => value = oldValue, () => value = newValue);
            value = newValue;
        }
    }

    [Fact]
    public void Commit_clears_the_existing_redo_stack_but_empty_commit_does_not()
    {
        using var history = new History();
        history.Push(static () => { }, static () => { });
        history.Undo();

        using (var empty = history.BeginTransaction())
            empty.Commit();

        Assert.Equal(1, history.RedoCount);

        using (var transaction = history.BeginTransaction())
        {
            history.Push(static () => { }, static () => { });
            transaction.Commit();
        }

        Assert.Equal(1, history.UndoCount);
        Assert.Equal(0, history.RedoCount);
    }

    [Fact]
    public void MaxUndoCount_zero_still_records_enough_to_rollback()
    {
        using var history = new History { MaxUndoCount = 0 };
        var value = 0;

        using (history.BeginTransaction())
        {
            history.Push(() => value = 0, () => value = 1);
            value = 1;
        }

        Assert.Equal(0, value);
        Assert.False(history.CanUndo);

        using (var transaction = history.BeginTransaction())
        {
            history.Push(() => value = 0, () => value = 2);
            value = 2;
            transaction.Commit();
        }

        Assert.Equal(2, value);
        Assert.False(history.CanUndo);
    }

    [Fact]
    public void Nested_transactions_are_savepoints()
    {
        using var history = new History();
        var values = new List<int>();

        using (var outer = history.BeginTransaction())
        {
            Add(1);

            using (var rolledBackInner = history.BeginTransaction())
            {
                Add(2);
                rolledBackInner.Rollback();
            }

            using (var committedInner = history.BeginTransaction())
            {
                Add(3);
                committedInner.Commit();
            }

            Assert.Equal([1, 3], values);
            outer.Rollback();
        }

        Assert.Empty(values);
        Assert.False(history.CanUndo);

        void Add(int value)
        {
            history.Push(() => values.RemoveAt(values.Count - 1), () => values.Add(value));
            values.Add(value);
        }
    }

    [Fact]
    public void Nested_commit_is_one_action_in_the_outer_history()
    {
        using var history = new History();
        var value = 0;

        using (var outer = history.BeginTransaction())
        {
            SetValue(1);
            using (var inner = history.BeginTransaction())
            {
                SetValue(2);
                inner.Commit();
            }
            outer.Commit();
        }

        Assert.Equal(1, history.UndoCount);
        history.Undo();
        Assert.Equal(0, value);
        history.Redo();
        Assert.Equal(2, value);

        void SetValue(int newValue)
        {
            var oldValue = value;
            history.Push(() => value = oldValue, () => value = newValue);
            value = newValue;
        }
    }

    [Fact]
    public void Transactions_must_complete_in_reverse_order_and_only_once()
    {
        using var history = new History();
        using var outer = history.BeginTransaction();
        using var inner = history.BeginTransaction();

        Assert.Throws<InvalidOperationException>(() => outer.Commit());
        inner.Commit();
        outer.Commit();
        Assert.Throws<InvalidOperationException>(() => outer.Commit());
        Assert.Throws<InvalidOperationException>(() => outer.Rollback());
    }

    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void Batch_and_coalescing_batch_can_participate_in_a_transaction(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateBasicTestModel(testModelKind, history);

        using (var transaction = history.BeginTransaction())
        {
            using (history.Batch())
            {
                model.StringValue = "A";
                model.StringValue = "B";
            }

            using (history.CoalescingBatch())
            {
                model.IntValue = 1;
                model.IntValue = 2;
                model.IntValue = 3;
            }

            transaction.Commit();
        }

        Assert.Equal(1, history.UndoCount);
        history.Undo();
        Assert.Equal(string.Empty, model.StringValue);
        Assert.Equal(0, model.IntValue);
        history.Redo();
        Assert.Equal("B", model.StringValue);
        Assert.Equal(3, model.IntValue);
    }

    [Fact]
    public void Transaction_and_pause_or_outer_batch_combinations_are_rejected()
    {
        using var history = new History();

        using (history.Pause())
            Assert.Throws<InvalidOperationException>(() => history.BeginTransaction());

        using (history.Batch())
            Assert.Throws<InvalidOperationException>(() => history.BeginTransaction());

        using (var transaction = history.BeginTransaction())
        {
            Assert.Throws<InvalidOperationException>(() => history.BeginPause());
            transaction.Rollback();
        }
    }

    [Fact]
    public void History_stack_operations_are_rejected_during_a_transaction()
    {
        using var history = new History();
        history.Push(() => { }, () => { });

        using var transaction = history.BeginTransaction();
        Assert.Throws<InvalidOperationException>(() => history.Undo());
        Assert.Throws<InvalidOperationException>(() => history.TryUndo());
        Assert.Throws<InvalidOperationException>(() => history.Redo());
        Assert.Throws<InvalidOperationException>(() => history.TryRedo());
        Assert.Throws<InvalidOperationException>(() => history.Clear());
        transaction.Rollback();
    }

    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void Lifecycle_events_fire_for_the_outermost_transaction_and_include_callback_changes(
        TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateBasicTestModel(testModelKind, history);
        var events = new List<string>();

        history.TransactionBeginning += (_, _) =>
        {
            events.Add("Beginning");
            model.IntValue = 1;
        };
        history.TransactionCommitting += (_, _) =>
        {
            events.Add("Committing");
            model.IntValue = 3;
        };
        history.TransactionCommitted += (_, _) => events.Add("Committed");
        history.TransactionRolledBack += (_, _) => events.Add("RolledBack");

        using (var outer = history.BeginTransaction())
        {
            model.IntValue = 2;
            using (var inner = history.BeginTransaction())
                inner.Commit();
            outer.Commit();
        }

        Assert.Equal(["Beginning", "Committing", "Committed"], events);
        Assert.Equal(3, model.IntValue);
        history.Undo();
        Assert.Equal(0, model.IntValue);
    }

    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void Committing_exception_rolls_back_and_rethrows(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateBasicTestModel(testModelKind, history);
        var events = new List<string>();
        history.TransactionCommitting += (_, _) =>
        {
            model.IntValue = 2;
            throw new InvalidOperationException("validation failed");
        };
        history.TransactionRolledBack += (_, _) => events.Add("RolledBack");

        using var transaction = history.BeginTransaction();
        model.IntValue = 1;
        var exception = Assert.Throws<InvalidOperationException>(() => transaction.Commit());

        Assert.Equal("validation failed", exception.Message);
        Assert.Equal(0, model.IntValue);
        Assert.Equal(["RolledBack"], events);
        Assert.False(history.IsInTransaction);
        Assert.False(history.CanUndo);
    }

    [Fact]
    public void Beginning_exception_rolls_back_callback_changes()
    {
        using var history = new History();
        var value = 0;
        history.TransactionBeginning += (_, _) =>
        {
            history.Push(() => value = 0, () => value = 1);
            value = 1;
            throw new InvalidOperationException("beginning failed");
        };

        var exception = Assert.Throws<InvalidOperationException>(() => history.BeginTransaction());

        Assert.Equal("beginning failed", exception.Message);
        Assert.Equal(0, value);
        Assert.False(history.IsInTransaction);
        Assert.False(history.CanUndo);
    }

    [Fact]
    public void Post_completion_event_exceptions_do_not_change_the_completed_state()
    {
        using (var history = new History())
        {
            var value = 0;
            history.TransactionCommitted += (_, _) => throw new InvalidOperationException("committed event");
            using var transaction = history.BeginTransaction();
            history.Push(() => value = 0, () => value = 1);
            value = 1;

            var exception = Assert.Throws<InvalidOperationException>(() => transaction.Commit());

            Assert.Equal("committed event", exception.Message);
            Assert.False(history.IsInTransaction);
            Assert.Equal(1, history.UndoCount);
            history.Undo();
            Assert.Equal(0, value);
        }

        using (var history = new History())
        {
            var value = 0;
            history.TransactionRolledBack += (_, _) => throw new InvalidOperationException("rolled back event");
            using var transaction = history.BeginTransaction();
            history.Push(() => value = 0, () => value = 1);
            value = 1;

            var exception = Assert.Throws<InvalidOperationException>(() => transaction.Rollback());

            Assert.Equal("rolled back event", exception.Message);
            Assert.False(history.IsInTransaction);
            Assert.Equal(0, value);
            Assert.False(history.CanUndo);
        }
    }

    [Fact]
    public void Transaction_state_changes_raise_property_changed_only_for_the_outer_boundary()
    {
        using var history = new History();
        var propertyNames = new List<string?>();
        history.PropertyChanged += (_, e) => propertyNames.Add(e.PropertyName);

        using (var outer = history.BeginTransaction())
        {
            using (var inner = history.BeginTransaction())
                inner.Commit();
            outer.Rollback();
        }

        Assert.Equal(
            [nameof(History.IsInTransaction), nameof(History.IsInTransaction)],
            propertyNames);
    }

    [Fact]
    public void Failed_rollback_resumes_without_replaying_actions_that_already_succeeded()
    {
        using var history = new History();
        var first = 1;
        var second = 1;
        var third = 1;
        var thirdUndoCount = 0;
        var failSecondUndo = true;
        var transaction = history.BeginTransaction();

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

        Assert.Throws<InvalidOperationException>(() => transaction.Rollback());
        Assert.Equal(1, first);
        Assert.Equal(1, second);
        Assert.Equal(0, third);
        Assert.Equal(1, thirdUndoCount);
        Assert.True(history.IsInTransaction);

        transaction.Rollback();
        transaction.Dispose();
        Assert.Equal(0, first);
        Assert.Equal(0, second);
        Assert.Equal(0, third);
        Assert.Equal(1, thirdUndoCount);
        Assert.False(history.IsInTransaction);
    }

    [Fact]
    public void Completing_a_transaction_with_an_active_batch_is_rejected()
    {
        using var history = new History();
        using var transaction = history.BeginTransaction();
        history.BeginBatch();

        Assert.Throws<InvalidOperationException>(() => transaction.Commit());
        Assert.Throws<InvalidOperationException>(() => transaction.Rollback());

        history.EndBatch();
        transaction.Rollback();
    }
}
