using CommunityToolkit.Mvvm.ComponentModel;
using Jewelry.EditingSystem.CommunityToolkit.Mvvm;
using System.Collections.ObjectModel;

namespace Jewelry.EditingSystem.Tests.TestModels;

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
    private int _IntValue;

    [Undoable]
    [ObservableProperty]
    private string _StringValue = "";

    [Undoable]
    [ObservableProperty]
    private ObservableCollection<int> _IntCollection = new();

    [Undoable]
    [ObservableProperty]
    private ObservableCollection<CollectionItem> _Collection = new();

    partial void OnIntValueChanged(int value) => ++ChangingCount;
    partial void OnStringValueChanged(string value) => ++ChangingCount;
    partial void OnIntCollectionChanged(ObservableCollection<int> value) => ++ChangingCount;
    partial void OnCollectionChanged(ObservableCollection<CollectionItem> value) => ++ChangingCount;

    partial void OnCollectionChanging(ObservableCollection<CollectionItem>? oldValue, ObservableCollection<CollectionItem> newValue)
        => ES_OnCollectionChanging(oldValue, newValue);

    partial void OnIntCollectionChanging(ObservableCollection<int>? oldValue, ObservableCollection<int> newValue)
        => ES_OnIntCollectionChanging(oldValue, newValue);

    partial void OnIntValueChanging(int oldValue, int newValue)
        => ES_OnIntValueChanging(oldValue, newValue);

    partial void OnStringValueChanging(string? oldValue, string newValue)
        => ES_OnStringValueChanging(oldValue, newValue);
}