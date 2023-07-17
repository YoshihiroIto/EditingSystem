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
        Action<T> setValue, T currentValue, T nextValue)
    {
        return EditablePropertyCommon.SetEditableProperty(history, setValue, currentValue, nextValue);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool SetEditableFlagProperty<T>(
        this INotifyPropertyChanged _, 
        History history,
        Action<T> setValue, T currentValue, T flag, bool value)
        where T : IBitwiseOperators<T, T, T>, IEqualityOperators<T, T, bool>, IUnsignedNumber<T>
    {
        return EditablePropertyCommon.SetEditableFlagProperty(history, setValue, currentValue, flag, value);
    }
}