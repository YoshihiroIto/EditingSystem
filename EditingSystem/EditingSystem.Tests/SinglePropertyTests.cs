using System.ComponentModel;
using Xunit;

namespace EditingSystem.Tests
{
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

            var canUndoCount = 0;
            var canRedoCount = 0;

            void HistoryOnPropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == "CanUndo") ++canUndoCount;
                if (e.PropertyName == "CanRedo") ++canRedoCount;
            }

            history.PropertyChanged += HistoryOnPropertyChanged;

            model.IntValue = 123;
            Assert.Equal(1, canUndoCount);
            Assert.Equal(0, canRedoCount);

            model.IntValue = 456;
            Assert.Equal(1, canUndoCount);
            Assert.Equal(0, canRedoCount);

            history.Undo();
            Assert.Equal(1, canUndoCount);
            Assert.Equal(1, canRedoCount);

            history.Undo();
            Assert.Equal(2, canUndoCount);
            Assert.Equal(1, canRedoCount);
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
}