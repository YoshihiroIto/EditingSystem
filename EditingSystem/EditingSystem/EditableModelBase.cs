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
        protected History History { get; private set; }

        public void SetupEditingSystem(History history)
        {
            History = history;
        }

        protected bool SetEditableProperty<T>(Action<T> setValue, T currentValue, T nextValue, [CallerMemberName] string propertyName = "")
        {
            if (History != null)
                if (History.OnSetValue(this, currentValue, nextValue, propertyName) == OnSetValueResult.Cancel)
                    return false;

            if (EqualityComparer<T>.Default.Equals(currentValue, nextValue))
                return false;

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

            return true;
        }

        protected bool SetEditableFlagProperty(Action<uint> setValue, uint currentValue, uint flag, bool value, [CallerMemberName] string propertyName = "")
        {
            var oldValue = currentValue;
            var nextValue = currentValue;

            if (value)
            {
                if ((currentValue & flag) != 0)
                    return false;

                nextValue |= flag;
            }
            else
            {
                if ((currentValue & flag) == 0)
                    return false;

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

            return true;
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

        public event PropertyChangedEventHandler PropertyChanged;

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

                        for (var i = 0; i != addCount; ++i)
                            list.Insert(addIndex + i, addItems[i]);
                    }

                    void Undo()
                    {
                        var list = (IList) sender;

                        var addItems = e.NewItems;
                        var addCount = addItems.Count;
                        var addIndex = e.NewStartingIndex;

                        for (var i = 0; i != addCount; ++i)
                            list.RemoveAt(addIndex + i);
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
                    }

                    void Undo()
                    {
                        var list = (IList) sender;

                        var src = e.NewStartingIndex;
                        var dst = e.OldStartingIndex;

                        var item = list[src];
                        list.RemoveAt(src);

                        list.Insert(dst, item);
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
                    }

                    void Undo()
                    {
                        var list = (IList) sender;

                        list.Insert(e.OldStartingIndex, item);
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
                        list[index] = e.NewItems[0];
                    }

                    void Undo()
                    {
                        var list = (IList) sender;

                        var index = e.OldStartingIndex;
                        list[index] = e.OldItems[0];
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
    }
}