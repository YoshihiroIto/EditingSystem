using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EditingSystem
{
    public class EditableModelBase : INotifyPropertyChanged
    {
        protected History? History { get; private set; }

        public void SetupEditingSystem(History? history)
        {
            History = history;
        }

        protected void SetEditableProperty<T>(Action<T> setValue, T currentValue, T nextValue, [CallerMemberName] string propertyName = "")
        {
            if (History != null)
#nullable disable
                if (History.OnSetValue(this, currentValue, nextValue, propertyName) == OnSetValueResult.Cancel)
#nullable enable
                    return;

            if (EqualityComparer<T>.Default.Equals(currentValue, nextValue))
                return;

            var oldValue = currentValue;

            setValue(nextValue);

            History?.Push(() =>
            {
                setValue(oldValue);
                RaisePropertyChanged(propertyName);
            }, () =>
            {
                setValue(nextValue);
                RaisePropertyChanged(propertyName);
            });

            // INotifyCollectionChanged
            {
                if (currentValue is INotifyCollectionChanged current)
                    current.CollectionChanged -= CollectionOnCollectionChanged;

                if (nextValue is INotifyCollectionChanged next)
                    next.CollectionChanged += CollectionOnCollectionChanged;
            }

            RaisePropertyChanged(propertyName);
        }

        protected void SetEditableFlagProperty(Action<uint> setValue, uint currentValue, uint flag, bool value, [CallerMemberName] string propertyName = "")
        {
            var oldValue = currentValue;
            var nextValue = currentValue;

            if (value)
            {
                if ((currentValue & flag) != 0)
                    return;

                nextValue |= flag;
            }
            else
            {
                if ((currentValue & flag) == 0)
                    return;

                nextValue &= ~flag;
            }

            setValue(nextValue);

            History?.Push(() =>
            {
                setValue(oldValue);
                RaisePropertyChanged(propertyName);
            }, () =>
            {
                setValue(nextValue);
                RaisePropertyChanged(propertyName);
            });

            RaisePropertyChanged(propertyName);
        }

        #region Without History

        protected bool SetPropertyWithoutHistory<T>(ref T storage, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
                return false;

            storage = value;

            RaisePropertyChanged(propertyName);

            return true;
        }

        protected bool SetFlagPropertyWithoutHistory(ref uint storage, uint flag, bool value, [CallerMemberName] string propertyName = "")
        {
            if (value)
            {
                if ((storage & flag) != 0)
                    return false;

                storage |= flag;
            }
            else
            {
                if ((storage & flag) == 0)
                    return false;

                storage &= ~flag;
            }

            RaisePropertyChanged(propertyName);

            return true;
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void RaisePropertyChanged([CallerMemberName] string propertyName = "")
        {
            // ReSharper disable once InconsistentlySynchronizedField
            var pc = (PropertyChangedEventArgs) _propChanged[propertyName];

            if (pc == null)
            {
                // double-checked;
                lock (_propChanged)
                {
                    pc = (PropertyChangedEventArgs) _propChanged[propertyName];

                    if (pc == null)
                    {
                        pc = new PropertyChangedEventArgs(propertyName);
                        _propChanged[propertyName] = pc;
                    }
                }
            }

            PropertyChanged?.Invoke(this, pc);
        }

        // use Hashtable to get free lockless reading
        private static readonly Hashtable _propChanged = new Hashtable();

        #endregion

        #region INotifyCollectionChanged

        private void CollectionOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (History == null)
                return;

            if (History.IsInUndoing)
                return;

            CollectionChanged(sender, e);

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                {
                    void Redo()
                    {
                        var list = (IList) sender;

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

                    void Undo()
                    {
                        var list = (IList) sender;

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

                    History.Push(Undo, Redo);
                    break;
                }

                case NotifyCollectionChangedAction.Move:
                {
                    if (e.OldItems.Count != 1)
                        throw new NotImplementedException();

                    if (e.NewItems.Count != 1)
                        throw new NotImplementedException();

                    void Redo()
                    {
                        var list = (IList) sender;

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

                    void Undo()
                    {
                        var list = (IList) sender;

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

                    History.Push(Undo, Redo);
                    break;
                }

                case NotifyCollectionChangedAction.Remove:
                {
                    if (e.OldItems.Count != 1)
                        throw new NotImplementedException();

                    if (e.NewItems != null)
                        throw new NotImplementedException();

                    var item = e.OldItems[0];

                    void Redo()
                    {
                        var list = (IList) sender;

                        item = list[e.OldStartingIndex];
                        list.RemoveAt(e.OldStartingIndex);

                        // ICollectionItem
                        {
                            if (item is ICollectionItem collItem)
                                collItem.Changed(CollectionItemChangedInfo.Remove);
                        }
                    }

                    void Undo()
                    {
                        var list = (IList) sender;

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

                    History.Push(Undo, Redo);
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

                    void Redo()
                    {
                        var list = (IList) sender;

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

                    void Undo()
                    {
                        var list = (IList) sender;

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

                    History.Push(Undo, Redo);
                    break;
                }

                case NotifyCollectionChangedAction.Reset:
                {
                    if (History == null)
                        break;

                    if (History.IsInPaused)
                        break;

                    throw new NotSupportedException("Clear() is not support. Use ClearEx()");
                }

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #endregion

        protected virtual void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
        }
    }
}