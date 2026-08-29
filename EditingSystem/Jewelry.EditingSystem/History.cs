using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

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
        return new RecordingScope(this, RecordingScopeKind.Pause);
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
        BeginBatchCore(isCoalescing: false);
    }

    /// <summary>
    /// Begins a batch that coalesces repeated changes to the same target property.
    /// </summary>
    public void BeginCoalescingBatch()
    {
        BeginBatchCore(isCoalescing: true);
    }

    private void BeginBatchCore(bool isCoalescing)
    {
        if (BatchDepth > 0 && _isCoalescingBatch != isCoalescing)
            throw new InvalidOperationException("Regular and coalescing batches cannot be nested together.");

        var wasInBatch = IsInBatch;
        ++BatchDepth;

        if (BatchDepth is 1)
        {
            _isCoalescingBatch = isCoalescing;
            BeginBatchInternal(isCoalescing);
        }

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
        return new RecordingScope(this, RecordingScopeKind.Batch);
    }

    /// <summary>
    /// Groups recorded changes into one undo action and coalesces repeated changes to the same
    /// target property until the returned scope is disposed.
    /// </summary>
    public IDisposable CoalescingBatch()
    {
        BeginCoalescingBatch();
        return new RecordingScope(this, RecordingScopeKind.CoalescingBatch);
    }

    public void EndBatch()
    {
        EndBatchCore(isCoalescing: false);
    }

    /// <summary>
    /// Ends a batch begun by <see cref="BeginCoalescingBatch"/>.
    /// </summary>
    public void EndCoalescingBatch()
    {
        EndBatchCore(isCoalescing: true);
    }

    private void EndBatchCore(bool isCoalescing)
    {
        if (BatchDepth is 0)
            throw new InvalidOperationException("Batch recording has not begun.");
        if (_isCoalescingBatch != isCoalescing)
            throw new InvalidOperationException(isCoalescing
                ? "A regular batch is active. Call EndBatch()."
                : "A coalescing batch is active. Call EndCoalescingBatch().");

        var wasInBatch = IsInBatch;
        --BatchDepth;

        if (BatchDepth is 0)
        {
            EndBatchInternal();
            _isCoalescingBatch = false;
        }

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

        PushAction(new DelegateHistoryAction(undo, redo));
    }

    internal void PushPropertyChange<T>(
        object? target,
        object? propertyKey,
        Action<T> setValue,
        T oldValue,
        T newValue,
        EditableModelBase? notificationTarget = null,
        string? notificationPropertyName = null)
    {
        if (IsInPaused || IsInUndoing)
            return;
        if (!IsInBatch && MaxUndoCount is 0)
            return;

        PropertyChangeKey? key = target is not null && propertyKey is not null
            ? new PropertyChangeKey(target, propertyKey)
            : null;
        PushAction(new PropertyHistoryAction<T>(
            this,
            key,
            setValue,
            oldValue,
            newValue,
            notificationTarget,
            notificationPropertyName));
    }

    private void PushAction(HistoryAction action)
    {
        if (IsInPaused || IsInUndoing)
            return;

        if (IsInBatch)
        {
            var batchRecorder = _batchRecorder ?? throw new InvalidOperationException();
            batchRecorder.Add(action);
            return;
        }

        PushRecordedAction(action);
    }

    private void PushRecordedAction(HistoryAction action)
    {
        var currentFlags = CanUndoRedoClear;
        var currentUndoRedoCount = UndoRedoCount;
        var currentDepth = PauseBatchDepth;

        _undoStack.Push(action, MaxUndoCount);

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

        return setValue.Target is { } target
            ? EditablePropertyCommon.RecordPropertyChange(
                this,
                target,
                setValue.Method,
                setValue,
                oldValue,
                newValue)
            : EditablePropertyCommon.RecordPropertyChange(this, setValue, oldValue, newValue);
    }

    public bool RecordPropertyChange<T>(
        object target,
        string propertyName,
        Action<T> setValue,
        T oldValue,
        T newValue)
    {
        if (target is null)
            throw new ArgumentNullException(nameof(target));
        if (string.IsNullOrEmpty(propertyName))
            throw new ArgumentException("A property name is required.", nameof(propertyName));
        if (setValue is null)
            throw new ArgumentNullException(nameof(setValue));

        return EditablePropertyCommon.RecordPropertyChange(
            this,
            target,
            propertyName,
            setValue,
            oldValue,
            newValue);
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

        if (setValue.Target is { } target)
        {
            EditablePropertyCommon.RecordAppliedPropertyChange(
                this,
                target,
                setValue.Method,
                setValue,
                oldValue,
                newValue);
        }
        else
            EditablePropertyCommon.RecordAppliedPropertyChange(this, setValue, oldValue, newValue);
    }

    public void RecordAppliedPropertyChange<T>(
        object target,
        string propertyName,
        Action<T> setValue,
        T oldValue,
        T newValue)
    {
        if (target is null)
            throw new ArgumentNullException(nameof(target));
        if (string.IsNullOrEmpty(propertyName))
            throw new ArgumentException("A property name is required.", nameof(propertyName));
        if (setValue is null)
            throw new ArgumentNullException(nameof(setValue));
        if (EqualityComparer<T>.Default.Equals(oldValue, newValue))
            return;

        EditablePropertyCommon.RecordAppliedPropertyChange(
            this,
            target,
            propertyName,
            setValue,
            oldValue,
            newValue);
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

                var addItems = e.NewItems ?? throw new NullReferenceException();
                var addCount = addItems.Count;
                for (var i = 0; i != addCount; ++i)
                {
                    if (addItems[i] is ICollectionItem collItem)
                        collItem.Changed(CollectionItemChangedInfo.Add);
                }

                Push(DoUndo, DoRedo);
                break;
            }

            case NotifyCollectionChangedAction.Move:
            {
                if (list is null)
                    throw new NotSupportedException("Move is only supported for IList collections.");

                var oldItems = e.OldItems ?? throw new NullReferenceException();
                var newItems = e.NewItems ?? throw new NullReferenceException();
                if (oldItems.Count != newItems.Count)
                    throw new InvalidOperationException("Move item counts do not match.");

                if (oldItems.Count is 1)
                {
                    var movedItem = list[e.NewStartingIndex];
                    if (movedItem is ICollectionItem collectionItem)
                        collectionItem.Changed(CollectionItemChangedInfo.Move);

                    PushAction(new MoveHistoryAction(
                        sender!,
                        e.OldStartingIndex,
                        e.NewStartingIndex,
                        movedItem));
                    break;
                }

                var movedItems = SnapshotItems(oldItems);

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
        var snapshot = items is ICollection collection
            ? new List<object?>(collection.Count)
            : new List<object?>();
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
        if (count is 1)
        {
            if (CollectionAdapter.TryMove(collectionObject, sourceIndex, destinationIndex))
                return;

            var item = list[sourceIndex];
            list.RemoveAt(sourceIndex);
            list.Insert(destinationIndex, item);
            return;
        }

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

    private void BeginBatchInternal(bool isCoalescing)
    {
        Debug.Assert(_batchRecorder is null);
        _batchRecorder = new BatchRecorder(isCoalescing);
    }

    private void EndBatchInternal()
    {
        var batchRecorder = _batchRecorder ?? throw new InvalidOperationException();
        _batchRecorder = null;

        if (batchRecorder.CreateAction() is { } action)
            PushRecordedAction(action);
    }

    private (int UndoCount, int RedoCount) UndoRedoCount => (UndoCount, RedoCount);
    private (bool CanUndo, bool CanRedo, bool CanClear) CanUndoRedoClear => (CanUndo, CanRedo, CanClear);
    private (int PauseDepth, int BatchDepth) PauseBatchDepth => (PauseDepth, BatchDepth);

    private BatchRecorder? _batchRecorder;
    private bool _isCoalescingBatch;

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

    private sealed class RecordingScope : IDisposable
    {
        public RecordingScope(History history, RecordingScopeKind kind)
        {
            _history = history;
            _kind = kind;
        }

        public void Dispose()
        {
            var history = _history;
            if (history is null)
                return;

            _history = null;
            switch (_kind)
            {
                case RecordingScopeKind.Pause:
                    history.EndPause();
                    break;
                case RecordingScopeKind.Batch:
                    history.EndBatch();
                    break;
                case RecordingScopeKind.CoalescingBatch:
                    history.EndCoalescingBatch();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private History? _history;
        private readonly RecordingScopeKind _kind;
    }

    private enum RecordingScopeKind
    {
        Pause,
        Batch,
        CoalescingBatch,
    }

    private sealed class BatchRecorder
    {
        public BatchRecorder(bool isCoalescing)
        {
            _coalescingIndices = isCoalescing
                ? new Dictionary<PropertyChangeKey, int>()
                : null;
        }

        public void Add(HistoryAction action)
        {
            if (_coalescingIndices is null)
            {
                _actions.Add(action);
                return;
            }

            if (action.CoalescingKey is not { } key)
            {
                _coalescingIndices.Clear();
                _actions.Add(action);
                return;
            }

            if (_coalescingIndices.TryGetValue(key, out var index) &&
                _actions[index].TryMerge(action))
                return;

            _coalescingIndices[key] = _actions.Count;
            _actions.Add(action);
        }

        public BatchHistoryAction? CreateAction()
        {
            var writeIndex = 0;
            for (var readIndex = 0; readIndex < _actions.Count; ++readIndex)
            {
                var action = _actions[readIndex];
                if (action.IsNoOp)
                    continue;

                _actions[writeIndex++] = action;
            }

            if (writeIndex < _actions.Count)
                _actions.RemoveRange(writeIndex, _actions.Count - writeIndex);

            return _actions.Count is 0 ? null : new BatchHistoryAction(_actions);
        }

        private readonly List<HistoryAction> _actions = new();
        private readonly Dictionary<PropertyChangeKey, int>? _coalescingIndices;
    }

    private readonly struct PropertyChangeKey : IEquatable<PropertyChangeKey>
    {
        public PropertyChangeKey(object target, object propertyKey)
        {
            Target = target;
            PropertyKey = propertyKey;
        }

        public object Target { get; }
        public object PropertyKey { get; }

        public bool Equals(PropertyChangeKey other)
        {
            return ReferenceEquals(Target, other.Target) &&
                object.Equals(PropertyKey, other.PropertyKey);
        }

        public override bool Equals(object? obj)
        {
            return obj is PropertyChangeKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (RuntimeHelpers.GetHashCode(Target) * 397) ^
                    PropertyKey.GetHashCode();
            }
        }
    }

    private abstract class HistoryAction
    {
        public abstract void Undo();
        public abstract void Redo();
        public virtual PropertyChangeKey? CoalescingKey => null;
        public virtual bool IsNoOp => false;
        public virtual bool TryMerge(HistoryAction newerAction) => false;
    }

    private sealed class DelegateHistoryAction(Action undo, Action redo) : HistoryAction
    {
        public override void Undo() => undo();
        public override void Redo() => redo();
    }

    private sealed class MoveHistoryAction(
        object collectionObject,
        int oldIndex,
        int newIndex,
        object? movedItem) : HistoryAction
    {
        public override void Undo() => Apply(newIndex, oldIndex);
        public override void Redo() => Apply(oldIndex, newIndex);

        private void Apply(int sourceIndex, int destinationIndex)
        {
            MoveItems(collectionObject, (IList)collectionObject, sourceIndex, destinationIndex, 1);
            if (movedItem is ICollectionItem collectionItem)
                collectionItem.Changed(CollectionItemChangedInfo.Move);
        }
    }

    private sealed class BatchHistoryAction(List<HistoryAction> actions) : HistoryAction
    {
        public override void Undo()
        {
            while (_appliedCount > 0)
            {
                actions[_appliedCount - 1].Undo();
                --_appliedCount;
            }
        }

        public override void Redo()
        {
            while (_appliedCount < actions.Count)
            {
                actions[_appliedCount].Redo();
                ++_appliedCount;
            }
        }

        private int _appliedCount = actions.Count;
    }

    private sealed class PropertyHistoryAction<T> : HistoryAction
    {
        public PropertyHistoryAction(
            History history,
            PropertyChangeKey? key,
            Action<T> setValue,
            T oldValue,
            T newValue,
            EditableModelBase? notificationTarget,
            string? notificationPropertyName)
        {
            _history = history;
            _key = key;
            _setValue = setValue;
            _oldValue = oldValue;
            _newValue = newValue;
            _notificationTarget = notificationTarget;
            _notificationPropertyName = notificationPropertyName;
        }

        public override PropertyChangeKey? CoalescingKey => _key;
        public override bool IsNoOp => EqualityComparer<T>.Default.Equals(_oldValue, _newValue);

        public override void Undo() => Apply(_newValue, _oldValue);
        public override void Redo() => Apply(_oldValue, _newValue);

        public override bool TryMerge(HistoryAction newerAction)
        {
            if (_key is not { } key ||
                newerAction is not PropertyHistoryAction<T> newer ||
                newer._key is not { } newerKey ||
                key.Equals(newerKey) is false)
                return false;

            _setValue = newer._setValue;
            _newValue = newer._newValue;
            _notificationTarget = newer._notificationTarget;
            _notificationPropertyName = newer._notificationPropertyName;
            return true;
        }

        private void Apply(T currentValue, T value)
        {
            EditablePropertyCommon.UpdateCollectionListener(_history, currentValue, value);
            _setValue(value);
            _notificationTarget?.RaisePropertyChangedFromHistory(_notificationPropertyName!);
        }

        private readonly History _history;
        private readonly PropertyChangeKey? _key;
        private Action<T> _setValue;
        private readonly T _oldValue;
        private T _newValue;
        private EditableModelBase? _notificationTarget;
        private string? _notificationPropertyName;
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

            if (_items.Length > maxCount)
                Resize(maxCount);
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

            Resize(newCapacity);
        }

        private void Resize(int newCapacity)
        {
            if (newCapacity is 0)
            {
                _items = Array.Empty<T>();
                _start = 0;
                return;
            }

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
}
