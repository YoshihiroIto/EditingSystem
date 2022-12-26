using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Xunit;

namespace Jewelry.EditingSystem.Tests;

public class FlagPropertyDirectModeTests
{
    [Fact]
    public void BasicUint()
    {
        var history = new History();
        var model = new UintTestModel(history);

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
        var history = new History();
        var model = new UlongTestModel(history);

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
    
    public class UintTestModel : INotifyPropertyChanged
    {
        private readonly History _history;

        public UintTestModel(History history)
        {
            _history = history;
        }

        public bool IsA
        {
            get => (_uintFlags & UintFlag_IsA) != 0;
            set => this.SetEditableFlagProperty(_history, v => _uintFlags = v, _uintFlags, UintFlag_IsA, value);
        }

        public bool IsB
        {
            get => (_uintFlags & UintFlag_IsB) != 0;
            set => this.SetEditableFlagProperty(_history, v => _uintFlags = v, _uintFlags, UintFlag_IsB, value);
        }

        public bool IsC
        {
            get => (_uintFlags & UintFlag_IsC) != 0;
            set => this.SetEditableFlagProperty(_history, v => _uintFlags = v, _uintFlags, UintFlag_IsC, value);
        }

        private uint _uintFlags;
        private const uint UintFlag_IsA = 1 << 0;
        private const uint UintFlag_IsB = 1 << 1;
        private const uint UintFlag_IsC = 1 << 2;
        
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

    public class UlongTestModel : INotifyPropertyChanged
    {
        private readonly History _history;

        public UlongTestModel(History history)
        {
            _history = history;
        }

        public bool IsA
        {
            get => (_ulongFlags & UlongFlag_IsA) != 0;
            set => this.SetEditableFlagProperty(_history, v => _ulongFlags = v, _ulongFlags, UlongFlag_IsA, value);
        }

        public bool IsB
        {
            get => (_ulongFlags & UlongFlag_IsB) != 0;
            set => this.SetEditableFlagProperty(_history, v => _ulongFlags = v, _ulongFlags, UlongFlag_IsB, value);
        }

        public bool IsC
        {
            get => (_ulongFlags & UlongFlag_IsC) != 0;
            set => this.SetEditableFlagProperty(_history, v => _ulongFlags = v, _ulongFlags, UlongFlag_IsC, value);
        }

        private ulong _ulongFlags;
        private const ulong UlongFlag_IsA = 1 << 0;
        private const ulong UlongFlag_IsB = 1 << 1;
        private const ulong UlongFlag_IsC = 1 << 2;
        
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