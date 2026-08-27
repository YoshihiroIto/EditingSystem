using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Numerics;

namespace Jewelry.EditingSystem;

internal static class EditablePropertyCommon
{
    internal static bool SetEditableProperty<T>(History history, Action<T> setValue, T oldValue, T newValue)
    {
        if (RecordPropertyChange(history, setValue, oldValue, newValue) is false)
            return false;

        setValue(newValue);
        return true;
    }

    internal static bool RecordPropertyChange<T>(History history, Action<T> setValue, T oldValue, T newValue)
    {
        if (EqualityComparer<T>.Default.Equals(oldValue, newValue))
            return false;

        void ApplyValue(T currentValue, T value)
        {
            UpdateCollectionListener(history, currentValue, value);
            setValue(value);
        }

        history.Push(
            () => ApplyValue(newValue, oldValue),
            () => ApplyValue(oldValue, newValue));

        UpdateCollectionListener(history, oldValue, newValue);
        return true;
    }

    private static void UpdateCollectionListener<T>(History history, T oldValue, T newValue)
    {
        if (oldValue is INotifyCollectionChanged oldNotifyCollectionChanged)
            history.CollectionChangedWeakEventManager.RemoveWeakEventListener(oldNotifyCollectionChanged);

        if (newValue is INotifyCollectionChanged newNotifyCollectionChanged)
            history.CollectionChangedWeakEventManager.AddWeakEventListener(
                newNotifyCollectionChanged,
                history.OnCollectionPropertyCollectionChanged);
    }

#if NET8_0_OR_GREATER 
    internal static bool SetEditableFlagProperty<T>(History history, Action<T> setValue, T oldFlags, T newFlags, bool value)
        where T : IBitwiseOperators<T, T, T>, IEqualityOperators<T, T, bool>, IUnsignedNumber<T>
    {
        var newValue = oldFlags;

        if (value)
        {
            if ((oldFlags & newFlags) != default)
                return false;

            newValue |= newFlags;
        }
        else
        {
            if ((oldFlags & newFlags) == default)
                return false;

            newValue &= ~newFlags;
        }

        history.Push(() => setValue(oldFlags), () => setValue(newValue));

        setValue(newValue);
        return true;
    }
#endif
}
