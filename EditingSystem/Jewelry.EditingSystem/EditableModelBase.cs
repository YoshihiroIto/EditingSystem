using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Jewelry.EditingSystem;

public class EditableModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    
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

    protected void SetEditableFlagProperty<T>(Action<T> setValue, T currentValue, T flag, bool value, [CallerMemberName] string propertyName = "")
        where T : IBitwiseOperators<T, T, T>, IEqualityOperators<T, T, bool>, IUnsignedNumber<T>
    {
        void SetValueWithRaisePropertyChanged(T v)
        {
            setValue(v);
            RaisePropertyChanged(propertyName);
        }
        
        EditablePropertyCommon.SetEditableFlagProperty(_history, SetValueWithRaisePropertyChanged, currentValue, flag, value);
    }
    
    protected bool SetPropertyWithoutHistory<T>(ref T storage, T value, [CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
            return false;

        storage = value;

        RaisePropertyChanged(propertyName);

        return true;
    }

    protected bool SetFlagPropertyWithoutHistory<T>(ref T storage, T flag, bool value, [CallerMemberName] string propertyName = "")
        where T : struct, IBitwiseOperators<T, T, T>, IEqualityOperators<T, T, bool>, IUnsignedNumber<T>
    {
        if (value)
        {
            if ((storage & flag) != default)
                return false;

            storage |= flag;
        }
        else
        {
            if ((storage & flag) == default)
                return false;

            storage &= ~flag;
        }

        RaisePropertyChanged(propertyName);

        return true;
    }

    protected void RaisePropertyChanged([CallerMemberName] string propertyName = "")
    {
        if (PropertyChanged is null)
            return;

        var pc = PropChanged.GetOrAdd(propertyName, name => new PropertyChangedEventArgs(name));

        PropertyChanged.Invoke(this, pc);
    }

    private readonly History _history;
    private static readonly ConcurrentDictionary<string, PropertyChangedEventArgs> PropChanged = new();
}