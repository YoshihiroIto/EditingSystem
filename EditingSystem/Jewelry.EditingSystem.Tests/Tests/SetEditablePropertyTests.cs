using Jewelry.EditingSystem.Tests.TestModels;
using Xunit;
using static Jewelry.EditingSystem.Tests.TestModels.TestModelCreator;

namespace Jewelry.EditingSystem.Tests.Tests;

public sealed class SetEditablePropertyTests
{
    [Theory]
    [ClassData(typeof(TestModelKindsTestData))]
    public void Basic(TestModelKinds testModelKind)
    {
        using var history = new History();
        var model = CreateBasicTestModel(testModelKind, history);

        model.IntValue = 123;
        Assert.Equal(1, model.ChangingCount);

        model.IntValue = 456;
        Assert.Equal(2, model.ChangingCount);

        model.IntValue = 456;
        Assert.Equal(2, model.ChangingCount);

        model.IntValue = 123;
        Assert.Equal(3, model.ChangingCount);
    }

    [Fact]
    public void Throwing_setter_does_not_leave_phantom_history()
    {
        using var history = new History();
        var model = new ThrowingEditableModel(history);

        Assert.Throws<System.InvalidOperationException>(() => model.Value = 1);

        Assert.Equal(0, model.Value);
        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);
    }

    [Fact]
    public void Throwing_collection_setter_keeps_observing_the_applied_collection()
    {
        using var history = new History();
        var model = new ThrowingCollectionModel(history);
        var applied = new System.Collections.ObjectModel.ObservableCollection<int>();
        var rejected = new System.Collections.ObjectModel.ObservableCollection<int>();
        model.Items = applied;
        history.Clear();

        model.ThrowOnSet = true;
        Assert.Throws<System.InvalidOperationException>(() => model.Items = rejected);
        Assert.Same(applied, model.Items);
        Assert.False(history.CanUndo);

        applied.Add(1);
        Assert.True(history.CanUndo);
        history.Undo();
        Assert.Empty(applied);

        history.Clear();
        rejected.Add(1);
        Assert.False(history.CanUndo);
    }

    [Fact]
    public void Failed_collection_property_undo_keeps_observing_the_current_collection()
    {
        using var history = new History();
        var model = new ThrowingCollectionModel(history);
        var oldItems = new System.Collections.ObjectModel.ObservableCollection<int>();
        var currentItems = new System.Collections.ObjectModel.ObservableCollection<int>();
        model.Items = oldItems;
        history.Clear();
        model.Items = currentItems;

        model.ThrowOnSet = true;
        Assert.Throws<System.InvalidOperationException>(() => history.Undo());

        Assert.Same(currentItems, model.Items);
        Assert.Equal(1, history.UndoCount);

        history.Clear();
        currentItems.Add(1);
        Assert.Equal(1, history.UndoCount);
        history.Undo();
        Assert.Empty(currentItems);

        history.Clear();
        oldItems.Add(1);
        Assert.False(history.CanUndo);
    }

    [Fact]
    public void Failed_collection_property_redo_keeps_observing_the_current_collection()
    {
        using var history = new History();
        var model = new ThrowingCollectionModel(history);
        var currentItems = new System.Collections.ObjectModel.ObservableCollection<int>();
        var redoItems = new System.Collections.ObjectModel.ObservableCollection<int>();
        model.Items = currentItems;
        history.Clear();
        model.Items = redoItems;
        history.Undo();

        model.ThrowOnSet = true;
        Assert.Throws<System.InvalidOperationException>(() => history.Redo());

        Assert.Same(currentItems, model.Items);
        Assert.Equal(1, history.RedoCount);

        history.Clear();
        currentItems.Add(1);
        Assert.Equal(1, history.UndoCount);
        history.Undo();
        Assert.Empty(currentItems);

        history.Clear();
        redoItems.Add(1);
        Assert.False(history.CanUndo);
    }

    private sealed class ThrowingEditableModel(History history) : EditableModelBase(history)
    {
        public int Value
        {
            get => 0;
            set => SetEditableProperty<int>(
                _ => throw new System.InvalidOperationException(),
                0,
                value);
        }
    }

    private sealed class ThrowingCollectionModel(History history) : EditableModelBase(history)
    {
        private System.Collections.ObjectModel.ObservableCollection<int> _items = new();

        public bool ThrowOnSet { get; set; }

        public System.Collections.ObjectModel.ObservableCollection<int> Items
        {
            get => _items;
            set => SetEditableProperty(
                newValue =>
                {
                    if (ThrowOnSet)
                        throw new System.InvalidOperationException();
                    _items = newValue;
                },
                _items,
                value);
        }
    }
}
