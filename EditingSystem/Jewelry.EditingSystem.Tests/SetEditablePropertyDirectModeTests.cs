using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Xunit;

namespace Jewelry.EditingSystem.Tests;

public sealed class SetEditablePropertyDirectModeTests
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

    public sealed class TestModel : INotifyPropertyChanged
    {
        private readonly History _history;

        public int ChangingCount { get; set; }

        public TestModel(History history)
        {
            _history = history;
        }

        #region IntValue

        private int _IntValue;

        public int IntValue
        {
            get => _IntValue;
            set
            {
                if (this.SetEditableProperty(_history, v => SetField(ref _IntValue, v), _IntValue, value))
                    ++ChangingCount;
            }
        }

        #endregion
        
        public bool IsA
        {
            get => (_flags & FlagIsA) != default;
            set
            {
                if (this.SetEditableFlagProperty(_history, v => _flags = v, _flags, FlagIsA, value))
                    ++ChangingCount;
            }
        }

        public bool IsB
        {
            get => (_flags & FlagIsB) != default;
            set
            {
                if (this.SetEditableFlagProperty(_history, v => _flags = v, _flags, FlagIsB, value))
                    ++ChangingCount;
            }
        }

        public bool IsC
        {
            get => (_flags & FlagIsC) != default;
            set
            {
                if (this.SetEditableFlagProperty(_history, v => _flags = v, _flags, FlagIsC, value))
                    ++ChangingCount;
            }
        }

        private byte _flags;
        private const byte FlagIsA = 1 << 0;
        private const byte FlagIsB = 1 << 1;
        private const byte FlagIsC = 1 << 2;

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // ReSharper disable once UnusedMethodReturnValue.Local
        private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}