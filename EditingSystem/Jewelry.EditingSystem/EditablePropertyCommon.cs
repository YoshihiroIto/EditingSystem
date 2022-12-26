using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Numerics;

namespace Jewelry.EditingSystem;

internal static class EditablePropertyCommon
{
    internal static void SetEditableProperty<T>(History history, Action<T> setValue, T currentValue, T nextValue)
    {
        if (EqualityComparer<T>.Default.Equals(currentValue, nextValue))
            return;

        var oldValue = currentValue;

        history.Push(() => setValue(oldValue), () => setValue(nextValue));

        // INotifyCollectionChanged
        {
            if (currentValue is INotifyCollectionChanged current)
                current.CollectionChanged -= history.OnCollectionPropertyCollectionChanged;

            if (nextValue is INotifyCollectionChanged next)
                next.CollectionChanged += history.OnCollectionPropertyCollectionChanged;
        }

        setValue(nextValue);
    }

    internal static void SetEditableFlagProperty<T>(History history, Action<T> setValue, T currentValue, T flag, bool value)
        where T : IBitwiseOperators<T, T, T>, IEqualityOperators<T, T, bool>, IUnsignedNumber<T>
    {
        var oldValue = currentValue;
        var nextValue = currentValue;

        if (value)
        {
            if ((currentValue & flag) != default)
                return;

            nextValue |= flag;
        }
        else
        {
            if ((currentValue & flag) == default)
                return;

            nextValue &= ~flag;
        }

        history.Push(() => setValue(oldValue), () => setValue(nextValue));

        setValue(nextValue);
    }
}