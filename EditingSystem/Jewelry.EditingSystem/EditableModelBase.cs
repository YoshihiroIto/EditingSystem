using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Jewelry.EditingSystem;

public class EditableModelBase : INotifyPropertyChanged
{
    protected EditableModelBase(History history)
    {
        _history = history;
    }

    protected void SetEditableProperty<T>(Action<T> setValue, T currentValue, T nextValue, [CallerMemberName] string propertyName = "")
    {
        void SetValueWithRaisePropertyChanged(T v)
        {
            setValue(v);
            RaisePropertyChanged(propertyName);
        }
        
        EditablePropertyCommon.SetEditableProperty(_history, SetValueWithRaisePropertyChanged, currentValue, nextValue);
    }

    protected void SetEditableFlagProperty(Action<uint> setValue, uint currentValue, uint flag, bool value, [CallerMemberName] string propertyName = "")
    {
        void SetValueWithRaisePropertyChanged(uint v)
        {
            setValue(v);
            RaisePropertyChanged(propertyName);
        }
        
        EditablePropertyCommon.SetEditableFlagProperty(_history, SetValueWithRaisePropertyChanged, currentValue, flag, value);
    }
    
    protected void SetEditableFlagProperty(Action<ulong> setValue, ulong currentValue, ulong flag, bool value, [CallerMemberName] string propertyName = "")
    {
        void SetValueWithRaisePropertyChanged(ulong v)
        {
            setValue(v);
            RaisePropertyChanged(propertyName);
        }
        
        EditablePropertyCommon.SetEditableFlagProperty(_history, SetValueWithRaisePropertyChanged, currentValue, flag, value);
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

    protected void RaisePropertyChanged([CallerMemberName] string propertyName = "")
    {
        if (PropertyChanged is null)
            return;

        var pc = PropChanged.GetOrAdd(propertyName, name => new PropertyChangedEventArgs(name));

        PropertyChanged.Invoke(this, pc);
    }

    private static readonly ConcurrentDictionary<string, PropertyChangedEventArgs> PropChanged = new();

    #endregion
    
    private readonly History _history;
}