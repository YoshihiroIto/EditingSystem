using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Numerics;

namespace Jewelry.EditingSystem;

internal static class EditablePropertyCommon
{
    internal static bool SetEditableProperty<T>(History history, Action<T> setValue, T oldValue, T newValue)
    {
        if (EqualityComparer<T>.Default.Equals(oldValue, newValue))
            return false;


        history.Push(() => setValue(oldValue), () => setValue(newValue));

        if (oldValue is INotifyCollectionChanged oldNotifyCollectionChanged)
            history.CollectionChangedWeakEventManager.RemoveWeakEventListener(oldNotifyCollectionChanged);
        
        if (newValue is INotifyCollectionChanged newNotifyCollectionChanged)
            history.CollectionChangedWeakEventManager.AddWeakEventListener(newNotifyCollectionChanged, history.OnCollectionPropertyCollectionChanged);

        setValue(newValue);
        return true;
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