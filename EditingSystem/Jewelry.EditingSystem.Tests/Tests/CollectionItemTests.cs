using Jewelry.EditingSystem.Tests.TestModels;
using System.Collections.ObjectModel;
using Xunit;
using static Jewelry.EditingSystem.Tests.TestModels.TestModelCreator;

// ReSharper disable UseObjectOrCollectionInitializer

namespace Jewelry.EditingSystem.Tests;

public sealed class CollectionItemTests
{
    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void Add(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateTestModel(testModelKind, history);

        model.Collection = new ObservableCollection<CollectionItem>();

        var item0 = new CollectionItem();
        var item1 = new CollectionItem();
        var item2 = new CollectionItem();

        model.Collection.Add(item0);
        model.Collection.Add(item1);
        model.Collection.Add(item2);

        Assert.Equal(1, item0.CollectionChangedAddCount);
        Assert.Equal(1, item1.CollectionChangedAddCount);
        Assert.Equal(1, item2.CollectionChangedAddCount);
        Assert.Equal(0, item0.CollectionChangedRemoveCount);
        Assert.Equal(0, item1.CollectionChangedRemoveCount);
        Assert.Equal(0, item2.CollectionChangedRemoveCount);

        history.Undo();
        Assert.Equal(1, item0.CollectionChangedAddCount);
        Assert.Equal(1, item1.CollectionChangedAddCount);
        Assert.Equal(1, item2.CollectionChangedAddCount);
        Assert.Equal(0, item0.CollectionChangedRemoveCount);
        Assert.Equal(0, item1.CollectionChangedRemoveCount);
        Assert.Equal(1, item2.CollectionChangedRemoveCount);

        history.Undo();
        Assert.Equal(1, item0.CollectionChangedAddCount);
        Assert.Equal(1, item1.CollectionChangedAddCount);
        Assert.Equal(1, item2.CollectionChangedAddCount);
        Assert.Equal(0, item0.CollectionChangedRemoveCount);
        Assert.Equal(1, item1.CollectionChangedRemoveCount);
        Assert.Equal(1, item2.CollectionChangedRemoveCount);

        history.Undo();
        Assert.Equal(1, item0.CollectionChangedAddCount);
        Assert.Equal(1, item1.CollectionChangedAddCount);
        Assert.Equal(1, item2.CollectionChangedAddCount);
        Assert.Equal(1, item0.CollectionChangedRemoveCount);
        Assert.Equal(1, item1.CollectionChangedRemoveCount);
        Assert.Equal(1, item2.CollectionChangedRemoveCount);

        history.Redo();
        Assert.Equal(2, item0.CollectionChangedAddCount);
        Assert.Equal(1, item1.CollectionChangedAddCount);
        Assert.Equal(1, item2.CollectionChangedAddCount);
        Assert.Equal(1, item0.CollectionChangedRemoveCount);
        Assert.Equal(1, item1.CollectionChangedRemoveCount);
        Assert.Equal(1, item2.CollectionChangedRemoveCount);

        history.Redo();
        Assert.Equal(2, item0.CollectionChangedAddCount);
        Assert.Equal(2, item1.CollectionChangedAddCount);
        Assert.Equal(1, item2.CollectionChangedAddCount);
        Assert.Equal(1, item0.CollectionChangedRemoveCount);
        Assert.Equal(1, item1.CollectionChangedRemoveCount);
        Assert.Equal(1, item2.CollectionChangedRemoveCount);

        history.Redo();
        Assert.Equal(2, item0.CollectionChangedAddCount);
        Assert.Equal(2, item1.CollectionChangedAddCount);
        Assert.Equal(2, item2.CollectionChangedAddCount);
        Assert.Equal(1, item0.CollectionChangedRemoveCount);
        Assert.Equal(1, item1.CollectionChangedRemoveCount);
        Assert.Equal(1, item2.CollectionChangedRemoveCount);
    }

    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void Move_Ascending(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateTestModel(testModelKind, history);

        model.Collection = new ObservableCollection<CollectionItem>();

        var item0 = new CollectionItem();
        var item1 = new CollectionItem();
        var item2 = new CollectionItem();
        var item3 = new CollectionItem();

        model.Collection.Add(item0);
        model.Collection.Add(item1);
        model.Collection.Add(item2);
        model.Collection.Add(item3);

        Assert.Equal(1, item0.CollectionChangedAddCount);
        Assert.Equal(1, item1.CollectionChangedAddCount);
        Assert.Equal(1, item2.CollectionChangedAddCount);
        Assert.Equal(1, item3.CollectionChangedAddCount);
        Assert.Equal(0, item0.CollectionChangedRemoveCount);
        Assert.Equal(0, item1.CollectionChangedRemoveCount);
        Assert.Equal(0, item2.CollectionChangedRemoveCount);
        Assert.Equal(0, item3.CollectionChangedRemoveCount);
        Assert.Equal(0, item0.CollectionChangedMoveCount);
        Assert.Equal(0, item1.CollectionChangedMoveCount);
        Assert.Equal(0, item2.CollectionChangedMoveCount);
        Assert.Equal(0, item3.CollectionChangedMoveCount);

        model.Collection.Move(0, 3);

        Assert.Equal(1, item0.CollectionChangedAddCount);
        Assert.Equal(1, item1.CollectionChangedAddCount);
        Assert.Equal(1, item2.CollectionChangedAddCount);
        Assert.Equal(1, item3.CollectionChangedAddCount);
        Assert.Equal(0, item0.CollectionChangedRemoveCount);
        Assert.Equal(0, item1.CollectionChangedRemoveCount);
        Assert.Equal(0, item2.CollectionChangedRemoveCount);
        Assert.Equal(0, item3.CollectionChangedRemoveCount);
        Assert.Equal(1, item0.CollectionChangedMoveCount);
        Assert.Equal(0, item1.CollectionChangedMoveCount);
        Assert.Equal(0, item2.CollectionChangedMoveCount);
        Assert.Equal(0, item3.CollectionChangedMoveCount);

        history.Undo();
        Assert.Equal(1, item0.CollectionChangedAddCount);
        Assert.Equal(1, item1.CollectionChangedAddCount);
        Assert.Equal(1, item2.CollectionChangedAddCount);
        Assert.Equal(1, item3.CollectionChangedAddCount);
        Assert.Equal(0, item0.CollectionChangedRemoveCount);
        Assert.Equal(0, item1.CollectionChangedRemoveCount);
        Assert.Equal(0, item2.CollectionChangedRemoveCount);
        Assert.Equal(0, item3.CollectionChangedRemoveCount);
        Assert.Equal(2, item0.CollectionChangedMoveCount);
        Assert.Equal(0, item1.CollectionChangedMoveCount);
        Assert.Equal(0, item2.CollectionChangedMoveCount);
        Assert.Equal(0, item3.CollectionChangedMoveCount);

        history.Redo();
        Assert.Equal(1, item0.CollectionChangedAddCount);
        Assert.Equal(1, item1.CollectionChangedAddCount);
        Assert.Equal(1, item2.CollectionChangedAddCount);
        Assert.Equal(1, item3.CollectionChangedAddCount);
        Assert.Equal(0, item0.CollectionChangedRemoveCount);
        Assert.Equal(0, item1.CollectionChangedRemoveCount);
        Assert.Equal(0, item2.CollectionChangedRemoveCount);
        Assert.Equal(0, item3.CollectionChangedRemoveCount);
        Assert.Equal(3, item0.CollectionChangedMoveCount);
        Assert.Equal(0, item1.CollectionChangedMoveCount);
        Assert.Equal(0, item2.CollectionChangedMoveCount);
        Assert.Equal(0, item3.CollectionChangedMoveCount);
    }

    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void Move_Descending(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateTestModel(testModelKind, history);

        model.Collection = new ObservableCollection<CollectionItem>();

        var item0 = new CollectionItem();
        var item1 = new CollectionItem();
        var item2 = new CollectionItem();
        var item3 = new CollectionItem();

        model.Collection.Add(item0);
        model.Collection.Add(item1);
        model.Collection.Add(item2);
        model.Collection.Add(item3);

        Assert.Equal(1, item0.CollectionChangedAddCount);
        Assert.Equal(1, item1.CollectionChangedAddCount);
        Assert.Equal(1, item2.CollectionChangedAddCount);
        Assert.Equal(1, item3.CollectionChangedAddCount);
        Assert.Equal(0, item0.CollectionChangedRemoveCount);
        Assert.Equal(0, item1.CollectionChangedRemoveCount);
        Assert.Equal(0, item2.CollectionChangedRemoveCount);
        Assert.Equal(0, item3.CollectionChangedRemoveCount);
        Assert.Equal(0, item0.CollectionChangedMoveCount);
        Assert.Equal(0, item1.CollectionChangedMoveCount);
        Assert.Equal(0, item2.CollectionChangedMoveCount);
        Assert.Equal(0, item3.CollectionChangedMoveCount);

        model.Collection.Move(3, 0);

        Assert.Equal(1, item0.CollectionChangedAddCount);
        Assert.Equal(1, item1.CollectionChangedAddCount);
        Assert.Equal(1, item2.CollectionChangedAddCount);
        Assert.Equal(1, item3.CollectionChangedAddCount);
        Assert.Equal(0, item0.CollectionChangedRemoveCount);
        Assert.Equal(0, item1.CollectionChangedRemoveCount);
        Assert.Equal(0, item2.CollectionChangedRemoveCount);
        Assert.Equal(0, item3.CollectionChangedRemoveCount);
        Assert.Equal(0, item0.CollectionChangedMoveCount);
        Assert.Equal(0, item1.CollectionChangedMoveCount);
        Assert.Equal(0, item2.CollectionChangedMoveCount);
        Assert.Equal(1, item3.CollectionChangedMoveCount);

        history.Undo();
        Assert.Equal(1, item0.CollectionChangedAddCount);
        Assert.Equal(1, item1.CollectionChangedAddCount);
        Assert.Equal(1, item2.CollectionChangedAddCount);
        Assert.Equal(1, item3.CollectionChangedAddCount);
        Assert.Equal(0, item0.CollectionChangedRemoveCount);
        Assert.Equal(0, item1.CollectionChangedRemoveCount);
        Assert.Equal(0, item2.CollectionChangedRemoveCount);
        Assert.Equal(0, item3.CollectionChangedRemoveCount);
        Assert.Equal(0, item0.CollectionChangedMoveCount);
        Assert.Equal(0, item1.CollectionChangedMoveCount);
        Assert.Equal(0, item2.CollectionChangedMoveCount);
        Assert.Equal(2, item3.CollectionChangedMoveCount);

        history.Redo();
        Assert.Equal(1, item0.CollectionChangedAddCount);
        Assert.Equal(1, item1.CollectionChangedAddCount);
        Assert.Equal(1, item2.CollectionChangedAddCount);
        Assert.Equal(1, item3.CollectionChangedAddCount);
        Assert.Equal(0, item0.CollectionChangedRemoveCount);
        Assert.Equal(0, item1.CollectionChangedRemoveCount);
        Assert.Equal(0, item2.CollectionChangedRemoveCount);
        Assert.Equal(0, item3.CollectionChangedRemoveCount);
        Assert.Equal(0, item0.CollectionChangedMoveCount);
        Assert.Equal(0, item1.CollectionChangedMoveCount);
        Assert.Equal(0, item2.CollectionChangedMoveCount);
        Assert.Equal(3, item3.CollectionChangedMoveCount);
    }

    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void Remove(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateTestModel(testModelKind, history);

        model.Collection = new ObservableCollection<CollectionItem>();

        var item0 = new CollectionItem();
        var item1 = new CollectionItem();
        var item2 = new CollectionItem();
        var item3 = new CollectionItem();

        model.Collection.Add(item0);
        model.Collection.Add(item1);
        model.Collection.Add(item2);
        model.Collection.Add(item3);

        model.Collection.Remove(item3);

        Assert.Equal(1, item0.CollectionChangedAddCount);
        Assert.Equal(1, item1.CollectionChangedAddCount);
        Assert.Equal(1, item2.CollectionChangedAddCount);
        Assert.Equal(1, item3.CollectionChangedAddCount);
        Assert.Equal(0, item0.CollectionChangedRemoveCount);
        Assert.Equal(0, item1.CollectionChangedRemoveCount);
        Assert.Equal(0, item2.CollectionChangedRemoveCount);
        Assert.Equal(1, item3.CollectionChangedRemoveCount);
        Assert.Equal(0, item0.CollectionChangedMoveCount);
        Assert.Equal(0, item1.CollectionChangedMoveCount);
        Assert.Equal(0, item2.CollectionChangedMoveCount);
        Assert.Equal(0, item3.CollectionChangedMoveCount);

        history.Undo();
        Assert.Equal(1, item0.CollectionChangedAddCount);
        Assert.Equal(1, item1.CollectionChangedAddCount);
        Assert.Equal(1, item2.CollectionChangedAddCount);
        Assert.Equal(2, item3.CollectionChangedAddCount);
        Assert.Equal(0, item0.CollectionChangedRemoveCount);
        Assert.Equal(0, item1.CollectionChangedRemoveCount);
        Assert.Equal(0, item2.CollectionChangedRemoveCount);
        Assert.Equal(1, item3.CollectionChangedRemoveCount);
        Assert.Equal(0, item0.CollectionChangedMoveCount);
        Assert.Equal(0, item1.CollectionChangedMoveCount);
        Assert.Equal(0, item2.CollectionChangedMoveCount);
        Assert.Equal(0, item3.CollectionChangedMoveCount);

        history.Redo();
        Assert.Equal(1, item0.CollectionChangedAddCount);
        Assert.Equal(1, item1.CollectionChangedAddCount);
        Assert.Equal(1, item2.CollectionChangedAddCount);
        Assert.Equal(2, item3.CollectionChangedAddCount);
        Assert.Equal(0, item0.CollectionChangedRemoveCount);
        Assert.Equal(0, item1.CollectionChangedRemoveCount);
        Assert.Equal(0, item2.CollectionChangedRemoveCount);
        Assert.Equal(2, item3.CollectionChangedRemoveCount);
        Assert.Equal(0, item0.CollectionChangedMoveCount);
        Assert.Equal(0, item1.CollectionChangedMoveCount);
        Assert.Equal(0, item2.CollectionChangedMoveCount);
        Assert.Equal(0, item3.CollectionChangedMoveCount);
    }

    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void RemoveAt(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateTestModel(testModelKind, history);

        model.Collection = new ObservableCollection<CollectionItem>();

        var item0 = new CollectionItem();
        var item1 = new CollectionItem();
        var item2 = new CollectionItem();
        var item3 = new CollectionItem();

        model.Collection.Add(item0);
        model.Collection.Add(item1);
        model.Collection.Add(item2);
        model.Collection.Add(item3);

        model.Collection.RemoveAt(3);

        Assert.Equal(1, item0.CollectionChangedAddCount);
        Assert.Equal(1, item1.CollectionChangedAddCount);
        Assert.Equal(1, item2.CollectionChangedAddCount);
        Assert.Equal(1, item3.CollectionChangedAddCount);
        Assert.Equal(0, item0.CollectionChangedRemoveCount);
        Assert.Equal(0, item1.CollectionChangedRemoveCount);
        Assert.Equal(0, item2.CollectionChangedRemoveCount);
        Assert.Equal(1, item3.CollectionChangedRemoveCount);
        Assert.Equal(0, item0.CollectionChangedMoveCount);
        Assert.Equal(0, item1.CollectionChangedMoveCount);
        Assert.Equal(0, item2.CollectionChangedMoveCount);
        Assert.Equal(0, item3.CollectionChangedMoveCount);

        history.Undo();
        Assert.Equal(1, item0.CollectionChangedAddCount);
        Assert.Equal(1, item1.CollectionChangedAddCount);
        Assert.Equal(1, item2.CollectionChangedAddCount);
        Assert.Equal(2, item3.CollectionChangedAddCount);
        Assert.Equal(0, item0.CollectionChangedRemoveCount);
        Assert.Equal(0, item1.CollectionChangedRemoveCount);
        Assert.Equal(0, item2.CollectionChangedRemoveCount);
        Assert.Equal(1, item3.CollectionChangedRemoveCount);
        Assert.Equal(0, item0.CollectionChangedMoveCount);
        Assert.Equal(0, item1.CollectionChangedMoveCount);
        Assert.Equal(0, item2.CollectionChangedMoveCount);
        Assert.Equal(0, item3.CollectionChangedMoveCount);

        history.Redo();
        Assert.Equal(1, item0.CollectionChangedAddCount);
        Assert.Equal(1, item1.CollectionChangedAddCount);
        Assert.Equal(1, item2.CollectionChangedAddCount);
        Assert.Equal(2, item3.CollectionChangedAddCount);
        Assert.Equal(0, item0.CollectionChangedRemoveCount);
        Assert.Equal(0, item1.CollectionChangedRemoveCount);
        Assert.Equal(0, item2.CollectionChangedRemoveCount);
        Assert.Equal(2, item3.CollectionChangedRemoveCount);
        Assert.Equal(0, item0.CollectionChangedMoveCount);
        Assert.Equal(0, item1.CollectionChangedMoveCount);
        Assert.Equal(0, item2.CollectionChangedMoveCount);
        Assert.Equal(0, item3.CollectionChangedMoveCount);
    }

    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void Insert(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateTestModel(testModelKind, history);

        model.Collection = new ObservableCollection<CollectionItem>();

        var item0 = new CollectionItem();
        var item1 = new CollectionItem();
        var item2 = new CollectionItem();
        var item3 = new CollectionItem();

        model.Collection.Add(item0);
        model.Collection.Add(item1);
        model.Collection.Add(item2);
        model.Collection.Add(item3);

        Assert.Equal(1, item0.CollectionChangedAddCount);
        Assert.Equal(1, item1.CollectionChangedAddCount);
        Assert.Equal(1, item2.CollectionChangedAddCount);
        Assert.Equal(1, item3.CollectionChangedAddCount);
        Assert.Equal(0, item0.CollectionChangedRemoveCount);
        Assert.Equal(0, item1.CollectionChangedRemoveCount);
        Assert.Equal(0, item2.CollectionChangedRemoveCount);
        Assert.Equal(0, item3.CollectionChangedRemoveCount);
        Assert.Equal(0, item0.CollectionChangedMoveCount);
        Assert.Equal(0, item1.CollectionChangedMoveCount);
        Assert.Equal(0, item2.CollectionChangedMoveCount);
        Assert.Equal(0, item3.CollectionChangedMoveCount);

        var itemX = new CollectionItem();
        Assert.Equal(0, itemX.CollectionChangedAddCount);
        Assert.Equal(0, itemX.CollectionChangedRemoveCount);
        Assert.Equal(0, itemX.CollectionChangedMoveCount);

        model.Collection.Insert(2, itemX);

        Assert.Equal(1, item0.CollectionChangedAddCount);
        Assert.Equal(1, item1.CollectionChangedAddCount);
        Assert.Equal(1, itemX.CollectionChangedAddCount);
        Assert.Equal(1, item2.CollectionChangedAddCount);
        Assert.Equal(1, item3.CollectionChangedAddCount);
        Assert.Equal(0, item0.CollectionChangedRemoveCount);
        Assert.Equal(0, item1.CollectionChangedRemoveCount);
        Assert.Equal(0, itemX.CollectionChangedRemoveCount);
        Assert.Equal(0, item2.CollectionChangedRemoveCount);
        Assert.Equal(0, item3.CollectionChangedRemoveCount);
        Assert.Equal(0, item0.CollectionChangedMoveCount);
        Assert.Equal(0, itemX.CollectionChangedMoveCount);
        Assert.Equal(0, item1.CollectionChangedMoveCount);
        Assert.Equal(0, item2.CollectionChangedMoveCount);
        Assert.Equal(0, item3.CollectionChangedMoveCount);

        history.Undo();
        Assert.Equal(1, item0.CollectionChangedAddCount);
        Assert.Equal(1, item1.CollectionChangedAddCount);
        Assert.Equal(1, itemX.CollectionChangedAddCount);
        Assert.Equal(1, item2.CollectionChangedAddCount);
        Assert.Equal(1, item3.CollectionChangedAddCount);
        Assert.Equal(0, item0.CollectionChangedRemoveCount);
        Assert.Equal(0, item1.CollectionChangedRemoveCount);
        Assert.Equal(1, itemX.CollectionChangedRemoveCount);
        Assert.Equal(0, item2.CollectionChangedRemoveCount);
        Assert.Equal(0, item3.CollectionChangedRemoveCount);
        Assert.Equal(0, item0.CollectionChangedMoveCount);
        Assert.Equal(0, itemX.CollectionChangedMoveCount);
        Assert.Equal(0, item1.CollectionChangedMoveCount);
        Assert.Equal(0, item2.CollectionChangedMoveCount);
        Assert.Equal(0, item3.CollectionChangedMoveCount);

        history.Redo();
        Assert.Equal(1, item0.CollectionChangedAddCount);
        Assert.Equal(1, item1.CollectionChangedAddCount);
        Assert.Equal(2, itemX.CollectionChangedAddCount);
        Assert.Equal(1, item2.CollectionChangedAddCount);
        Assert.Equal(1, item3.CollectionChangedAddCount);
        Assert.Equal(0, item0.CollectionChangedRemoveCount);
        Assert.Equal(0, item1.CollectionChangedRemoveCount);
        Assert.Equal(1, itemX.CollectionChangedRemoveCount);
        Assert.Equal(0, item2.CollectionChangedRemoveCount);
        Assert.Equal(0, item3.CollectionChangedRemoveCount);
        Assert.Equal(0, item0.CollectionChangedMoveCount);
        Assert.Equal(0, itemX.CollectionChangedMoveCount);
        Assert.Equal(0, item1.CollectionChangedMoveCount);
        Assert.Equal(0, item2.CollectionChangedMoveCount);
        Assert.Equal(0, item3.CollectionChangedMoveCount);
    }

    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void ClearEx(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateTestModel(testModelKind, history);

        model.Collection = new ObservableCollection<CollectionItem>();

        var item0 = new CollectionItem();
        var item1 = new CollectionItem();
        var item2 = new CollectionItem();
        var item3 = new CollectionItem();

        model.Collection.Add(item0);
        model.Collection.Add(item1);
        model.Collection.Add(item2);
        model.Collection.Add(item3);

        Assert.Equal(1, item0.CollectionChangedAddCount);
        Assert.Equal(1, item1.CollectionChangedAddCount);
        Assert.Equal(1, item2.CollectionChangedAddCount);
        Assert.Equal(1, item3.CollectionChangedAddCount);
        Assert.Equal(0, item0.CollectionChangedRemoveCount);
        Assert.Equal(0, item1.CollectionChangedRemoveCount);
        Assert.Equal(0, item2.CollectionChangedRemoveCount);
        Assert.Equal(0, item3.CollectionChangedRemoveCount);
        Assert.Equal(0, item0.CollectionChangedMoveCount);
        Assert.Equal(0, item1.CollectionChangedMoveCount);
        Assert.Equal(0, item2.CollectionChangedMoveCount);
        Assert.Equal(0, item3.CollectionChangedMoveCount);

        model.Collection.ClearEx(history);

        Assert.Equal(1, item0.CollectionChangedAddCount);
        Assert.Equal(1, item1.CollectionChangedAddCount);
        Assert.Equal(1, item2.CollectionChangedAddCount);
        Assert.Equal(1, item3.CollectionChangedAddCount);
        Assert.Equal(1, item0.CollectionChangedRemoveCount);
        Assert.Equal(1, item1.CollectionChangedRemoveCount);
        Assert.Equal(1, item2.CollectionChangedRemoveCount);
        Assert.Equal(1, item3.CollectionChangedRemoveCount);
        Assert.Equal(0, item0.CollectionChangedMoveCount);
        Assert.Equal(0, item1.CollectionChangedMoveCount);
        Assert.Equal(0, item2.CollectionChangedMoveCount);
        Assert.Equal(0, item3.CollectionChangedMoveCount);

        history.Undo();
        Assert.Equal(2, item0.CollectionChangedAddCount);
        Assert.Equal(2, item1.CollectionChangedAddCount);
        Assert.Equal(2, item2.CollectionChangedAddCount);
        Assert.Equal(2, item3.CollectionChangedAddCount);
        Assert.Equal(1, item0.CollectionChangedRemoveCount);
        Assert.Equal(1, item1.CollectionChangedRemoveCount);
        Assert.Equal(1, item2.CollectionChangedRemoveCount);
        Assert.Equal(1, item3.CollectionChangedRemoveCount);
        Assert.Equal(0, item0.CollectionChangedMoveCount);
        Assert.Equal(0, item1.CollectionChangedMoveCount);
        Assert.Equal(0, item2.CollectionChangedMoveCount);
        Assert.Equal(0, item3.CollectionChangedMoveCount);

        history.Redo();
        Assert.Equal(2, item0.CollectionChangedAddCount);
        Assert.Equal(2, item1.CollectionChangedAddCount);
        Assert.Equal(2, item2.CollectionChangedAddCount);
        Assert.Equal(2, item3.CollectionChangedAddCount);
        Assert.Equal(2, item0.CollectionChangedRemoveCount);
        Assert.Equal(2, item1.CollectionChangedRemoveCount);
        Assert.Equal(2, item2.CollectionChangedRemoveCount);
        Assert.Equal(2, item3.CollectionChangedRemoveCount);
        Assert.Equal(0, item0.CollectionChangedMoveCount);
        Assert.Equal(0, item1.CollectionChangedMoveCount);
        Assert.Equal(0, item2.CollectionChangedMoveCount);
        Assert.Equal(0, item3.CollectionChangedMoveCount);
    }

    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void Replace(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateTestModel(testModelKind, history);

        model.Collection = new ObservableCollection<CollectionItem>();

        var item0 = new CollectionItem();
        var item1 = new CollectionItem();
        var item2 = new CollectionItem();
        var item3 = new CollectionItem();

        model.Collection.Add(item0);
        model.Collection.Add(item1);
        model.Collection.Add(item2);
        model.Collection.Add(item3);

        Assert.Equal(1, item0.CollectionChangedAddCount);
        Assert.Equal(1, item1.CollectionChangedAddCount);
        Assert.Equal(1, item2.CollectionChangedAddCount);
        Assert.Equal(1, item3.CollectionChangedAddCount);
        Assert.Equal(0, item0.CollectionChangedRemoveCount);
        Assert.Equal(0, item1.CollectionChangedRemoveCount);
        Assert.Equal(0, item2.CollectionChangedRemoveCount);
        Assert.Equal(0, item3.CollectionChangedRemoveCount);
        Assert.Equal(0, item0.CollectionChangedMoveCount);
        Assert.Equal(0, item1.CollectionChangedMoveCount);
        Assert.Equal(0, item2.CollectionChangedMoveCount);
        Assert.Equal(0, item3.CollectionChangedMoveCount);

        var itemX = new CollectionItem();

        model.Collection[2] = itemX;

        Assert.Equal(1, item0.CollectionChangedAddCount);
        Assert.Equal(1, item1.CollectionChangedAddCount);
        Assert.Equal(1, item2.CollectionChangedAddCount);
        Assert.Equal(1, item3.CollectionChangedAddCount);
        Assert.Equal(1, itemX.CollectionChangedAddCount);
        Assert.Equal(0, item0.CollectionChangedRemoveCount);
        Assert.Equal(0, item1.CollectionChangedRemoveCount);
        Assert.Equal(1, item2.CollectionChangedRemoveCount);
        Assert.Equal(0, item3.CollectionChangedRemoveCount);
        Assert.Equal(0, itemX.CollectionChangedRemoveCount);
        Assert.Equal(0, item0.CollectionChangedMoveCount);
        Assert.Equal(0, item1.CollectionChangedMoveCount);
        Assert.Equal(0, item2.CollectionChangedMoveCount);
        Assert.Equal(0, item3.CollectionChangedMoveCount);
        Assert.Equal(0, itemX.CollectionChangedMoveCount);

        history.Undo();
        Assert.Equal(1, item0.CollectionChangedAddCount);
        Assert.Equal(1, item1.CollectionChangedAddCount);
        Assert.Equal(1, item2.CollectionChangedAddCount);
        Assert.Equal(1, item3.CollectionChangedAddCount);
        Assert.Equal(2, itemX.CollectionChangedAddCount);
        Assert.Equal(0, item0.CollectionChangedRemoveCount);
        Assert.Equal(0, item1.CollectionChangedRemoveCount);
        Assert.Equal(2, item2.CollectionChangedRemoveCount);
        Assert.Equal(0, item3.CollectionChangedRemoveCount);
        Assert.Equal(0, itemX.CollectionChangedRemoveCount);
        Assert.Equal(0, item0.CollectionChangedMoveCount);
        Assert.Equal(0, item1.CollectionChangedMoveCount);
        Assert.Equal(0, item2.CollectionChangedMoveCount);
        Assert.Equal(0, item3.CollectionChangedMoveCount);
        Assert.Equal(0, itemX.CollectionChangedMoveCount);

        history.Redo();
        Assert.Equal(1, item0.CollectionChangedAddCount);
        Assert.Equal(1, item1.CollectionChangedAddCount);
        Assert.Equal(1, item2.CollectionChangedAddCount);
        Assert.Equal(1, item3.CollectionChangedAddCount);
        Assert.Equal(3, itemX.CollectionChangedAddCount);
        Assert.Equal(0, item0.CollectionChangedRemoveCount);
        Assert.Equal(0, item1.CollectionChangedRemoveCount);
        Assert.Equal(3, item2.CollectionChangedRemoveCount);
        Assert.Equal(0, item3.CollectionChangedRemoveCount);
        Assert.Equal(0, itemX.CollectionChangedRemoveCount);
        Assert.Equal(0, item0.CollectionChangedMoveCount);
        Assert.Equal(0, item1.CollectionChangedMoveCount);
        Assert.Equal(0, item2.CollectionChangedMoveCount);
        Assert.Equal(0, item3.CollectionChangedMoveCount);
        Assert.Equal(0, itemX.CollectionChangedMoveCount);
    }
}