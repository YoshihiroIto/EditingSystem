using System;

namespace Jewelry.EditingSystem.Annotations;

/// <summary>
/// Marks a partial property whose changes should be recorded in the configured <see cref="Jewelry.EditingSystem.History"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class UndoableAttribute : Attribute;
