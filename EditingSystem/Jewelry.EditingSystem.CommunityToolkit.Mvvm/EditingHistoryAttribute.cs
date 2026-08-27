using System;

namespace Jewelry.EditingSystem.CommunityToolkit.Mvvm;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class EditingHistoryAttribute : Attribute
{
    public EditingHistoryAttribute(string memberName)
    {
        MemberName = memberName;
    }

    public string MemberName { get; }
}
