using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;

namespace Jewelry.EditingSystem;

public class History : INotifyPropertyChanged
{
    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;
    public bool CanClear => CanUndo || CanRedo;
    public int UndoCount => _undoStack.Count;
    public int RedoCount => _redoStack.Count;
    public int PauseDepth { get; private set; }
    public int BatchDepth { get; private set; }

    public event PropertyChangedEventHandler? PropertyChanged;

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

    private BatchHistory? _batchHistory;

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
        Debug.Assert(_batchHistory is null);

        _batchHistory = new BatchHistory();
    }

    private void EndBatchInternal()
    {
        Debug.Assert(_batchHistory is not null);

#pragma warning disable CS8602
        if (_batchHistory.UndoRedoCount != (UndoCount:0, RedoCount:0))
#pragma warning restore CS8602
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

        var currentFlags = CanUndoRedoClear;
        var currentUndoRedoCount = UndoRedoCount;
        var currentDepth = PauseBatchDepth;

        var action = _undoStack.Pop();

        try
        {
            IsInUndoing = true;
            action.Undo();
        }
        finally
        {
            IsInUndoing = false;
        }

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

        var currentFlags = CanUndoRedoClear;
        var currentUndoRedoCount = UndoRedoCount;
        var currentDepth = PauseBatchDepth;

        var action = _redoStack.Pop();

        try
        {
            IsInUndoing = true;
            action.Redo();
        }
        finally
        {
            IsInUndoing = false;
        }

        _undoStack.Push(action);

        InvokePropertyChanged(currentFlags, currentUndoRedoCount, currentDepth);
    }

    public void Push(Action undo, Action redo)
    {
        if (IsInPaused)
            return;

        if (IsInBatch)
        {
            if (_batchHistory is null)
                throw new NullReferenceException();

            _batchHistory.Push(undo, redo);
            return;
        }

        var currentFlags = CanUndoRedoClear;
        var currentUndoRedoCount = UndoRedoCount;
        var currentDepth = PauseBatchDepth;

        _undoStack.Push(new HistoryAction(undo, redo));

        if (_redoStack.Count > 0)
            _redoStack.Clear();

        InvokePropertyChanged(currentFlags, currentUndoRedoCount, currentDepth);
    }

    public void Clear()
    {
        var currentFlags = CanUndoRedoClear;
        var currentUndoRedoCount = UndoRedoCount;
        var currentDepth = PauseBatchDepth;

        _undoStack.Clear();
        _redoStack.Clear();

        InvokePropertyChanged(currentFlags, currentUndoRedoCount, currentDepth);
    }
    
    internal void OnCollectionPropertyCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (IsInUndoing)
            return;

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                {
                    void DoRedo()
                    {
                        var list = (IList)sender;

                        var addItems = e.NewItems;
                        var addCount = addItems.Count;
                        var addIndex = e.NewStartingIndex;

                        // ICollectionItem
                        for (var i = 0; i != addCount; ++i)
                        {
                            list.Insert(addIndex + i, addItems[i]);

                            if (addItems[i] is ICollectionItem collItem)
                                collItem.Changed(CollectionItemChangedInfo.Add);
                        }
                    }

                    void DoUndo()
                    {
                        var list = (IList)sender;

                        var addItems = e.NewItems;
                        var addCount = addItems.Count;
                        var addIndex = e.NewStartingIndex;

                        // ICollectionItem
                        for (var i = 0; i != addCount; ++i)
                        {
                            list.RemoveAt(addIndex + i);

                            if (addItems[i] is ICollectionItem collItem)
                                collItem.Changed(CollectionItemChangedInfo.Remove);
                        }
                    }

                    // ICollectionItem
                    {
                        var addItems = e.NewItems;
                        var addCount = addItems.Count;

                        for (var i = 0; i != addCount; ++i)
                        {
                            if (addItems[i] is ICollectionItem collItem)
                                collItem.Changed(CollectionItemChangedInfo.Add);
                        }
                    }

                    Push(DoUndo, DoRedo);
                    break;
                }

            case NotifyCollectionChangedAction.Move:
                {
                    if (e.OldItems.Count != 1)
                        throw new NotImplementedException();

                    if (e.NewItems.Count != 1)
                        throw new NotImplementedException();

                    void DoRedo()
                    {
                        var list = (IList)sender;

                        var src = e.OldStartingIndex;
                        var dst = e.NewStartingIndex;

                        var item = list[src];
                        list.RemoveAt(src);

                        list.Insert(dst, item);

                        // ICollectionItem
                        {
                            if (item is ICollectionItem collItem)
                                collItem.Changed(CollectionItemChangedInfo.Move);
                        }
                    }

                    void DoUndo()
                    {
                        var list = (IList)sender;

                        var src = e.NewStartingIndex;
                        var dst = e.OldStartingIndex;

                        var item = list[src];
                        list.RemoveAt(src);

                        list.Insert(dst, item);

                        // ICollectionItem
                        {
                            if (item is ICollectionItem collItem)
                                collItem.Changed(CollectionItemChangedInfo.Move);
                        }
                    }

                    // ICollectionItem
                    {
                        if (e.OldItems[0] is ICollectionItem collItem)
                            collItem.Changed(CollectionItemChangedInfo.Move);
                    }

                    Push(DoUndo, DoRedo);
                    break;
                }

            case NotifyCollectionChangedAction.Remove:
                {
                    if (e.OldItems.Count != 1)
                        throw new NotImplementedException();

                    if (e.NewItems is not null)
                        throw new NotImplementedException();

                    var item = e.OldItems[0];

                    void DoRedo()
                    {
                        var list = (IList)sender;

                        item = list[e.OldStartingIndex];
                        list.RemoveAt(e.OldStartingIndex);

                        // ICollectionItem
                        {
                            if (item is ICollectionItem collItem)
                                collItem.Changed(CollectionItemChangedInfo.Remove);
                        }
                    }

                    void DoUndo()
                    {
                        var list = (IList)sender;

                        list.Insert(e.OldStartingIndex, item);

                        // ICollectionItem
                        {
                            if (item is ICollectionItem collItem)
                                collItem.Changed(CollectionItemChangedInfo.Add);
                        }
                    }

                    // ICollectionItem
                    {
                        if (e.OldItems[0] is ICollectionItem collItem)
                            collItem.Changed(CollectionItemChangedInfo.Remove);
                    }

                    Push(DoUndo, DoRedo);
                    break;
                }

            case NotifyCollectionChangedAction.Replace:
                {
                    if (e.OldItems.Count != 1)
                        throw new NotImplementedException();

                    if (e.NewItems.Count != 1)
                        throw new NotImplementedException();

                    if (e.NewStartingIndex != e.OldStartingIndex)
                        throw new NotImplementedException();

                    void DoRedo()
                    {
                        var list = (IList)sender;

                        var index = e.OldStartingIndex;
                        var oldItem = list[index];
                        list[index] = e.NewItems[0];

                        // ICollectionItem
                        {
                            if (oldItem is ICollectionItem oldCollItem)
                                oldCollItem.Changed(CollectionItemChangedInfo.Remove);

                            if (list[index] is ICollectionItem collItem)
                                collItem.Changed(CollectionItemChangedInfo.Add);
                        }
                    }

                    void DoUndo()
                    {
                        var list = (IList)sender;

                        var index = e.OldStartingIndex;
                        var oldItem = list[index];
                        list[index] = e.OldItems[0];

                        // ICollectionItem
                        {
                            if (oldItem is ICollectionItem oldCollItem)
                                oldCollItem.Changed(CollectionItemChangedInfo.Add);

                            if (list[index] is ICollectionItem collItem)
                                collItem.Changed(CollectionItemChangedInfo.Remove);
                        }
                    }

                    // ICollectionItem
                    {
                        if (e.OldItems[0] is ICollectionItem oldCollItem)
                            oldCollItem.Changed(CollectionItemChangedInfo.Remove);

                        if (e.NewItems[0] is ICollectionItem newCollItem)
                            newCollItem.Changed(CollectionItemChangedInfo.Add);
                    }

                    Push(DoUndo, DoRedo);
                    break;
                }

            case NotifyCollectionChangedAction.Reset:
                {
                    if (IsInPaused)
                        break;

                    throw new NotSupportedException("Clear() is not support. Use ClearEx()");
                }

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void InvokePropertyChanged((bool CanUndo, bool CanRedo, bool CanClear) flags, (int UndoCount, int RedoCount) undoRedoCount, (int PauseDepth, int BatchDepth) depthCount)
    {
        if (PropertyChanged is null)
            return;

        if (flags.CanUndo != CanUndo)
            PropertyChanged.Invoke(this, CanUndoArgs);

        if (flags.CanRedo != CanRedo)
            PropertyChanged.Invoke(this, CanRedoArgs);

        if (flags.CanClear != CanClear)
            PropertyChanged.Invoke(this, CanClearArgs);

        if (undoRedoCount.UndoCount != UndoCount)
            PropertyChanged.Invoke(this, UndoCountArgs);

        if (undoRedoCount.RedoCount != RedoCount)
            PropertyChanged.Invoke(this, RedoCountArgs);

        if (depthCount.PauseDepth != PauseDepth)
            PropertyChanged.Invoke(this, PauseDepthArgs);

        if (depthCount.BatchDepth != BatchDepth)
            PropertyChanged.Invoke(this, BatchDepthArgs);
    }

    internal bool IsInUndoing { get; private set; }

    private (int UndoCount, int RedoCount) UndoRedoCount => (UndoCount, RedoCount);
    private (bool CanUndo, bool CanRedo, bool CanClear) CanUndoRedoClear => (CanUndo, CanRedo, CanClear);
    private (int PauseDepth, int BatchDepth) PauseBatchDepth => (PauseDepth, BatchDepth);

    private readonly Stack<HistoryAction> _undoStack = new();
    private readonly Stack<HistoryAction> _redoStack = new();

    private static readonly PropertyChangedEventArgs CanUndoArgs = new(nameof(CanUndo));
    private static readonly PropertyChangedEventArgs CanRedoArgs = new(nameof(CanRedo));
    private static readonly PropertyChangedEventArgs CanClearArgs = new(nameof(CanClear));
    private static readonly PropertyChangedEventArgs UndoCountArgs = new(nameof(UndoCount));
    private static readonly PropertyChangedEventArgs RedoCountArgs = new(nameof(RedoCount));
    private static readonly PropertyChangedEventArgs PauseDepthArgs = new(nameof(PauseDepth));
    private static readonly PropertyChangedEventArgs BatchDepthArgs = new(nameof(BatchDepth));

    private record struct HistoryAction(Action Undo, Action Redo);
}