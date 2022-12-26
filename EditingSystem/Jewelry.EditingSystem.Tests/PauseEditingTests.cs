using System;
using Xunit;

namespace Jewelry.EditingSystem.Tests;

public class PauseEditingTests
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

    public class TestModel : EditableModelBase
    {
        public TestModel(History history)
        {
            SetupEditingSystem(history);
        }

        #region ValueA

        private int _ValueA;

        public int ValueA
        {
            get => _ValueA;
            set => SetEditableProperty(v => _ValueA = v, _ValueA, value);
        }

        #endregion

        #region ValueB

        private string _ValueB;

        public string ValueB
        {
            get => _ValueB;
            set => SetEditableProperty(v => _ValueB = v, _ValueB, value);
        }

        #endregion
    }
}