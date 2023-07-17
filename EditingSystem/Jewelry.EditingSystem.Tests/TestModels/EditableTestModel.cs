using System.Collections.ObjectModel;

namespace Jewelry.EditingSystem.Tests.TestModels;

public sealed class EditableTestModel : EditableModelBase, ITestModel
{
    public EditableTestModel(History history) : base(history)
    {
    }

    public int ChangingCount { get; private set; }

    #region IntValue

    private int _IntValue;

    public int IntValue
    {
        get => _IntValue;
        set
        {
            if (SetEditableProperty(v => _IntValue = v, _IntValue, value))
                ++ChangingCount;
        }
    }

    #endregion

    #region StringValue

    private string _StringValue = "";

    public string StringValue
    {
        get => _StringValue;
        set
        {
            if (SetEditableProperty(v => _StringValue = v, _StringValue, value))
                ++ChangingCount;
        }
    }

    #endregion

    public bool IsA
    {
        get => (_flags & FlagIsA) != default;
        set
        {
            if (SetEditableFlagProperty(v => _flags = v, _flags, FlagIsA, value))
                ++ChangingCount;
        }
    }

    public bool IsB
    {
        get => (_flags & FlagIsB) != default;
        set
        {
            if (SetEditableFlagProperty(v => _flags = v, _flags, FlagIsB, value))
                ++ChangingCount;
        }
    }

    public bool IsC
    {
        get => (_flags & FlagIsC) != default;
        set
        {
            if (SetEditableFlagProperty(v => _flags = v, _flags, FlagIsC, value))
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
        set => SetEditableProperty(v => _IntCollection = v, _IntCollection, value);
    }

    #endregion

    #region Collection

    private ObservableCollection<CollectionItem> _Collection = new();

    public ObservableCollection<CollectionItem> Collection
    {
        get => _Collection;
        set => SetEditableProperty(v => _Collection = v, _Collection, value);
    }

    #endregion
}