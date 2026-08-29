using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;

namespace Jewelry.EditingSystem;

public class History : INotifyPropertyChanged, IDisposable
{
    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;
    public bool CanClear => CanUndo || CanRedo;
    public int UndoCount => _undoStack.Count;
    public int RedoCount => _redoStack.Count;
    public int MaxUndoCount
    {
        get => _maxUndoCount;
        set
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value));
            if (_maxUndoCount == value)
                return;

            var currentFlags = CanUndoRedoClear;
            var currentUndoRedoCount = UndoRedoCount;
            var currentDepth = PauseBatchDepth;

            _maxUndoCount = value;
            _undoStack.TrimOldest(value);
            _redoStack.TrimOldest(value);

            PropertyChanged?.Invoke(this, MaxUndoCountArgs);
            InvokePropertyChanged(currentFlags, currentUndoRedoCount, currentDepth);
        }
    }
    public int PauseDepth { get; private set; }
    public int BatchDepth { get; private set; }
    public bool IsInUndoing { get; private set; }
    public bool IsInPaused => PauseDepth > 0;
    public bool IsInBatch => BatchDepth > 0;

    public event PropertyChangedEventHandler? PropertyChanged;

    internal readonly CollectionChangedWeakEventManager CollectionChangedWeakEventManager = new();

    public void Dispose()
    {
        CollectionChangedWeakEventManager.Dispose();
    }

    public void BeginPause()
    {
        var wasInPaused = IsInPaused;
        ++PauseDepth;
        PropertyChanged?.Invoke(this, PauseDepthArgs);

        if (wasInPaused != IsInPaused)
            PropertyChanged?.Invoke(this, IsInPausedArgs);
    }

    /// <summary>
    /// Pauses history recording until the returned scope is disposed.
    /// </summary>
    public IDisposable Pause()
    {
        BeginPause();
        return new RecordingScope(this, isBatch: false);
    }

    public void EndPause()
    {
        if (PauseDepth is 0)
            throw new InvalidOperationException("Pause is not begun.");

        var wasInPaused = IsInPaused;
        --PauseDepth;
        PropertyChanged?.Invoke(this, PauseDepthArgs);

        if (wasInPaused != IsInPaused)
            PropertyChanged?.Invoke(this, IsInPausedArgs);
    }

    public void BeginBatch()
    {
        var wasInBatch = IsInBatch;
        ++BatchDepth;

        if (BatchDepth is 1)
            BeginBatchInternal();

        PropertyChanged?.Invoke(this, BatchDepthArgs);

        if (wasInBatch != IsInBatch)
            PropertyChanged?.Invoke(this, IsInBatchArgs);
    }

    /// <summary>
    /// Groups recorded changes into one undo action until the returned scope is disposed.
    /// </summary>
    public IDisposable Batch()
    {
        BeginBatch();
        return new RecordingScope(this, isBatch: true);
    }

    public void EndBatch()
    {
        if (BatchDepth is 0)
            throw new InvalidOperationException("Batch recording has not begun.");

        var wasInBatch = IsInBatch;
        --BatchDepth;

        if (BatchDepth is 0)
            EndBatchInternal();

        PropertyChanged?.Invoke(this, BatchDepthArgs);

        if (wasInBatch != IsInBatch)
            PropertyChanged?.Invoke(this, IsInBatchArgs);
    }

    public void Undo()
    {
        if (IsInBatch)
            throw new InvalidOperationException("Can't call Undo() during batch recording.");

        if (IsInPaused)
            throw new InvalidOperationException("Can't call Undo() during in paused.");

        if (CanUndo is false)
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
        catch
        {
            _undoStack.Push(action, MaxUndoCount);
            throw;
        }
        finally
        {
            IsInUndoing = false;
        }

        _redoStack.Push(action, MaxUndoCount);

        InvokePropertyChanged(currentFlags, currentUndoRedoCount, currentDepth);
    }

    public bool TryUndo()
    {
        if (CanUndo is false)
            return false;

        Undo();
        return true;
    }

    public void Redo()
    {
        if (IsInBatch)
            throw new InvalidOperationException("Can't call Redo() during batch recording.");

        if (IsInPaused)
            throw new InvalidOperationException("Can't call Redo() during in paused.");

        if (CanRedo is false)
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
        catch
        {
            _redoStack.Push(action, MaxUndoCount);
            throw;
        }
        finally
        {
            IsInUndoing = false;
        }

        _undoStack.Push(action, MaxUndoCount);

        InvokePropertyChanged(currentFlags, currentUndoRedoCount, currentDepth);
    }

    public bool TryRedo()
    {
        if (CanRedo is false)
            return false;

        Redo();
        return true;
    }

    public void Push(Action undo, Action redo)
    {
        if (undo is null)
            throw new ArgumentNullException(nameof(undo));
        if (redo is null)
            throw new ArgumentNullException(nameof(redo));

        if (IsInPaused || IsInUndoing)
            return;

        if (IsInBatch)
        {
            _ = _batchHistory ?? throw new NullReferenceException();

            _batchHistory.Push(undo, redo);
            return;
        }

        var currentFlags = CanUndoRedoClear;
        var currentUndoRedoCount = UndoRedoCount;
        var currentDepth = PauseBatchDepth;

        _undoStack.Push(new HistoryAction(undo, redo), MaxUndoCount);

        if (_redoStack.Count > 0)
            _redoStack.Clear();

        InvokePropertyChanged(currentFlags, currentUndoRedoCount, currentDepth);
    }

    /// <summary>
    /// Records a property change without applying <paramref name="newValue"/> immediately.
    /// </summary>
    /// <remarks>
    /// This is intended for property setters that apply the value themselves after recording the
    /// change. Undo and redo use <paramref name="setValue"/> so the original setter pipeline is
    /// executed again.
    /// </remarks>
    public bool RecordPropertyChange<T>(Action<T> setValue, T oldValue, T newValue)
    {
        if (setValue is null)
            throw new ArgumentNullException(nameof(setValue));
        return EditablePropertyCommon.RecordPropertyChange(this, setValue, oldValue, newValue);
    }

    /// <summary>
    /// Records a property change that has already been applied successfully.
    /// </summary>
    /// <remarks>
    /// Use this from post-change hooks. Undo and redo use <paramref name="setValue"/> so the
    /// original setter pipeline is executed again.
    /// </remarks>
    public void RecordAppliedPropertyChange<T>(Action<T> setValue, T oldValue, T newValue)
    {
        if (setValue is null)
            throw new ArgumentNullException(nameof(setValue));

        if (EqualityComparer<T>.Default.Equals(oldValue, newValue))
            return;

        EditablePropertyCommon.RecordAppliedPropertyChange(this, setValue, oldValue, newValue);
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

    internal void OnCollectionPropertyCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (IsInUndoing)
            return;

        if (IsInPaused)
        {
            NotifyPausedCollectionChange(sender, e);
            return;
        }

        var list = sender as IList;
        var collection = list is null && e.Action is not NotifyCollectionChangedAction.Reset
            ? CollectionAdapter.Create(sender ?? throw new NullReferenceException())
            : null;

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
            {
                    void DoRedo()
                    {
                        var addItems = e.NewItems ?? throw new NullReferenceException();
                        var addCount = addItems.Count;

                        // ICollectionItem
                        for (var i = 0; i != addCount; ++i)
                        {
                            if (list is not null)
                                list.Insert(e.NewStartingIndex + i, addItems[i]);
                            else
                                collection!.Add(addItems[i]);

                            if (addItems[i] is ICollectionItem collItem)
                                collItem.Changed(CollectionItemChangedInfo.Add);
                        }
                    }

                    void DoUndo()
                    {
                        var addItems = e.NewItems ?? throw new NullReferenceException();
                        var addCount = addItems.Count;

                        // ICollectionItem
                        for (var i = 0; i != addCount; ++i)
                        {
                            if (list is not null)
                                list.RemoveAt(e.NewStartingIndex);
                            else
                                collection!.Remove(addItems[i]);

                            if (addItems[i] is ICollectionItem collItem)
                                collItem.Changed(CollectionItemChangedInfo.Remove);
                        }
                    }

                    // ICollectionItem
                    {
                        var addItems = e.NewItems ?? throw new NullReferenceException();
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
                    if (list is null)
                        throw new NotSupportedException("Move is only supported for IList collections.");

                    var movedItems = SnapshotItems(e.OldItems ?? throw new NullReferenceException());
                    if (movedItems.Count != (e.NewItems ?? throw new NullReferenceException()).Count)
                        throw new InvalidOperationException("Move item counts do not match.");

                    void DoRedo()
                    {
                        MoveItems(sender!, list, e.OldStartingIndex, e.NewStartingIndex, movedItems.Count);
                        NotifyCollectionItems(movedItems, CollectionItemChangedInfo.Move);
                    }

                    void DoUndo()
                    {
                        MoveItems(sender!, list, e.NewStartingIndex, e.OldStartingIndex, movedItems.Count);
                        NotifyCollectionItems(movedItems, CollectionItemChangedInfo.Move);
                    }

                    NotifyCollectionItems(movedItems, CollectionItemChangedInfo.Move);

                    Push(DoUndo, DoRedo);
                    break;
            }

            case NotifyCollectionChangedAction.Remove:
            {
                    if (e.NewItems is not null)
                        throw new InvalidOperationException("Remove notifications cannot contain new items.");

                    var removedItems = SnapshotItems(e.OldItems ?? throw new NullReferenceException());

                    void DoRedo()
                    {
                        if (list is not null)
                        {
                            for (var i = 0; i < removedItems.Count; ++i)
                                list.RemoveAt(e.OldStartingIndex);
                        }
                        else
                        {
                            foreach (var item in removedItems)
                                collection!.Remove(item);
                        }

                        NotifyCollectionItems(removedItems, CollectionItemChangedInfo.Remove);
                    }

                    void DoUndo()
                    {
                        if (list is not null)
                        {
                            for (var i = 0; i < removedItems.Count; ++i)
                                list.Insert(e.OldStartingIndex + i, removedItems[i]);
                        }
                        else
                        {
                            foreach (var item in removedItems)
                                collection!.Add(item);
                        }

                        NotifyCollectionItems(removedItems, CollectionItemChangedInfo.Add);
                    }

                    NotifyCollectionItems(removedItems, CollectionItemChangedInfo.Remove);

                    Push(DoUndo, DoRedo);
                    break;
            }

            case NotifyCollectionChangedAction.Replace:
            {
                    if (e.NewStartingIndex != e.OldStartingIndex)
                        throw new InvalidOperationException("Replace indices do not match.");

                    var oldItems = SnapshotItems(e.OldItems ?? throw new NullReferenceException());
                    var newItems = SnapshotItems(e.NewItems ?? throw new NullReferenceException());

                    void DoRedo()
                    {
                        if (list is not null)
                            ReplaceItems(list, e.OldStartingIndex, oldItems.Count, newItems);
                        else
                        {
                            foreach (var item in oldItems)
                                collection!.Remove(item);
                            foreach (var item in newItems)
                                collection!.Add(item);
                        }

                        NotifyCollectionItems(oldItems, CollectionItemChangedInfo.Remove);
                        NotifyCollectionItems(newItems, CollectionItemChangedInfo.Add);
                    }

                    void DoUndo()
                    {
                        if (list is not null)
                            ReplaceItems(list, e.OldStartingIndex, newItems.Count, oldItems);
                        else
                        {
                            foreach (var item in newItems)
                                collection!.Remove(item);
                            foreach (var item in oldItems)
                                collection!.Add(item);
                        }

                        NotifyCollectionItems(newItems, CollectionItemChangedInfo.Remove);
                        NotifyCollectionItems(oldItems, CollectionItemChangedInfo.Add);
                    }

                    NotifyCollectionItems(oldItems, CollectionItemChangedInfo.Remove);
                    NotifyCollectionItems(newItems, CollectionItemChangedInfo.Add);

                    Push(DoUndo, DoRedo);
                    break;
            }

            case NotifyCollectionChangedAction.Reset:
            {
                var notifyCollection = sender as INotifyCollectionChanged ?? throw new NullReferenceException();
                    var oldItems = CollectionChangedWeakEventManager.GetSnapshot(notifyCollection);
                    var newItems = SnapshotItems(sender as IEnumerable ?? throw new NotSupportedException(
                        $"Collection type '{sender?.GetType()}' must implement IEnumerable."));

                    if (ItemsEqual(oldItems, newItems))
                        break;

                    void DoUndo()
                    {
                        ReplaceAllItems(sender!, list, newItems, oldItems);
                        NotifyCollectionItems(newItems, CollectionItemChangedInfo.Remove);
                        NotifyCollectionItems(oldItems, CollectionItemChangedInfo.Add);
                    }

                    void DoRedo()
                    {
                        ReplaceAllItems(sender!, list, oldItems, newItems);
                        NotifyCollectionItems(oldItems, CollectionItemChangedInfo.Remove);
                        NotifyCollectionItems(newItems, CollectionItemChangedInfo.Add);
                    }

                    NotifyCollectionItems(oldItems, CollectionItemChangedInfo.Remove);
                    NotifyCollectionItems(newItems, CollectionItemChangedInfo.Add);
                    Push(DoUndo, DoRedo);
                    break;
            }

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private static List<object?> SnapshotItems(IEnumerable items)
    {
        var snapshot = new List<object?>();
        foreach (var item in items)
            snapshot.Add(item);

        return snapshot;
    }

    private static void MoveItems(
        object collectionObject,
        IList list,
        int sourceIndex,
        int destinationIndex,
        int count)
    {
        if (count is 1 && CollectionAdapter.TryMove(collectionObject, sourceIndex, destinationIndex))
            return;

        var items = new List<object?>(count);
        for (var i = 0; i < count; ++i)
        {
            items.Add(list[sourceIndex]);
            list.RemoveAt(sourceIndex);
        }

        for (var i = 0; i < items.Count; ++i)
            list.Insert(destinationIndex + i, items[i]);
    }

    private static void ReplaceItems(
        IList list,
        int index,
        int removeCount,
        IReadOnlyList<object?> replacementItems)
    {
        if (removeCount == replacementItems.Count)
        {
            for (var i = 0; i < replacementItems.Count; ++i)
                list[index + i] = replacementItems[i];
            return;
        }

        for (var i = 0; i < removeCount; ++i)
            list.RemoveAt(index);

        for (var i = 0; i < replacementItems.Count; ++i)
            list.Insert(index + i, replacementItems[i]);
    }

    private static void ReplaceAllItems(
        object collectionObject,
        IList? list,
        IReadOnlyList<object?> currentItems,
        IReadOnlyList<object?> replacementItems)
    {
        if (list is not null)
        {
            while (list.Count > 0)
                list.RemoveAt(list.Count - 1);
            foreach (var item in replacementItems)
                list.Add(item);
            return;
        }

        var adapter = CollectionAdapter.Create(collectionObject);
        foreach (var item in currentItems)
            adapter.Remove(item);
        foreach (var item in replacementItems)
            adapter.Add(item);
    }

    private static bool ItemsEqual(IReadOnlyList<object?> left, IReadOnlyList<object?> right)
    {
        if (left.Count != right.Count)
            return false;

        for (var i = 0; i < left.Count; ++i)
        {
            if (Equals(left[i], right[i]) is false)
                return false;
        }

        return true;
    }

    private static void NotifyCollectionItems(
        IEnumerable items,
        in CollectionItemChangedInfo changedInfo)
    {
        foreach (var item in items)
        {
            if (item is ICollectionItem collectionItem)
                collectionItem.Changed(changedInfo);
        }
    }

    private void NotifyPausedCollectionChange(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                NotifyCollectionItems(e.NewItems ?? throw new NullReferenceException(), CollectionItemChangedInfo.Add);
                break;

            case NotifyCollectionChangedAction.Remove:
                NotifyCollectionItems(e.OldItems ?? throw new NullReferenceException(), CollectionItemChangedInfo.Remove);
                break;

            case NotifyCollectionChangedAction.Move:
                NotifyCollectionItems(e.OldItems ?? throw new NullReferenceException(), CollectionItemChangedInfo.Move);
                break;

            case NotifyCollectionChangedAction.Replace:
                NotifyCollectionItems(e.OldItems ?? throw new NullReferenceException(), CollectionItemChangedInfo.Remove);
                NotifyCollectionItems(e.NewItems ?? throw new NullReferenceException(), CollectionItemChangedInfo.Add);
                break;

            case NotifyCollectionChangedAction.Reset:
            {
                var notifyCollection = sender as INotifyCollectionChanged ?? throw new NullReferenceException();
                NotifyCollectionItems(
                    CollectionChangedWeakEventManager.GetSnapshot(notifyCollection),
                    CollectionItemChangedInfo.Remove);
                NotifyCollectionItems(
                    sender as IEnumerable ?? throw new NotSupportedException(
                        $"Collection type '{sender?.GetType()}' must implement IEnumerable."),
                    CollectionItemChangedInfo.Add);
                break;
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

    private void BeginBatchInternal()
    {
        Debug.Assert(_batchHistory is null);

        _batchHistory = new BatchHistory();
    }

    private void EndBatchInternal()
    {
        var batchHistory = _batchHistory ?? throw new InvalidOperationException();

        if (batchHistory.UndoRedoCount != (UndoCount: 0, RedoCount: 0))
            Push(batchHistory.UndoAll, batchHistory.RedoAll);

        batchHistory.Dispose();
        _batchHistory = null;
    }

    private (int UndoCount, int RedoCount) UndoRedoCount => (UndoCount, RedoCount);
    private (bool CanUndo, bool CanRedo, bool CanClear) CanUndoRedoClear => (CanUndo, CanRedo, CanClear);
    private (int PauseDepth, int BatchDepth) PauseBatchDepth => (PauseDepth, BatchDepth);

    private BatchHistory? _batchHistory;

    private readonly HistoryStack<HistoryAction> _undoStack = new();
    private readonly HistoryStack<HistoryAction> _redoStack = new();
    private int _maxUndoCount = int.MaxValue;

    private static readonly PropertyChangedEventArgs CanUndoArgs = new(nameof(CanUndo));
    private static readonly PropertyChangedEventArgs CanRedoArgs = new(nameof(CanRedo));
    private static readonly PropertyChangedEventArgs CanClearArgs = new(nameof(CanClear));
    private static readonly PropertyChangedEventArgs UndoCountArgs = new(nameof(UndoCount));
    private static readonly PropertyChangedEventArgs RedoCountArgs = new(nameof(RedoCount));
    private static readonly PropertyChangedEventArgs MaxUndoCountArgs = new(nameof(MaxUndoCount));
    private static readonly PropertyChangedEventArgs PauseDepthArgs = new(nameof(PauseDepth));
    private static readonly PropertyChangedEventArgs BatchDepthArgs = new(nameof(BatchDepth));
    private static readonly PropertyChangedEventArgs IsInPausedArgs = new(nameof(IsInPaused));
    private static readonly PropertyChangedEventArgs IsInBatchArgs = new(nameof(IsInBatch));

    private sealed class BatchHistory : History
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

    private sealed class RecordingScope : IDisposable
    {
        public RecordingScope(History history, bool isBatch)
        {
            _history = history;
            _isBatch = isBatch;
        }

        public void Dispose()
        {
            var history = _history;
            if (history is null)
                return;

            _history = null;
            if (_isBatch)
                history.EndBatch();
            else
                history.EndPause();
        }

        private History? _history;
        private readonly bool _isBatch;
    }

    private sealed class HistoryStack<T>
    {
        public int Count => _count;

        public void Push(T item, int maxCount)
        {
            if (maxCount is 0)
                return;

            if (_count == maxCount)
                RemoveOldest();

            EnsureCapacity(_count + 1, maxCount);
            _items[(_start + _count) % _items.Length] = item;
            ++_count;
        }

        public T Pop()
        {
            if (_count is 0)
                throw new InvalidOperationException("The history stack is empty.");

            var index = (_start + _count - 1) % _items.Length;
            var item = _items[index];
            _items[index] = default!;
            --_count;

            if (_count is 0)
                _start = 0;

            return item;
        }

        public void TrimOldest(int maxCount)
        {
            while (_count > maxCount)
                RemoveOldest();
        }

        public void Clear()
        {
            if (_count > 0)
                Array.Clear(_items, 0, _items.Length);
            _start = 0;
            _count = 0;
        }

        private void EnsureCapacity(int requiredCount, int maxCount)
        {
            if (_items.Length >= requiredCount)
                return;

            var newCapacity = _items.Length is 0
                ? 4
                : _items.Length <= int.MaxValue / 2
                    ? _items.Length * 2
                    : int.MaxValue;
            if (newCapacity < requiredCount)
                newCapacity = requiredCount;
            if (newCapacity > maxCount)
                newCapacity = maxCount;

            var newItems = new T[newCapacity];
            for (var i = 0; i < _count; ++i)
                newItems[i] = _items[(_start + i) % _items.Length];

            _items = newItems;
            _start = 0;
        }

        private void RemoveOldest()
        {
            if (_count is 0)
                return;

            _items[_start] = default!;
            _start = (_start + 1) % _items.Length;
            --_count;

            if (_count is 0)
                _start = 0;
        }

        private T[] _items = Array.Empty<T>();
        private int _start;
        private int _count;
    }

    private record struct HistoryAction(Action Undo, Action Redo);
}
