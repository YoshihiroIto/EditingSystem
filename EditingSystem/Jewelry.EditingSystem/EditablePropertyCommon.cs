using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;

namespace Jewelry.EditingSystem;

internal static class EditablePropertyCommon
{
    internal static void SetEditableProperty<T>(
        History history,
        Action<T> setValue, T currentValue, T nextValue, string propertyName)
    {
        if (EqualityComparer<T>.Default.Equals(currentValue, nextValue))
            return;

        var oldValue = currentValue;

        PushPropertyHistory(history, setValue, propertyName, oldValue, nextValue);

        // INotifyCollectionChanged
        {
            if (currentValue is INotifyCollectionChanged current)
                current.CollectionChanged -= history.OnCollectionPropertyCollectionChanged;

            if (nextValue is INotifyCollectionChanged next)
                next.CollectionChanged += history.OnCollectionPropertyCollectionChanged;
        }

        setValue(nextValue);
    }
    
    internal static void SetEditableFlagProperty(
        History history,
        Action<uint> setValue, uint currentValue, uint flag, bool value, [CallerMemberName] string propertyName = "")
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

        PushPropertyHistory(history, setValue, propertyName, oldValue, nextValue);

        setValue(nextValue);
    }

    private static void PushPropertyHistory<T>(History history, Action<T> setValue, string propertyName, T oldValue, T nextValue)
    {
        history.Push(
            () => setValue(oldValue),
            () => setValue(nextValue));
    }
}