using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Jewelry.EditingSystem.CommunityToolkit.Mvvm;
using Jewelry.EditingSystem.Tests.TestModels;
using Xunit;

namespace Jewelry.EditingSystem.Tests;

public sealed partial class CommunityToolkitMvvmIntegrationTests
{
    [Fact]
    public void GeneratedSetterPipelineRunsForNormalUndoAndRedo()
    {
        using var history = new History();
        var model = new CommunityToolkitFeaturesModel(history);
        var dependentPropertyChangedCount = 0;
        var canExecuteChangedCount = 0;

        model.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(model.DoubledValue))
                dependentPropertyChangedCount++;
        };
        model.ApplyCommand.CanExecuteChanged += (_, _) => canExecuteChangedCount++;

        model.Value = 10;
        history.Undo();
        history.Redo();

        Assert.Equal(10, model.Value);
        Assert.Equal(3, dependentPropertyChangedCount);
        Assert.Equal(3, canExecuteChangedCount);
        Assert.Equal(3, model.ChangingHookCount);
        Assert.Equal(3, model.ChangedHookCount);
        Assert.Equal((0, 10), model.LastChangingValues);
        Assert.Equal((0, 10), model.LastChangedValues);
    }

    [Fact]
    public void ValidationRunsForUndoAndRedo()
    {
        using var history = new History();
        var model = new CommunityToolkitValidationModel(history);

        model.Name = "valid";
        model.Name = null;
        Assert.True(model.HasErrors);

        history.Undo();
        Assert.Equal("valid", model.Name);
        Assert.False(model.HasErrors);

        history.Redo();
        Assert.Null(model.Name);
        Assert.True(model.HasErrors);
    }

    [Fact]
    public void RecipientBroadcastRunsForUndoAndRedo()
    {
        using var history = new History();
        var messenger = new StrongReferenceMessenger();
        var model = new CommunityToolkitRecipientModel(history, messenger);
        var messages = new List<(int OldValue, int NewValue)>();

        messenger.Register<CommunityToolkitMvvmIntegrationTests, PropertyChangedMessage<int>>(
            this,
            (_, message) => messages.Add((message.OldValue, message.NewValue)));

        model.Value = 5;
        history.Undo();
        history.Redo();

        Assert.Equal(new[] { (0, 5), (5, 0), (0, 5) }, messages);
    }

    [Theory]
    [InlineData(TestModelKinds.CommunityToolkitMvvm)]
    [InlineData(TestModelKinds.CommunityToolkitMvvmPartialProperty)]
    public void CollectionListenerFollowsPropertyUndoAndRedo(TestModelKinds kind)
    {
        using var history = new History();
        var model = TestModelCreator.CreateBasicTestModel(kind, history);
        var original = model.IntCollection;
        var replacement = new ObservableCollection<int>();

        model.IntCollection = replacement;
        history.Undo();
        history.Clear();

        replacement.Add(1);
        Assert.False(history.CanUndo);

        original.Add(1);
        Assert.True(history.CanUndo);

        history.Clear();
        model.IntCollection = replacement;
        history.Undo();
        history.Redo();
        history.Clear();

        original.Add(2);
        Assert.False(history.CanUndo);

        replacement.Add(2);
        Assert.True(history.CanUndo);
    }

    [Fact]
    public void EqualAssignmentDoesNotRecordHistoryOrRunHooks()
    {
        using var history = new History();
        var model = new CommunityToolkitFeaturesModel(history);

        model.Value = 0;

        Assert.False(history.CanUndo);
        Assert.Equal(0, model.ChangingHookCount);
        Assert.Equal(0, model.ChangedHookCount);
    }

    [Fact]
    public void ThrowingChangingHookDoesNotLeavePhantomHistory()
    {
        using var history = new History();
        var model = new CommunityToolkitThrowingChangingModel(history);

        Assert.Throws<System.InvalidOperationException>(() => model.Value = 1);

        Assert.Equal(0, model.Value);
        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);
    }

    [EditingHistory(nameof(history))]
    private sealed partial class CommunityToolkitFeaturesModel(History history) : ObservableObject
    {
        public int DoubledValue => Value * 2;
        public int ChangingHookCount { get; private set; }
        public int ChangedHookCount { get; private set; }
        public (int OldValue, int NewValue) LastChangingValues { get; private set; }
        public (int OldValue, int NewValue) LastChangedValues { get; private set; }

        [Undoable]
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DoubledValue))]
        [NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
        public partial int Value { get; set; }

        private bool CanApply() => Value > 0;

        [RelayCommand(CanExecute = nameof(CanApply))]
        private void Apply()
        {
        }

        partial void OnValueChanging(int oldValue, int newValue)
        {
            ChangingHookCount++;
            LastChangingValues = (oldValue, newValue);
        }

        partial void OnValueChanged(int oldValue, int newValue)
        {
            ChangedHookCount++;
            LastChangedValues = (oldValue, newValue);
        }
    }

    [EditingHistory(nameof(history))]
    public sealed partial class CommunityToolkitValidationModel(History history) : ObservableValidator
    {
        [Undoable]
        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required]
        public partial string? Name { get; set; }
    }

    [EditingHistory(nameof(history))]
    private sealed partial class CommunityToolkitThrowingChangingModel(History history) : ObservableObject
    {
        [Undoable]
        [ObservableProperty]
        public partial int Value { get; set; }

        partial void OnValueChanging(int oldValue, int newValue)
        {
            throw new System.InvalidOperationException();
        }
    }

    [EditingHistory(nameof(_history))]
    private sealed partial class CommunityToolkitRecipientModel : ObservableRecipient
    {
        private readonly History _history;

        public CommunityToolkitRecipientModel(History history, IMessenger messenger)
            : base(messenger)
        {
            _history = history;
            IsActive = true;
        }

        [Undoable]
        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial int Value { get; set; }
    }
}
