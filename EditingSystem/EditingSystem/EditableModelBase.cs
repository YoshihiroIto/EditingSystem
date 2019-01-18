using System;
using System.Collections;
using System.Collections.Generic;
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

        protected bool SetEditableProperty<T>(Action<T> setValue, T currentValue, T nextValue, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(currentValue, nextValue))
                return false;

            var oldValue = currentValue;

            setValue(nextValue);
            _historyUnit?.Push(() => setValue(oldValue), () => setValue(nextValue));

            RaisePropertyChanged(propertyName);

            return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void RaisePropertyChanged([CallerMemberName] string propertyName = "")
        {
            // ReSharper disable once InconsistentlySynchronizedField
            var pc = (PropertyChangedEventArgs)_propChanged[propertyName];

            if (pc == null)
            {
                // double-checked;
                lock (_propChanged)
                {
                    pc = (PropertyChangedEventArgs)_propChanged[propertyName];

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
    }
}