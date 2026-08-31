using System.ComponentModel;
using Jewelry.EditingSystem.Annotations;
using Xunit;

namespace Jewelry.EditingSystem.Tests.Tests;

public sealed class EditingPropertyChangedAttributeTests
{
    [Fact]
    public void Notify_property_changed_method_is_used_automatically_for_set_undo_and_redo()
    {
        using var history = new History();
        var model = new AutomaticNotificationModel(history);
        var eventCount = 0;
        model.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(AutomaticNotificationModel.Value))
                ++eventCount;
        };

        model.Value = 42;
        history.Undo();
        history.Redo();

        Assert.Equal(42, model.Value);
        Assert.Equal(3, model.NotificationCount);
        Assert.Equal(3, eventCount);
    }

    [Fact]
    public void Explicit_notification_method_is_used_for_set_undo_and_redo()
    {
        using var history = new History();
        var model = new ExplicitNotificationModel(history);
        var eventCount = 0;
        model.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ExplicitNotificationModel.Value))
                ++eventCount;
        };

        model.Value = 42;
        history.Undo();
        history.Redo();

        Assert.Equal(42, model.Value);
        Assert.Equal(3, model.NotificationCount);
        Assert.Equal(3, eventCount);
    }
}

[EditingHistory(nameof(history))]
internal sealed partial class AutomaticNotificationModel(History history) : INotifyPropertyChanged
{
    [Undoable]
    public partial int Value { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public int NotificationCount { get; private set; }

    private void NotifyPropertyChanged(string propertyName)
    {
        ++NotificationCount;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

[EditingHistory(nameof(history))]
[EditingPropertyChanged(nameof(NotifyPropertyChanged))]
internal sealed partial class ExplicitNotificationModel(History history) : INotifyPropertyChanged
{
    [Undoable]
    public partial int Value { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public int NotificationCount { get; private set; }

    private void NotifyPropertyChanged(string propertyName)
    {
        ++NotificationCount;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
