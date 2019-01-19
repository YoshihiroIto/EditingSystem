using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

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
            if (IsInBatch)
            {
                _batchHistory.Push(undo, redo);
                return;
            }

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
            if (IsInBatch)
                throw new InvalidOperationException("Can't call Undo() during batch recording.");

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
            if (IsInBatch)
                throw new InvalidOperationException("Can't call Redo() during batch recording.");

            if (_batchHistory != null)
            {
                _batchHistory.Redo();
                return;
            }

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

        private struct HistoryAction
        {
            public readonly Action Undo;
            public readonly Action Redo;

            public HistoryAction(Action undo, Action redo)
            {
                Undo = undo;
                Redo = redo;
            }
        }

        #region Batch

        private int _batchDepth;
        private BatchHistory _batchHistory;

        private bool IsInBatch => _batchDepth > 0;

        public void BeginBatch()
        {
            ++_batchDepth;

            if (_batchDepth == 1)
                BeginBatchInternal();
        }

        public void EndBatch()
        {
            if (_batchDepth == 0)
                throw new InvalidOperationException("Batch recording has not begun.");

            --_batchDepth;

            if (_batchDepth == 0)
                EndBatchInternal();
        }

        private void BeginBatchInternal()
        {
            Debug.Assert(_batchHistory == null);

            _batchHistory = new BatchHistory();
        }

        private void EndBatchInternal()
        {
            Debug.Assert(_batchHistory != null);

            Push(_batchHistory.UndoAll, _batchHistory.RedoAll);

            _batchHistory = null;
        }

        private class BatchHistory : History
        {
            public void UndoAll()
            {
                while (CanUndo)
                    Undo();
            }

            public void RedoAll()
            {
                while (CanRedo)
                    Redo();
            }
        }

        #endregion
    }
}