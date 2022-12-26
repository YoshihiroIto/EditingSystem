﻿using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Xunit;

namespace Jewelry.EditingSystem.Tests;

public class SinglePropertyDirectModeTests
{
    [Fact]
    public void Basic()
    {
        var history = new History();
        var model = new TestModel(history);
        
        Assert.Equal(0, history.UndoCount);
        Assert.Equal(0, history.RedoCount);

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

        void HistoryOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
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
    
    public sealed class TestModel : INotifyPropertyChanged
    {
        private readonly History _history;

        public TestModel(History history)
        {
            _history = history;
        }
        
        #region IntValue

        private int _IntValue;

        public int IntValue
        {
            get => _IntValue;
            set => this.SetEditableProperty(_history, v => SetField(ref _IntValue, v), _IntValue, value);
        }

        #endregion

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