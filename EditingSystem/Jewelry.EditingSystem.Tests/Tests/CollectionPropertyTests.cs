using System;
using System.Collections.ObjectModel;
using System.Linq;
using Jewelry.EditingSystem.Tests.TestModels;
using Xunit;
using static Jewelry.EditingSystem.Tests.TestModels.TestModelCreator;

namespace Jewelry.EditingSystem.Tests.Tests;

public sealed class CollectionPropertyTests
{
    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void Add(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateBasicTestModel(testModelKind, history);

        model.IntCollection = new ObservableCollection<int>();

        model.IntCollection.Add(1);
        Assert.Single(model.IntCollection);

        model.IntCollection.Add(2);
        Assert.Equal(2, model.IntCollection.Count);

        model.IntCollection.Add(3);
        Assert.Equal(3, model.IntCollection.Count);

        history.Undo();
        Assert.Equal(2, model.IntCollection.Count);
        Assert.True(model.IntCollection.SequenceEqual(new[] {1, 2}));

        history.Undo();
        Assert.Single(model.IntCollection);
        Assert.True(model.IntCollection.SequenceEqual(new[] {1}));

        history.Redo();
        Assert.Equal(2, model.IntCollection.Count);
        Assert.True(model.IntCollection.SequenceEqual(new[] {1, 2}));

        history.Redo();
        Assert.Equal(3, model.IntCollection.Count);
        Assert.True(model.IntCollection.SequenceEqual(new[] {1, 2, 3}));
    }

    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void Move_Ascending(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateBasicTestModel(testModelKind, history);

        model.IntCollection = new ObservableCollection<int>();

        model.IntCollection.Add(0);
        model.IntCollection.Add(1);
        model.IntCollection.Add(2);
        model.IntCollection.Add(3);

        Assert.True(model.IntCollection.SequenceEqual(new[] {0, 1, 2, 3}));

        model.IntCollection.Move(0, 3);
        Assert.True(model.IntCollection.SequenceEqual(new[] {1, 2, 3, 0}));

        history.Undo();
        Assert.True(model.IntCollection.SequenceEqual(new[] {0, 1, 2, 3}));

        history.Redo();
        Assert.True(model.IntCollection.SequenceEqual(new[] {1, 2, 3, 0}));
    }

    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void Move_Descending(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateBasicTestModel(testModelKind, history);

        model.IntCollection = new ObservableCollection<int>();

        model.IntCollection.Add(0);
        model.IntCollection.Add(1);
        model.IntCollection.Add(2);
        model.IntCollection.Add(3);

        Assert.True(model.IntCollection.SequenceEqual(new[] {0, 1, 2, 3}));

        model.IntCollection.Move(3, 0);
        Assert.True(model.IntCollection.SequenceEqual(new[] {3, 0, 1, 2}));

        history.Undo();
        Assert.True(model.IntCollection.SequenceEqual(new[] {0, 1, 2, 3}));

        history.Redo();
        Assert.True(model.IntCollection.SequenceEqual(new[] {3, 0, 1, 2}));
    }

    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void Remove(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateBasicTestModel(testModelKind, history);

        model.IntCollection = new ObservableCollection<int>();

        model.IntCollection.Add(100);
        model.IntCollection.Add(101);
        model.IntCollection.Add(102);
        model.IntCollection.Add(103);

        Assert.True(model.IntCollection.SequenceEqual(new[] {100, 101, 102, 103}));

        model.IntCollection.Remove(103);
        Assert.True(model.IntCollection.SequenceEqual(new[] {100, 101, 102}));

        history.Undo();
        Assert.True(model.IntCollection.SequenceEqual(new[] {100, 101, 102, 103}));

        history.Redo();
        Assert.True(model.IntCollection.SequenceEqual(new[] {100, 101, 102}));
    }

    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void RemoveAt(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateBasicTestModel(testModelKind, history);

        model.IntCollection = new ObservableCollection<int>();

        model.IntCollection.Add(100);
        model.IntCollection.Add(101);
        model.IntCollection.Add(102);
        model.IntCollection.Add(103);

        Assert.True(model.IntCollection.SequenceEqual(new[] {100, 101, 102, 103}));

        model.IntCollection.RemoveAt(3);
        Assert.True(model.IntCollection.SequenceEqual(new[] {100, 101, 102}));

        history.Undo();
        Assert.True(model.IntCollection.SequenceEqual(new[] {100, 101, 102, 103}));

        history.Redo();
        Assert.True(model.IntCollection.SequenceEqual(new[] {100, 101, 102}));
    }

    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void Insert(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateBasicTestModel(testModelKind, history);

        model.IntCollection = new ObservableCollection<int>();

        model.IntCollection.Add(100);
        model.IntCollection.Add(101);
        model.IntCollection.Add(102);
        model.IntCollection.Add(103);

        Assert.True(model.IntCollection.SequenceEqual(new[] {100, 101, 102, 103}));

        model.IntCollection.Insert(2, 999);
        Assert.True(model.IntCollection.SequenceEqual(new[] {100, 101, 999, 102, 103}));

        history.Undo();
        Assert.True(model.IntCollection.SequenceEqual(new[] {100, 101, 102, 103}));

        history.Redo();
        Assert.True(model.IntCollection.SequenceEqual(new[] {100, 101, 999, 102, 103}));
    }

    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void Clear(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateBasicTestModel(testModelKind, history);

        model.IntCollection = new ObservableCollection<int>();

        model.IntCollection.Add(100);
        model.IntCollection.Add(101);
        model.IntCollection.Add(102);
        model.IntCollection.Add(103);

        Assert.True(model.IntCollection.SequenceEqual(new[] {100, 101, 102, 103}));

        Assert.Throws<NotSupportedException>(() =>
            model.IntCollection.Clear()
        );
    }

    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void ClearEx(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateBasicTestModel(testModelKind, history);

        model.IntCollection = new ObservableCollection<int>();

        model.IntCollection.Add(100);
        model.IntCollection.Add(101);
        model.IntCollection.Add(102);
        model.IntCollection.Add(103);

        Assert.True(model.IntCollection.SequenceEqual(new[] {100, 101, 102, 103}));

        model.IntCollection.ClearEx(history);

        Assert.True(model.IntCollection.SequenceEqual(new int[] { }));

        history.Undo();
        Assert.True(model.IntCollection.SequenceEqual(new[] {100, 101, 102, 103}));

        history.Redo();
        Assert.True(model.IntCollection.SequenceEqual(new int[] { }));
    }

    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void Replace(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateBasicTestModel(testModelKind, history);

        model.IntCollection = new ObservableCollection<int>();

        model.IntCollection.Add(100);
        model.IntCollection.Add(101);
        model.IntCollection.Add(102);
        model.IntCollection.Add(103);

        Assert.True(model.IntCollection.SequenceEqual(new[] {100, 101, 102, 103}));

        model.IntCollection[2] = 999;
        Assert.True(model.IntCollection.SequenceEqual(new[] {100, 101, 999, 103}));

        history.Undo();
        Assert.True(model.IntCollection.SequenceEqual(new[] {100, 101, 102, 103}));

        history.Redo();
        Assert.True(model.IntCollection.SequenceEqual(new[] {100, 101, 999, 103}));
    }

    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void CollectionChanged_on_Undo_Redo(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateBasicTestModel(testModelKind, history);

        model.IntCollection = new ObservableCollection<int>();

        var count = 0;
        // ReSharper disable once AccessToModifiedClosure
        model.IntCollection.CollectionChanged += (_, __) => ++count;

        model.IntCollection.Add(100);
        model.IntCollection.Add(101);
        model.IntCollection.Add(102);
        model.IntCollection.Add(103);

        count = 0;

        var oldCount = count;
        history.Undo();
        Assert.NotEqual(oldCount, count);

        oldCount = count;
        history.Redo();
        Assert.NotEqual(oldCount, count);



        model.IntCollection.Move(0, 3);

        oldCount = count;
        history.Undo();
        Assert.NotEqual(oldCount, count);

        oldCount = count;
        history.Redo();
        Assert.NotEqual(oldCount, count);



        model.IntCollection.Remove(100);

        oldCount = count;
        history.Undo();
        Assert.NotEqual(oldCount, count);

        oldCount = count;
        history.Redo();
        Assert.NotEqual(oldCount, count);



        model.IntCollection[2] = 999;

        oldCount = count;
        history.Undo();
        Assert.NotEqual(oldCount, count);

        oldCount = count;
        history.Redo();
        Assert.NotEqual(oldCount, count);
    }

    [Fact]
    public void Shared_collection_is_recorded_once_and_remains_observed_until_last_owner_is_removed()
    {
        using var history = new History();
        var shared = new ObservableCollection<int>();
        var replacement = new ObservableCollection<int>();

        history.RecordPropertyChange<ObservableCollection<int>>(_ => { }, null!, shared);
        history.RecordPropertyChange<ObservableCollection<int>>(_ => { }, null!, shared);

        history.Clear();
        shared.Add(1);
        Assert.Equal(1, history.UndoCount);
        history.Undo();
        Assert.Empty(shared);

        history.RecordPropertyChange(_ => { }, shared, replacement);
        history.Clear();

        shared.Add(42);

        Assert.Equal(1, history.UndoCount);
        history.Undo();
        Assert.Empty(shared);
        Assert.False(history.CanUndo);
    }

    [Fact]
    public void Range_add_can_be_undone()
    {
        using var history = new History();
        var model = new DirectBasicTestModel(history);
        var collection = new RangeObservableCollection<int>();
        model.IntCollection = collection;
        collection.Add(10);
        history.Clear();

        collection.AddRange([20, 30]);
        history.Undo();

        Assert.Equal([10], collection);
    }

    private sealed class RangeObservableCollection<T> : ObservableCollection<T>
    {
        public void AddRange(System.Collections.Generic.IReadOnlyList<T> items)
        {
            var startingIndex = Count;
            foreach (var item in items)
                Items.Add(item);

            OnCollectionChanged(new System.Collections.Specialized.NotifyCollectionChangedEventArgs(
                System.Collections.Specialized.NotifyCollectionChangedAction.Add,
                (System.Collections.IList)items,
                startingIndex));
        }
    }
}
