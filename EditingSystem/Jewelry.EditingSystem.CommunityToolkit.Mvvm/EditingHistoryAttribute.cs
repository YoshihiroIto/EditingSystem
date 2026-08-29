namespace Jewelry.EditingSystem.CommunityToolkit.Mvvm;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class EditingHistoryAttribute(string memberName) : Attribute
{
    public string MemberName { get; } = memberName;
}
