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
        protected History _historyUnit;

        public void SetupEditingSystem(History history)
        {
            _historyUnit = history;
        }

        protected bool SetEditableProperty<T>(Action<T> setValue, T currentValue, T nextValue,
            [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(currentValue, nextValue))
                return false;

            var oldValue = currentValue;

            setValue(nextValue);

            _historyUnit?.Push(() => setValue(oldValue), () => setValue(nextValue));

            // INotifyCollectionChanged
            {
                if (currentValue is INotifyCollectionChanged cc)
                    UnregisterCollection(cc);

                if (nextValue is INotifyCollectionChanged nc)
                    RegisterCollection(nc);
            }

            RaisePropertyChanged(propertyName);

            return true;
        }

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

        protected void RegisterCollection(INotifyCollectionChanged collection)
        {
            if (collection == null)
                return;

            collection.CollectionChanged += CollectionOnCollectionChanged;
        }

        protected void UnregisterCollection(INotifyCollectionChanged collection)
        {
            if (collection == null)
                return;

            collection.CollectionChanged -= CollectionOnCollectionChanged;
        }

        private void CollectionOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_historyUnit.IsInUndoing)
                return;

            var list = (IList) sender;

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                {
                    var addItems = e.NewItems;
                    var addCount = addItems.Count;
                    var addIndex = e.NewStartingIndex;

                    void Redo()
                    {
                        for (var i = 0; i != addCount; ++i)
                            list.Insert(addIndex + i, addItems[i]);
                    }

                    void Undo()
                    {
                        for (var i = 0; i != addCount; ++i)
                            list.RemoveAt(addIndex + i);
                    }

                    _historyUnit.Push(Undo, Redo);
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
                        var src = e.OldStartingIndex;
                        var dst = e.NewStartingIndex;

                        var item = list[src];
                        list.RemoveAt(src);

                        list.Insert(dst, item);
                    }

                    void Undo()
                    {
                        var src = e.NewStartingIndex;
                        var dst = e.OldStartingIndex;

                        var item = list[src];
                        list.RemoveAt(src);

                        list.Insert(dst, item);
                    }

                    _historyUnit.Push(Undo, Redo);
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
                        item = list[e.OldStartingIndex];
                        list.RemoveAt(e.OldStartingIndex);
                    }

                    void Undo()
                    {
                        list.Insert(e.OldStartingIndex, item);
                    }

                    _historyUnit.Push(Undo, Redo);
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

                    var index = e.OldStartingIndex;

                    void Redo()
                    {
                        list[index] = e.NewItems[0];
                    }

                    void Undo()
                    {
                        list[index] = e.OldItems[0];
                    }

                    _historyUnit.Push(Undo, Redo);
                    break;
                }

                case NotifyCollectionChangedAction.Reset:
                    throw new NotSupportedException("Clear() is not support. Use ClearEx()");

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #endregion
    }
}