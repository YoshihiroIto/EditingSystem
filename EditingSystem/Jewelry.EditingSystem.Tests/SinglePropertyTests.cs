using System.ComponentModel;
using Xunit;

namespace Jewelry.EditingSystem.Tests;

public class SinglePropertyTests
{
    [Fact]
    public void Basic()
    {
        var history = new History();
        var model = new TestModel(history);

        Assert.Equal(0, model.IntValue);
        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);

        //------------------------------------------------
        model.IntValue = 123;
        Assert.Equal(123, model.IntValue);
        Assert.True(history.CanUndo);
        Assert.False(history.CanRedo);

        model.IntValue = 456;
        Assert.Equal(456, model.IntValue);
        Assert.True(history.CanUndo);
        Assert.False(history.CanRedo);

        model.IntValue = 789;
        Assert.Equal(789, model.IntValue);
        Assert.True(history.CanUndo);
        Assert.False(history.CanRedo);

        history.Undo();
        Assert.Equal(456, model.IntValue);
        Assert.True(history.CanUndo);
        Assert.True(history.CanRedo);

        history.Undo();
        Assert.Equal(123, model.IntValue);
        Assert.True(history.CanUndo);
        Assert.True(history.CanRedo);

        history.Undo();
        Assert.Equal(0, model.IntValue);
        Assert.False(history.CanUndo);
        Assert.True(history.CanRedo);

        //------------------------------------------------
        history.Redo();
        Assert.Equal(123, model.IntValue);
        Assert.True(history.CanUndo);
        Assert.True(history.CanRedo);

        history.Redo();
        Assert.Equal(456, model.IntValue);
        Assert.True(history.CanUndo);
        Assert.True(history.CanRedo);

        history.Redo();
        Assert.Equal(789, model.IntValue);
        Assert.True(history.CanUndo);
        Assert.False(history.CanRedo);

        //------------------------------------------------
        history.Undo();
        history.Undo();
        history.Undo();

        Assert.False(history.CanUndo);
        Assert.True(history.CanRedo);


        model.IntValue = 111;
        Assert.True(history.CanUndo);
        Assert.False(history.CanRedo);
    }


    [Fact]
    public void PropertyChanged()
    {
        var history = new History();
        var model = new TestModel(history);

        var count = 0;

        model.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(model.IntValue))
                ++count;
        };

        Assert.Equal(0, count);

        model.IntValue = 123;
        Assert.Equal(1, count);

        model.IntValue = 456;
        Assert.Equal(2, count);

        history.Undo();
        Assert.Equal(3, count);

        history.Redo();
        Assert.Equal(4, count);
    }

    [Fact]
    public void PropertyChanged_CanUndo_CanRedo_CanClear()
    {
        var history = new History();
        var model = new TestModel(history);

        var canUndoCount = 0;
        var canRedoCount = 0;
        var canClearCount = 0;

        void HistoryOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "CanUndo") ++canUndoCount;
            if (e.PropertyName == "CanRedo") ++canRedoCount;
            if (e.PropertyName == "CanClear") ++canClearCount;
        }

        history.PropertyChanged += HistoryOnPropertyChanged;

        model.IntValue = 123;
        Assert.Equal(1, canUndoCount);
        Assert.Equal(0, canRedoCount);
        Assert.Equal(1, canClearCount);

        model.IntValue = 456;
        Assert.Equal(1, canUndoCount);
        Assert.Equal(0, canRedoCount);
        Assert.Equal(1, canClearCount);

        history.Undo();
        Assert.Equal(1, canUndoCount);
        Assert.Equal(1, canRedoCount);
        Assert.Equal(1, canClearCount);

        history.Undo();
        Assert.Equal(2, canUndoCount);
        Assert.Equal(1, canRedoCount);
        Assert.Equal(1, canClearCount);
    }

    [Fact]
    public void Clear()
    {
        var history = new History();
        var model = new TestModel(history);

        Assert.Equal(0, model.IntValue);
        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);
        Assert.False(history.CanClear);

        //------------------------------------------------
        model.IntValue = 123;
        Assert.Equal(123, model.IntValue);
        Assert.True(history.CanUndo);
        Assert.False(history.CanRedo);
        Assert.True(history.CanClear);

        history.Clear();
        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);
        Assert.False(history.CanClear);

        //------------------------------------------------
        model.IntValue = 456;
        Assert.Equal(456, model.IntValue);
        Assert.True(history.CanUndo);
        Assert.False(history.CanRedo);
        Assert.True(history.CanClear);

        history.Undo();
        Assert.False(history.CanUndo);
        Assert.True(history.CanRedo);
        Assert.True(history.CanClear);

        history.Clear();
        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);
        Assert.False(history.CanClear);
    }

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

    [Fact]
    public void History_is_null()
    {
        var model = new TestModel(null);

        Assert.Equal(0, model.IntValue);

        model.IntValue = 123;
        Assert.Equal(123, model.IntValue);

        model.IntValue = 456;
        Assert.Equal(456, model.IntValue);
    }

    public class TestModel : EditableModelBase
    {
        public TestModel(History history)
        {
            SetupEditingSystem(history);
        }

        #region IntValue

        private int _IntValue;

        public int IntValue
        {
            get => _IntValue;
            set => SetEditableProperty(v => _IntValue = v, _IntValue, value);
        }

        #endregion
    }
}