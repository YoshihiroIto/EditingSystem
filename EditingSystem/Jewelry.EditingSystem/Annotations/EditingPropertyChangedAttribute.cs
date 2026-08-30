using System;

namespace Jewelry.EditingSystem.Annotations;

/// <summary>
/// Selects the method used by generated <see cref="UndoableAttribute"/> properties to raise
/// <see cref="System.ComponentModel.INotifyPropertyChanged.PropertyChanged"/> notifications.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class EditingPropertyChangedAttribute(string methodName) : Attribute
{
    public string MethodName { get; } = methodName;
}
