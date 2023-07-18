using CommunityToolkit.Mvvm.ComponentModel;
using Jewelry.EditingSystem.CommunityToolkit.Mvvm;
using System.Collections.ObjectModel;

namespace Jewelry.EditingSystem.Tests.TestModels;

public sealed partial class CommunityToolkitMvvmTestModel : ObservableObject, ITestModel
{
    private readonly History _history;

    public CommunityToolkitMvvmTestModel(History history)
    {
        _history = history;
    }

    public int ChangingCount { get; private set; }

    [History] [ObservableProperty] private int _IntValue;
              
    [History] [ObservableProperty] private string _StringValue = "";

    partial void OnIntValueChanged(int value) => ++ChangingCount;
    partial void OnStringValueChanged(string value) => ++ChangingCount;

    public bool IsA
    {
        get => (_flags & FlagIsA) != default;
        set
        {
            if (this.SetEditableFlagProperty(_history, v => _flags = v, _flags, FlagIsA, value))
                ++ChangingCount;
        }
    }

    public bool IsB
    {
        get => (_flags & FlagIsB) != default;
        set
        {
            if (this.SetEditableFlagProperty(_history, v => _flags = v, _flags, FlagIsB, value))
                ++ChangingCount;
        }
    }

    public bool IsC
    {
        get => (_flags & FlagIsC) != default;
        set
        {
            if (this.SetEditableFlagProperty(_history, v => _flags = v, _flags, FlagIsC, value))
                ++ChangingCount;
        }
    }

    private byte _flags;
    private const byte FlagIsA = 1 << 0;
    private const byte FlagIsB = 1 << 1;
    private const byte FlagIsC = 1 << 2;


    #region IntCollection

    private ObservableCollection<int> _IntCollection = new();

    public ObservableCollection<int> IntCollection
    {
        get => _IntCollection;
        set => this.SetEditableProperty(_history, v => SetProperty(ref _IntCollection, v), _IntCollection, value);
    }

    #endregion

    #region Collection

    private ObservableCollection<CollectionItem> _Collection = new();

    public ObservableCollection<CollectionItem> Collection
    {
        get => _Collection;
        set => this.SetEditableProperty(_history, v => SetProperty(ref _Collection, v), _Collection, value);
    }

    #endregion
}