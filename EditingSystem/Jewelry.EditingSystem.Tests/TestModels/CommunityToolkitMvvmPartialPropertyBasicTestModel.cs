using CommunityToolkit.Mvvm.ComponentModel;
using Jewelry.EditingSystem.CommunityToolkit.Mvvm;
using System.Collections.ObjectModel;

using Jewelry.Collections;

namespace Jewelry.EditingSystem.Tests.TestModels;

[EditingHistory(nameof(_history))]
public sealed partial class CommunityToolkitMvvmPartialPropertyBasicTestModel : ObservableObject, IBasicTestModel
{
    private readonly History _history;

    public CommunityToolkitMvvmPartialPropertyBasicTestModel(History history)
    {
        _history = history;
    }

    public int ChangingCount { get; private set; }

    [Undoable, ObservableProperty]
    public partial int IntValue { get; set; }

    [Undoable, ObservableProperty]
    public partial string StringValue { get; set; } = "";

    [Undoable, ObservableProperty]
    public partial ObservableCollection<int> IntCollection { get; set; } = new();

    [Undoable, ObservableProperty]
    public partial ObservableCollection<CollectionItem> Collection { get; set; } = new();

    [Undoable, ObservableProperty]
    public partial ObservableHashSet<int> IntSet { get; set; } = new();

    [Undoable, ObservableProperty]
    public partial ObservableDictionary<string, int> IntDictionary { get; set; } = new();

    partial void OnIntValueChanged(int oldValue, int newValue) => ++ChangingCount;
    partial void OnStringValueChanged(string oldValue, string newValue) => ++ChangingCount;
    partial void OnIntCollectionChanged(ObservableCollection<int> oldValue, ObservableCollection<int> newValue) => ++ChangingCount;
    partial void OnCollectionChanged(ObservableCollection<CollectionItem> oldValue, ObservableCollection<CollectionItem> newValue) => ++ChangingCount;
}
