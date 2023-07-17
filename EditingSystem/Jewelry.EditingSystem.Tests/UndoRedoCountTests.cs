using Xunit;

namespace Jewelry.EditingSystem.Tests;

public sealed class UndoRedoCountTests
{
    [Fact]
    public void Basic()
    {
        using var history = new History();
        var model = new TestModel(history);

        Assert.Equal(0, history.UndoCount);
        Assert.Equal(0, history.RedoCount);

        model.IntValue = 123;
        Assert.Equal(1, history.UndoCount);
        Assert.Equal(0, history.RedoCount);

        model.IntValue = 456;
        Assert.Equal(2, history.UndoCount);
        Assert.Equal(0, history.RedoCount);

        history.Undo();
        Assert.Equal(1, history.UndoCount);
        Assert.Equal(1, history.RedoCount);

        history.Clear();
        Assert.Equal(0, history.UndoCount);
        Assert.Equal(0, history.RedoCount);
    }

    public sealed class TestModel : EditableModelBase
    {
        public TestModel(History history) : base(history)
        {
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