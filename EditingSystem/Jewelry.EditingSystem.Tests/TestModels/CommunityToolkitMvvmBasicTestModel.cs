using CommunityToolkit.Mvvm.ComponentModel;
using Jewelry.EditingSystem.CommunityToolkit.Mvvm;
using System.Collections.ObjectModel;

namespace Jewelry.EditingSystem.Tests.TestModels;

[EditingHistory(nameof(_history))]
public sealed partial class CommunityToolkitMvvmBasicTestModel : ObservableObject, IBasicTestModel
{
    private readonly History _history;

    public CommunityToolkitMvvmBasicTestModel(History history)
    {
        _history = history;
    }

    public int ChangingCount { get; private set; }

    [Undoable]
    [ObservableProperty]
    private int _intValue;

    [Undoable]
    [ObservableProperty]
    private string _stringValue = "";

    [Undoable]
    [ObservableProperty]
    private ObservableCollection<int> _intCollection = new();

    [Undoable]
    [ObservableProperty]
    private ObservableCollection<CollectionItem> _collection = new();

    partial void OnIntValueChanged(int value) => ++ChangingCount;
    partial void OnStringValueChanged(string value) => ++ChangingCount;
    partial void OnIntCollectionChanged(ObservableCollection<int> value) => ++ChangingCount;
    partial void OnCollectionChanged(ObservableCollection<CollectionItem> value) => ++ChangingCount;

    partial void OnIntValueChanging(int oldValue, int newValue) => LastChangingValues = (oldValue, newValue);

    public (int OldValue, int NewValue) LastChangingValues { get; private set; }
}
