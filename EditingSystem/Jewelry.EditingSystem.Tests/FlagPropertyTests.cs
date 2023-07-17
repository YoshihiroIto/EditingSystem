using System.Numerics;
using Xunit;

namespace Jewelry.EditingSystem.Tests;

public sealed class FlagPropertyTests
{
    [Fact]
    public void BasicByte()
    {
        using var history = new History();
        var model = new TestModel<byte>(history);

        Assert.False(model.IsA);
        Assert.False(model.IsB);
        Assert.False(model.IsC);
        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);

        model.IsA = true;
        Assert.True(model.IsA);
        Assert.False(model.IsB);
        Assert.False(model.IsC);

        model.IsB = true;
        Assert.True(model.IsA);
        Assert.True(model.IsB);
        Assert.False(model.IsC);

        model.IsC = true;
        Assert.True(model.IsA);
        Assert.True(model.IsB);
        Assert.True(model.IsC);

        history.Undo();
        Assert.True(model.IsA);
        Assert.True(model.IsB);
        Assert.False(model.IsC);

        history.Undo();
        Assert.True(model.IsA);
        Assert.False(model.IsB);
        Assert.False(model.IsC);

        history.Undo();
        Assert.False(model.IsA);
        Assert.False(model.IsB);
        Assert.False(model.IsC);

        history.Redo();
        Assert.True(model.IsA);
        Assert.False(model.IsB);
        Assert.False(model.IsC);

        history.Redo();
        Assert.True(model.IsA);
        Assert.True(model.IsB);
        Assert.False(model.IsC);

        history.Redo();
        Assert.True(model.IsA);
        Assert.True(model.IsB);
        Assert.True(model.IsC);
    }

    [Fact]
    public void BasicUint()
    {
        using var history = new History();
        var model = new TestModel<uint>(history);

        Assert.False(model.IsA);
        Assert.False(model.IsB);
        Assert.False(model.IsC);
        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);

        model.IsA = true;
        Assert.True(model.IsA);
        Assert.False(model.IsB);
        Assert.False(model.IsC);

        model.IsB = true;
        Assert.True(model.IsA);
        Assert.True(model.IsB);
        Assert.False(model.IsC);

        model.IsC = true;
        Assert.True(model.IsA);
        Assert.True(model.IsB);
        Assert.True(model.IsC);

        history.Undo();
        Assert.True(model.IsA);
        Assert.True(model.IsB);
        Assert.False(model.IsC);

        history.Undo();
        Assert.True(model.IsA);
        Assert.False(model.IsB);
        Assert.False(model.IsC);

        history.Undo();
        Assert.False(model.IsA);
        Assert.False(model.IsB);
        Assert.False(model.IsC);

        history.Redo();
        Assert.True(model.IsA);
        Assert.False(model.IsB);
        Assert.False(model.IsC);

        history.Redo();
        Assert.True(model.IsA);
        Assert.True(model.IsB);
        Assert.False(model.IsC);

        history.Redo();
        Assert.True(model.IsA);
        Assert.True(model.IsB);
        Assert.True(model.IsC);
    }

    [Fact]
    public void BasicUlong()
    {
        using var history = new History();
        var model = new TestModel<ulong>(history);

        Assert.False(model.IsA);
        Assert.False(model.IsB);
        Assert.False(model.IsC);
        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);

        model.IsA = true;
        Assert.True(model.IsA);
        Assert.False(model.IsB);
        Assert.False(model.IsC);

        model.IsB = true;
        Assert.True(model.IsA);
        Assert.True(model.IsB);
        Assert.False(model.IsC);

        model.IsC = true;
        Assert.True(model.IsA);
        Assert.True(model.IsB);
        Assert.True(model.IsC);

        history.Undo();
        Assert.True(model.IsA);
        Assert.True(model.IsB);
        Assert.False(model.IsC);

        history.Undo();
        Assert.True(model.IsA);
        Assert.False(model.IsB);
        Assert.False(model.IsC);

        history.Undo();
        Assert.False(model.IsA);
        Assert.False(model.IsB);
        Assert.False(model.IsC);

        history.Redo();
        Assert.True(model.IsA);
        Assert.False(model.IsB);
        Assert.False(model.IsC);

        history.Redo();
        Assert.True(model.IsA);
        Assert.True(model.IsB);
        Assert.False(model.IsC);

        history.Redo();
        Assert.True(model.IsA);
        Assert.True(model.IsB);
        Assert.True(model.IsC);
    }

    public sealed class TestModel<T> : EditableModelBase
        where T : struct, IBitwiseOperators<T, T, T>, IEqualityOperators<T, T, bool>, IUnsignedNumber<T>
    {
        public TestModel(History history) : base(history)
        {
        }

        public bool IsA
        {
            get => (_flags & FlagIsA) != default;
            set => SetEditableFlagProperty(v => _flags = v, _flags, FlagIsA, value);
        }

        public bool IsB
        {
            get => (_flags & FlagIsB) != default;
            set => SetEditableFlagProperty(v => _flags = v, _flags, FlagIsB, value);
        }

        public bool IsC
        {
            get => (_flags & FlagIsC) != default;
            set => SetEditableFlagProperty(v => _flags = v, _flags, FlagIsC, value);
        }

        private T _flags;
        private static readonly T FlagIsA = T.CreateChecked(1 << 0);
        private static readonly T FlagIsB = T.CreateChecked(1 << 1);
        private static readonly T FlagIsC = T.CreateChecked(1 << 2);
    }
}