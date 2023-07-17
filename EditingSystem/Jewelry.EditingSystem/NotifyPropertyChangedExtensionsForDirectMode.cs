using System;
using System.ComponentModel;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Jewelry.EditingSystem;

public static class NotifyPropertyChangedExtensionsForDirectMode
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool SetEditableProperty<T>(
        this INotifyPropertyChanged _, 
        History history,
        Action<T> setValue, T oldValue, T newValue)
    {
        return EditablePropertyCommon.SetEditableProperty(history, setValue, oldValue, newValue);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool SetEditableFlagProperty<T>(
        this INotifyPropertyChanged _, 
        History history,
        Action<T> setValue, T oldFlags, T newFlags, bool value)
        where T : IBitwiseOperators<T, T, T>, IEqualityOperators<T, T, bool>, IUnsignedNumber<T>
    {
        return EditablePropertyCommon.SetEditableFlagProperty(history, setValue, oldFlags, newFlags, value);
    }
}