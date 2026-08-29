using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Jewelry.EditingSystem.Tests.TestModels;
using Xunit;
using static Jewelry.EditingSystem.Tests.TestModels.TestModelCreator;

namespace Jewelry.EditingSystem.Tests.Tests;

public sealed class CoalescingBatchTests
{
    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void One_thousand_changes_are_replayed_as_one_change(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateBasicTestModel(testModelKind, history);

        using (history.CoalescingBatch())
        {
            for (var value = 1; value <= 1_000; ++value)
                model.IntValue = value;
        }

        var changedProperties = ObservePropertyChanges(model);
        history.Undo();
        Assert.Equal(0, model.IntValue);
        Assert.Equal([nameof(model.IntValue)], changedProperties);

        changedProperties.Clear();
        history.Redo();
        Assert.Equal(1_000, model.IntValue);
        Assert.Equal([nameof(model.IntValue)], changedProperties);
    }

    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void Repeated_changes_to_multiple_properties_are_replayed_once(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateBasicTestModel(testModelKind, history);
        model.IntValue = 100;
        model.StringValue = "before";
        history.Clear();

        history.BeginCoalescingBatch();
        model.IntValue = 101;
        model.StringValue = "one";
        model.IntValue = 102;
        model.StringValue = "two";
        model.IntValue = 103;
        history.EndCoalescingBatch();

        Assert.Equal(1, history.UndoCount);

        var changedProperties = ObservePropertyChanges(model);
        history.Undo();

        Assert.Equal(100, model.IntValue);
        Assert.Equal("before", model.StringValue);
        Assert.Single(changedProperties, x => x == nameof(model.IntValue));
        Assert.Single(changedProperties, x => x == nameof(model.StringValue));

        changedProperties.Clear();
        history.Redo();

        Assert.Equal(103, model.IntValue);
        Assert.Equal("two", model.StringValue);
        Assert.Single(changedProperties, x => x == nameof(model.IntValue));
        Assert.Single(changedProperties, x => x == nameof(model.StringValue));
    }

    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void Same_property_on_multiple_targets_is_coalesced_independently(TestModelKinds testModelKind)
    {
        using var history = new History();
        var first = CreateBasicTestModel(testModelKind, history);
        var second = CreateBasicTestModel(testModelKind, history);
        first.IntValue = 10;
        second.IntValue = 20;
        history.Clear();

        using (history.CoalescingBatch())
        {
            first.IntValue = 11;
            second.IntValue = 21;
            first.IntValue = 12;
            second.IntValue = 22;
        }

        var firstChanges = ObservePropertyChanges(first);
        var secondChanges = ObservePropertyChanges(second);
        history.Undo();

        Assert.Equal(10, first.IntValue);
        Assert.Equal(20, second.IntValue);
        Assert.Equal([nameof(first.IntValue)], firstChanges);
        Assert.Equal([nameof(second.IntValue)], secondChanges);

        firstChanges.Clear();
        secondChanges.Clear();
        history.Redo();

        Assert.Equal(12, first.IntValue);
        Assert.Equal(22, second.IntValue);
        Assert.Equal([nameof(first.IntValue)], firstChanges);
        Assert.Equal([nameof(second.IntValue)], secondChanges);
    }

    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void Returning_to_the_initial_value_does_not_create_history(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateBasicTestModel(testModelKind, history);

        using (history.CoalescingBatch())
        {
            model.IntValue = 10;
            model.IntValue = 20;
            model.IntValue = 0;
        }

        Assert.False(history.CanUndo);
        Assert.Equal(0, history.UndoCount);
    }

    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void Collection_change_is_a_coalescing_barrier(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateBasicTestModel(testModelKind, history);
        model.IntCollection = new ObservableCollection<int>();
        history.Clear();

        history.BeginCoalescingBatch();
        model.IntValue = 1;
        model.IntCollection.Add(10);
        model.IntValue = 2;
        history.EndCoalescingBatch();

        var replayOrder = new List<string>();
        model.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(model.IntValue))
                replayOrder.Add($"value:{model.IntValue}");
        };
        model.IntCollection.CollectionChanged += (_, _) => replayOrder.Add("collection");

        history.Undo();
        Assert.Equal(["value:1", "collection", "value:0"], replayOrder);
        Assert.Empty(model.IntCollection);

        replayOrder.Clear();
        history.Redo();
        Assert.Equal(["value:1", "collection", "value:2"], replayOrder);
        Assert.Equal([10], model.IntCollection);
    }

    [Theory]
    [InlineData(TestModelKinds.EditableModel)]
    [InlineData(TestModelKinds.Direct)]
    public void Independent_boolean_properties_are_replayed_in_order(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateFlagTestModel(testModelKind, history);

        using (history.CoalescingBatch())
        {
            model.IsA = true;
            model.IsB = true;
            model.IsA = false;
        }

        Assert.False(model.IsA);
        Assert.True(model.IsB);

        history.Undo();
        Assert.False(model.IsA);
        Assert.False(model.IsB);

        history.Redo();
        Assert.False(model.IsA);
        Assert.True(model.IsB);
    }

    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void Coalescing_batches_can_be_nested(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateBasicTestModel(testModelKind, history);

        history.BeginCoalescingBatch();
        model.IntValue = 1;
        history.BeginCoalescingBatch();
        model.IntValue = 2;
        history.EndCoalescingBatch();
        model.IntValue = 3;
        history.EndCoalescingBatch();

        Assert.Equal(1, history.UndoCount);
        history.Undo();
        Assert.Equal(0, model.IntValue);
        history.Redo();
        Assert.Equal(3, model.IntValue);
    }

    [Fact]
    public void Regular_and_coalescing_batches_cannot_be_mixed()
    {
        using var history = new History();

        history.BeginBatch();
        Assert.Throws<InvalidOperationException>(() => history.BeginCoalescingBatch());
        Assert.Throws<InvalidOperationException>(() => history.EndCoalescingBatch());
        history.EndBatch();

        history.BeginCoalescingBatch();
        Assert.Throws<InvalidOperationException>(() => history.BeginBatch());
        Assert.Throws<InvalidOperationException>(() => history.EndBatch());
        history.EndCoalescingBatch();
    }

    [Fact]
    public void Coalescing_scope_is_balanced_when_an_exception_is_thrown()
    {
        using var history = new History();

        Assert.Throws<InvalidOperationException>((Action)(() =>
        {
            using (history.CoalescingBatch())
            {
                Assert.True(history.IsInBatch);
                throw new InvalidOperationException();
            }
        }));

        Assert.False(history.IsInBatch);
        Assert.Equal(0, history.BatchDepth);
    }

    [Fact]
    public void EndCoalescingBatch_requires_a_matching_begin()
    {
        using var history = new History();

        Assert.Throws<InvalidOperationException>(() => history.EndCoalescingBatch());
    }

    private static List<string?> ObservePropertyChanges(INotifyPropertyChanged model)
    {
        var propertyNames = new List<string?>();
        model.PropertyChanged += (_, e) => propertyNames.Add(e.PropertyName);
        return propertyNames;
    }
}
