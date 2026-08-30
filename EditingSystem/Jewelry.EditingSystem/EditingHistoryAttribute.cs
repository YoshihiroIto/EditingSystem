using System;

namespace Jewelry.EditingSystem.Annotations;

/// <summary>
/// Selects the <see cref="Jewelry.EditingSystem.History"/> field, property, or primary-constructor parameter used by
/// <see cref="UndoableAttribute"/> properties declared on this type.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class EditingHistoryAttribute(string memberName) : Attribute
{
    public string MemberName { get; } = memberName;
}
