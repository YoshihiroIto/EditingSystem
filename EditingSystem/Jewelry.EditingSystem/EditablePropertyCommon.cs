using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Numerics;

namespace Jewelry.EditingSystem;

internal static class EditablePropertyCommon
{
    internal static bool SetEditableProperty<T>(History history, Action<T> setValue, T currentValue, T nextValue)
    {
        if (EqualityComparer<T>.Default.Equals(currentValue, nextValue))
            return false;

        var oldValue = currentValue;

        history.Push(() => setValue(oldValue), () => setValue(nextValue));

        if (currentValue is INotifyCollectionChanged current)
            history._collectionChangedWeakEventManager.RemoveWeakEventListener(current);
        
        if (nextValue is INotifyCollectionChanged next)
            history._collectionChangedWeakEventManager.AddWeakEventListener(next, history.OnCollectionPropertyCollectionChanged);

        setValue(nextValue);
        return true;
    }

    internal static bool SetEditableFlagProperty<T>(History history, Action<T> setValue, T currentValue, T flag, bool value)
        where T : IBitwiseOperators<T, T, T>, IEqualityOperators<T, T, bool>, IUnsignedNumber<T>
    {
        var oldValue = currentValue;
        var nextValue = currentValue;

        if (value)
        {
            if ((currentValue & flag) != default)
                return false;

            nextValue |= flag;
        }
        else
        {
            if ((currentValue & flag) == default)
                return false;

            nextValue &= ~flag;
        }

        history.Push(() => setValue(oldValue), () => setValue(nextValue));

        setValue(nextValue);
        return true;
    }
}