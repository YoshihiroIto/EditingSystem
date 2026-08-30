using System.Collections.Generic;
using Jewelry.EditingSystem.Tests.TestModels;
using Xunit;

namespace Jewelry.EditingSystem.Tests.Tests;

public sealed class UndoableAttributeInheritedNotificationTests
{
    [Fact]
    public void AccessibleProtectedNotificationMethodOnBaseClassIsUsed()
    {
        using var history = new History();
        var model = new UndoableAttributeInheritedNotificationTestModel(history);
        var notifications = new List<string?>();

        model.PropertyChanged += (_, e) => notifications.Add(e.PropertyName);

        model.Value = 10;
        history.Undo();
        history.Redo();

        Assert.Equal([nameof(model.Value), nameof(model.Value), nameof(model.Value)], notifications);
    }
}
