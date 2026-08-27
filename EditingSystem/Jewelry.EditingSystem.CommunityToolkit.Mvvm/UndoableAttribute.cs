using System;

namespace Jewelry.EditingSystem.CommunityToolkit.Mvvm;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class UndoableAttribute : Attribute
{
}
