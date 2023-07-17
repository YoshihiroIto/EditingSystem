using Xunit;

namespace Jewelry.EditingSystem.Tests;

public sealed class SetEditablePropertyTests
{
    [Fact]
    public void Basic()
    {
        using var history = new History();
        var model = new TestModel(history);

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
    public void Flag()
    {
        using var history = new History();
        var model = new TestModel(history);

        model.IsA = true;
        Assert.Equal(1, model.ChangingCount);

        model.IsB = true;
        Assert.Equal(2, model.ChangingCount);

        model.IsB = true;
        Assert.Equal(2, model.ChangingCount);

        model.IsC = true;
        Assert.Equal(3, model.ChangingCount);
    }

    public sealed class TestModel : EditableModelBase
    {
        public TestModel(History history) : base(history)
        {
        }

        public int ChangingCount { get; set; }

        #region IntValue

        private int _IntValue;

        public int IntValue
        {
            get => _IntValue;
            set
            {
                if (SetEditableProperty(v => _IntValue = v, _IntValue, value))
                    ++ChangingCount;
            }
        }

        #endregion

        public bool IsA
        {
            get => (_flags & FlagIsA) != default;
            set
            {
                if (SetEditableFlagProperty(v => _flags = v, _flags, FlagIsA, value))
                    ++ChangingCount;
            }
        }

        public bool IsB
        {
            get => (_flags & FlagIsB) != default;
            set
            {
                if (SetEditableFlagProperty(v => _flags = v, _flags, FlagIsB, value))
                    ++ChangingCount;
            }
        }

        public bool IsC
        {
            get => (_flags & FlagIsC) != default;
            set
            {
                if (SetEditableFlagProperty(v => _flags = v, _flags, FlagIsC, value))
                    ++ChangingCount;
            }
        }

        private byte _flags;
        private const byte FlagIsA = 1 << 0;
        private const byte FlagIsB = 1 << 1;
        private const byte FlagIsC = 1 << 2;
    }
}