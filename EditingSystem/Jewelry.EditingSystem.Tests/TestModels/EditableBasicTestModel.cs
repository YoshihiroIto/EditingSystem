using System.Collections.ObjectModel;

namespace Jewelry.EditingSystem.Tests.TestModels;

public sealed class EditableBasicTestModel : EditableModelBase, IBasicTestModel
{
    public EditableBasicTestModel(History history) : base(history)
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