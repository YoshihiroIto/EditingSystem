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

    [History] [ObservableProperty] private int _IntValue;

    [History] [ObservableProperty] private string _StringValue = "";

    [History] [ObservableProperty] private ObservableCollection<int> _IntCollection = new();

    [History] [ObservableProperty] private ObservableCollection<CollectionItem> _Collection = new();

    partial void OnIntValueChanged(int value) => ++ChangingCount;
    partial void OnStringValueChanged(string value) => ++ChangingCount;
    partial void OnIntCollectionChanged(ObservableCollection<int> value) => ++ChangingCount;
    partial void OnCollectionChanged(ObservableCollection<CollectionItem> value) => ++ChangingCount;
}