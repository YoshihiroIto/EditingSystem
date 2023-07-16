using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Xunit;

namespace Jewelry.EditingSystem.Tests;

public sealed class BatchEditingDirectModeTests
{
    [Fact]
    public void Basic()
    {
        var history = new History();
        var model = new TestModel(history);

        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);

        model.ValueA = 999;
        model.ValueB = "XYZ";

        history.BeginBatch();
        {
            model.ValueA = 10;
            model.ValueA = 11;
            model.ValueA = 12;

            model.ValueB = "A";
            model.ValueB = "B";
            model.ValueB = "C";
        }
        history.EndBatch();

        history.Undo();

        Assert.Equal(999, model.ValueA);
        Assert.Equal("XYZ", model.ValueB);

        history.Redo();
        Assert.Equal(12, model.ValueA);
        Assert.Equal("C", model.ValueB);
    }

    [Fact]
    public void Empty()
    {
        var history = new History();

        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);
        Assert.Equal(0, history.UndoCount);
        Assert.Equal(0, history.RedoCount);

        history.BeginBatch();
        {
        }
        history.EndBatch();

        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);
        Assert.Equal(0, history.UndoCount);
        Assert.Equal(0, history.RedoCount);
    }

    [Fact]
    public void NestingBatch()
    {
        var history = new History();
        var model = new TestModel(history);

        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);

        model.ValueA = 999;
        model.ValueB = "XYZ";

        history.BeginBatch();
        {
            model.ValueA = 10;

            history.BeginBatch();
            {
                model.ValueA = 11;

                history.BeginBatch();
                {
                    model.ValueA = 12;
                    model.ValueB = "A";
                }
                history.EndBatch();

                model.ValueB = "B";
            }
            history.EndBatch();

            model.ValueB = "C";
        }
        history.EndBatch();

        history.Undo();

        Assert.Equal(999, model.ValueA);
        Assert.Equal("XYZ", model.ValueB);

        history.Redo();
        Assert.Equal(12, model.ValueA);
        Assert.Equal("C", model.ValueB);
    }

    [Fact]
    public void Cannot_call_undo_during_batch_recording()
    {
        var history = new History();
        var model = new TestModel(history);

        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);

        model.ValueA = 999;
        model.ValueB = "XYZ";

        history.BeginBatch();

        Assert.Throws<InvalidOperationException>(() =>
            history.Undo()
        );
    }

    [Fact]
    public void Cannot_call_redo_during_batch_recording()
    {
        var history = new History();
        var model = new TestModel(history);

        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);

        model.ValueA = 999;
        model.ValueB = "XYZ";

        history.BeginBatch();

        Assert.Throws<InvalidOperationException>(() =>
            history.Redo()
        );
    }

    [Fact]
    public void Batch_recording_has_not_begun()
    {
        var history = new History();
        var model = new TestModel(history);

        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);

        model.ValueA = 999;
        model.ValueB = "XYZ";

        Assert.Throws<InvalidOperationException>(() =>
            history.EndBatch()
        );
    }

    public sealed class TestModel : INotifyPropertyChanged
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