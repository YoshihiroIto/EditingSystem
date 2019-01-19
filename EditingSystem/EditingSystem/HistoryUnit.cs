using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace EditingSystem
{
    public class History : INotifyPropertyChanged
    {
        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        public event PropertyChangedEventHandler PropertyChanged;

        private readonly Stack<HistoryAction> _undoStack = new Stack<HistoryAction>();
        private readonly Stack<HistoryAction> _redoStack = new Stack<HistoryAction>();

        public void Push(Action undo, Action redo)
        {
            _undoStack.Push(new HistoryAction(undo, redo));

            if (_undoStack.Count == 1)
                PropertyChanged?.Invoke(this, CanUndoArgs);

            if (_redoStack.Count > 0)
            {
                _redoStack.Clear();
                PropertyChanged?.Invoke(this, CanRedoArgs);
            }
        }

        internal bool IsInUndoing { get; private set; }

        public void Undo()
        {
            if (CanUndo == false)
                return;

            var action = _undoStack.Pop();

            IsInUndoing = true;
            action.Undo();
            IsInUndoing = false;

            _redoStack.Push(action);

            if (_undoStack.Count == 0)
                PropertyChanged?.Invoke(this, CanUndoArgs);

            if (_redoStack.Count == 1)
                PropertyChanged?.Invoke(this, CanRedoArgs);
        }

        public void Redo()
        {
            if (CanRedo == false)
                return;

            var action = _redoStack.Pop();

            IsInUndoing = true;
            action.Redo();
            IsInUndoing = false;

            _undoStack.Push(action);

            if (_redoStack.Count == 0)
                PropertyChanged?.Invoke(this, CanRedoArgs);

            if (_undoStack.Count == 1)
                PropertyChanged?.Invoke(this, CanUndoArgs);
        }

        private static readonly PropertyChangedEventArgs CanUndoArgs = new PropertyChangedEventArgs(nameof(CanUndo));
        private static readonly PropertyChangedEventArgs CanRedoArgs = new PropertyChangedEventArgs(nameof(CanRedo));

        private class HistoryAction
        {
            public readonly Action Undo;
            public readonly Action Redo;

            public HistoryAction(Action undo, Action redo)
            {
                Undo = undo;
                Redo = redo;
            }
        }
    }
}