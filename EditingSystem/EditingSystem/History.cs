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
        public bool CanClear => CanUndo || CanRedo;
        public ValueTuple<int, int> UndoRedoCount => (_undoStack.Count, _redoStack.Count);
        public int PauseDepth { get; private set; }
        public int BatchDepth { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        #region Pause

        public bool IsInPaused => PauseDepth > 0;

        public void BeginPause()
        {
            ++PauseDepth;
        }

        public void EndPause()
        {
            if (PauseDepth == 0)
                throw new InvalidOperationException("Pause is not begun.");

            --PauseDepth;
        }
        #endregion

        #region Batch

        private BatchHistory _batchHistory;

        public bool IsInBatch => BatchDepth > 0;

        public void BeginBatch()
        {
            ++BatchDepth;

            if (BatchDepth == 1)
                BeginBatchInternal();
        }

        public void EndBatch()
        {
            if (BatchDepth == 0)
                throw new InvalidOperationException("Batch recording has not begun.");

            --BatchDepth;

            if (BatchDepth == 0)
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

            if (_batchHistory.UndoRedoCount != (0, 0))
                Push(_batchHistory.UndoAll, _batchHistory.RedoAll);

            _batchHistory = null;
        }

        private class BatchHistory : History
        {
            internal void UndoAll()
            {
                while (CanUndo)
                    Undo();
            }

            internal void RedoAll()
            {
                while (CanRedo)
                    Redo();
            }
        }

        #endregion

        public void Undo()
        {
            if (IsInBatch)
                throw new InvalidOperationException("Can't call Undo() during batch recording.");

            if (IsInPaused)
                throw new InvalidOperationException("Can't call Undo() during in paused.");

            if (CanUndo == false)
                return;

            var currentFlags = MakeCurrentFlags();
            var currentUndoRedoCount = UndoRedoCount;
            var currentDepth = (PauseDepth, BatchDepth);

            var action = _undoStack.Pop();

            IsInUndoing = true;
            action.Undo();
            IsInUndoing = false;

            _redoStack.Push(action);

            InvokePropertyChanged(currentFlags, currentUndoRedoCount, currentDepth);
        }

        public void Redo()
        {
            if (IsInBatch)
                throw new InvalidOperationException("Can't call Redo() during batch recording.");

            if (IsInPaused)
                throw new InvalidOperationException("Can't call Redo() during in paused.");

            if (CanRedo == false)
                return;

            var currentFlags = MakeCurrentFlags();
            var currentUndoRedoCount = UndoRedoCount;
            var currentDepth = (PauseDepth, BatchDepth);

            var action = _redoStack.Pop();

            IsInUndoing = true;
            action.Redo();
            IsInUndoing = false;

            _undoStack.Push(action);

            InvokePropertyChanged(currentFlags, currentUndoRedoCount, currentDepth);
        }

        public void Push(Action undo, Action redo)
        {
            if (IsInPaused)
                return;

            if (IsInBatch)
            {
                _batchHistory.Push(undo, redo);
                return;
            }

            var currentFlags = MakeCurrentFlags();
            var currentUndoRedoCount = UndoRedoCount;
            var currentDepth = (PauseDepth, BatchDepth);

            _undoStack.Push(new HistoryAction(undo, redo));

            if (_redoStack.Count > 0)
                _redoStack.Clear();

            InvokePropertyChanged(currentFlags, currentUndoRedoCount, currentDepth);
        }

        public void Clear()
        {
            var currentFlags = MakeCurrentFlags();
            var currentUndoRedoCount = UndoRedoCount;
            var currentDepth = (PauseDepth, BatchDepth);

            _undoStack.Clear();
            _redoStack.Clear();

            InvokePropertyChanged(currentFlags, currentUndoRedoCount, currentDepth);
        }

        private ValueTuple<bool, bool, bool> MakeCurrentFlags()
            => (CanUndo, CanRedo, CanClear);

        private void InvokePropertyChanged(in ValueTuple<bool, bool, bool> flags, in ValueTuple<int, int> undoRedoCount, ValueTuple<int, int> depthCount)
        {
            if (flags.Item1 != CanUndo)
                PropertyChanged?.Invoke(this, CanUndoArgs);

            if (flags.Item2 != CanRedo)
                PropertyChanged?.Invoke(this, CanRedoArgs);

            if (flags.Item3 != CanClear)
                PropertyChanged?.Invoke(this, CanClearArgs);

            if (undoRedoCount != UndoRedoCount)
                PropertyChanged?.Invoke(this, CanUndoRedoCountArgs);

            if (depthCount != (PauseDepth, BatchDepth))
            {
                PropertyChanged?.Invoke(this, PauseDepthArgs);
                PropertyChanged?.Invoke(this, BatchDepthArgs);
            }
        }

        internal bool IsInUndoing { get; private set; }

        private readonly Stack<HistoryAction> _undoStack = new Stack<HistoryAction>();
        private readonly Stack<HistoryAction> _redoStack = new Stack<HistoryAction>();

        private static readonly PropertyChangedEventArgs CanUndoArgs = new PropertyChangedEventArgs(nameof(CanUndo));
        private static readonly PropertyChangedEventArgs CanRedoArgs = new PropertyChangedEventArgs(nameof(CanRedo));
        private static readonly PropertyChangedEventArgs CanClearArgs = new PropertyChangedEventArgs(nameof(CanClear));
        private static readonly PropertyChangedEventArgs CanUndoRedoCountArgs = new PropertyChangedEventArgs(nameof(UndoRedoCount));
        private static readonly PropertyChangedEventArgs PauseDepthArgs = new PropertyChangedEventArgs(nameof(PauseDepth));
        private static readonly PropertyChangedEventArgs BatchDepthArgs = new PropertyChangedEventArgs(nameof(BatchDepth));

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
    }
}