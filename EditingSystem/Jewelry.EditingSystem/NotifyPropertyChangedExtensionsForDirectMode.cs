using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Jewelry.EditingSystem;

public static class NotifyPropertyChangedExtensionsForDirectMode
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetEditableProperty<T>(
        this INotifyPropertyChanged self, 
        History history,
        Action<T> setValue, T currentValue, T nextValue, [CallerMemberName] string propertyName = "")
    {
        EditablePropertyCommon.SetEditableProperty(history, setValue, currentValue, nextValue, propertyName);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetEditableFlagProperty(
        this INotifyPropertyChanged self, 
        History history,
        Action<uint> setValue, uint currentValue, uint flag, bool value, [CallerMemberName] string propertyName = "")
    {
        EditablePropertyCommon.SetEditableFlagProperty(history, setValue, currentValue, flag, value, propertyName);
    }
}