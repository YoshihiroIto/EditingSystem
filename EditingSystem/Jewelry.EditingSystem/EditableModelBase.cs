using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Jewelry.EditingSystem;

public class EditableModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected EditableModelBase(History history)
    {
        _history = history;
    }

    protected bool SetEditableProperty<T>(Action<T> setValue, T oldValue, T newValue, [CallerMemberName] string propertyName = "")
    {
        return EditablePropertyCommon.SetEditableProperty(
            _history,
            this,
            propertyName,
            setValue,
            oldValue,
            newValue,
            this,
            propertyName);
    }
    
    protected bool SetPropertyWithoutHistory<T>(ref T storage, T value, [CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
            return false;

        storage = value;

        RaisePropertyChanged(propertyName);

        return true;
    }

    protected void RaisePropertyChanged([CallerMemberName] string propertyName = "")
    {
        RaisePropertyChangedFromHistory(propertyName);
    }

    internal void RaisePropertyChangedFromHistory(string propertyName)
    {
        if (PropertyChanged is null)
            return;

        var pc = PropChanged.GetOrAdd(propertyName, static name => new PropertyChangedEventArgs(name));

        PropertyChanged.Invoke(this, pc);
    }

    private readonly History _history;
    private static readonly ConcurrentDictionary<string, PropertyChangedEventArgs> PropChanged = new();
}
