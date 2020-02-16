using Xunit;

namespace Jewelry.EditingSystem.Tests
{
    public class UndoRedoCountTests
    {
        [Fact]
        public void Basic()
        {
            var history = new History();
            var model = new TestModel(history);

            Assert.Equal((0, 0), history.UndoRedoCount);

            model.IntValue = 123;
            Assert.Equal((1, 0), history.UndoRedoCount);

            model.IntValue = 456;
            Assert.Equal((2, 0), history.UndoRedoCount);

            history.Undo();
            Assert.Equal((1, 1), history.UndoRedoCount);

            history.Clear();
            Assert.Equal((0, 0), history.UndoRedoCount);
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