using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Xunit;

namespace Jewelry.EditingSystem.Tests;

public sealed class PauseEditingDirectModeTests
{
    [Fact]
    public void Basic()
    {
        var history = new History();
        var model = new TestModel(history);

        history.BeginPause();
        {
            model.ValueA = 10;
            model.ValueA = 11;
            model.ValueA = 12;

            model.ValueB = "A";
            model.ValueB = "B";
            model.ValueB = "C";
        }
        history.EndPause();

        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);
    }

    [Fact]
    public void NestingPause()
    {
        var history = new History();
        var model = new TestModel(history);

        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);

        history.BeginPause();
        {
            model.ValueA = 10;

            history.BeginPause();
            {
                model.ValueA = 11;

                history.BeginPause();
                {
                    model.ValueA = 12;
                    model.ValueB = "A";
                }
                history.EndPause();

                model.ValueB = "B";
            }
            history.EndPause();

            model.ValueB = "C";
        }
        history.EndPause();

        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);
    }

    [Fact]
    public void Cannot_call_undo_during_pause()
    {
        var history = new History();
        var model = new TestModel(history);

        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);

        model.ValueA = 999;
        model.ValueB = "XYZ";

        history.BeginPause();

        Assert.Throws<InvalidOperationException>(() =>
            history.Undo()
        );
    }

    [Fact]
    public void Cannot_call_redo_during_pause()
    {
        var history = new History();
        var model = new TestModel(history);

        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);

        model.ValueA = 999;
        model.ValueB = "XYZ";

        history.BeginPause();

        Assert.Throws<InvalidOperationException>(() =>
            history.Redo()
        );
    }

    [Fact]
    public void Pause_has_not_begun()
    {
        var history = new History();
        var model = new TestModel(history);

        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);

        model.ValueA = 999;
        model.ValueB = "XYZ";

        Assert.Throws<InvalidOperationException>(() =>
            history.EndPause()
        );
    }

    public class TestModel : INotifyPropertyChanged
    {
        private readonly History _history;

        public TestModel(History history)
        {
            _history = history;
        }

        #region ValueA

        private int _ValueA;

        public int ValueA
        {
            get => _ValueA;
            set => this.SetEditableProperty(_history, v => SetField(ref _ValueA, v), _ValueA, value);
        }

        #endregion

        #region ValueB

        private string _ValueB = "";

        public string ValueB
        {
            get => _ValueB;
            set => this.SetEditableProperty(_history, v => SetField(ref _ValueB, v), _ValueB, value);
        }

        #endregion

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