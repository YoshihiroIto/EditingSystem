using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Jewelry.EditingSystem;

internal static class EditablePropertyCommon
{
    internal static bool SetEditableProperty<T>(History history, Action<T> setValue, T oldValue, T newValue)
    {
        return SetEditableProperty(history, null, null, setValue, oldValue, newValue);
    }

    internal static bool SetEditableProperty<T>(
        History history,
        object? target,
        object? propertyKey,
        Action<T> setValue,
        T oldValue,
        T newValue,
        EditableModelBase? notificationTarget = null,
        string? notificationPropertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(oldValue, newValue))
            return false;

        setValue(newValue);
        notificationTarget?.RaisePropertyChangedFromHistory(notificationPropertyName!);
        RecordAppliedPropertyChange(
            history,
            target,
            propertyKey,
            setValue,
            oldValue,
            newValue,
            notificationTarget,
            notificationPropertyName);
        return true;
    }

    internal static bool RecordPropertyChange<T>(History history, Action<T> setValue, T oldValue, T newValue)
    {
        return RecordPropertyChange(history, null, null, setValue, oldValue, newValue);
    }

    internal static bool RecordPropertyChange<T>(
        History history,
        object? target,
        object? propertyKey,
        Action<T> setValue,
        T oldValue,
        T newValue)
    {
        if (EqualityComparer<T>.Default.Equals(oldValue, newValue))
            return false;

        if (history.IsInUndoing)
            return true;

        RecordAppliedPropertyChange(history, target, propertyKey, setValue, oldValue, newValue);
        return true;
    }

    internal static void RecordAppliedPropertyChange<T>(History history, Action<T> setValue, T oldValue, T newValue)
    {
        RecordAppliedPropertyChange(history, null, null, setValue, oldValue, newValue);
    }

    internal static void RecordAppliedPropertyChange<T>(
        History history,
        object? target,
        object? propertyKey,
        Action<T> setValue,
        T oldValue,
        T newValue,
        EditableModelBase? notificationTarget = null,
        string? notificationPropertyName = null)
    {
        if (history.IsInUndoing)
            return;

        history.PushPropertyChange(
            target,
            propertyKey,
            setValue,
            oldValue,
            newValue,
            notificationTarget,
            notificationPropertyName);

        UpdateCollectionListener(history, oldValue, newValue);
    }

    internal static void UpdateCollectionListener<T>(History history, T oldValue, T newValue)
    {
        if (oldValue is INotifyCollectionChanged oldNotifyCollectionChanged)
            history.CollectionChangedWeakEventManager.RemoveWeakEventListener(oldNotifyCollectionChanged);

        if (newValue is INotifyCollectionChanged newNotifyCollectionChanged)
            history.CollectionChangedWeakEventManager.AddWeakEventListener(newNotifyCollectionChanged, history.OnCollectionPropertyCollectionChanged);
    }
}