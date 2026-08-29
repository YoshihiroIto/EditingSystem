using CommunityToolkit.Mvvm.ComponentModel;
using Jewelry.EditingSystem.CommunityToolkit.Mvvm;
using System.Collections.ObjectModel;

using Jewelry.Collections;

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

    [Undoable]
    [ObservableProperty]
    private ObservableHashSet<int> _intSet = new();

    [Undoable]
    [ObservableProperty]
    private ObservableDictionary<string, int> _intDictionary = new();

    partial void OnIntValueChanged(int oldValue, int newValue) => ++ChangingCount;
    partial void OnStringValueChanged(string? oldValue, string newValue) => ++ChangingCount;
    partial void OnIntCollectionChanged(ObservableCollection<int>? oldValue, ObservableCollection<int> newValue) => ++ChangingCount;
    partial void OnCollectionChanged(ObservableCollection<CollectionItem>? oldValue, ObservableCollection<CollectionItem> newValue) => ++ChangingCount;

    partial void OnIntValueChanging(int oldValue, int newValue) => LastChangingValues = (oldValue, newValue);

    public (int OldValue, int NewValue) LastChangingValues { get; private set; }
}
