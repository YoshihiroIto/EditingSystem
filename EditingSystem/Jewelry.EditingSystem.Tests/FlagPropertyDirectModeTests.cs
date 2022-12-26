using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Xunit;

namespace Jewelry.EditingSystem.Tests;

public class FlagPropertyDirectModeTests
{
    [Fact]
    public void Basic()
    {
        var history = new History();
        var model = new TestModel(history);

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

    public class TestModel : INotifyPropertyChanged
    {
        private readonly History _history;

        public TestModel(History history)
        {
            _history = history;
        }

        public bool IsA
        {
            get => (_flags & Flag_IsA) != 0;
            set => this.SetEditableFlagProperty(_history, v => SetField(ref _flags, v), _flags, Flag_IsA, value);
        }

        public bool IsB
        {
            get => (_flags & Flag_IsB) != 0;
            set => this.SetEditableFlagProperty(_history, v => SetField(ref _flags, v), _flags, Flag_IsB, value);
        }

        public bool IsC
        {
            get => (_flags & Flag_IsC) != 0;
            set => this.SetEditableFlagProperty(_history, v => SetField(ref _flags, v), _flags, Flag_IsC, value);
        }

        private uint _flags;
        private const uint Flag_IsA = 1<< 0;
        private const uint Flag_IsB = 1<< 1;
        private const uint Flag_IsC = 1<< 2;
        

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}