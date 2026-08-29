using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Jewelry.EditingSystem;

public static class NotifyPropertyChangedExtensionsForDirectMode
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool SetEditableProperty<T>(
        this INotifyPropertyChanged target,
        History history,
        Action<T> setValue,
        T oldValue,
        T newValue,
        [CallerMemberName] string propertyName = "")
    {
        return EditablePropertyCommon.SetEditableProperty(
            history,
            target,
            propertyName,
            setValue,
            oldValue,
            newValue);
    }
}
