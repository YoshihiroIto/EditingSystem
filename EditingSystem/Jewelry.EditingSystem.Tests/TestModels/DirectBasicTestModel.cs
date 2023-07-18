using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Jewelry.EditingSystem.Tests.TestModels;

public sealed class DirectBasicTestModel : IBasicTestModel
{
    private readonly History _history;

    public DirectBasicTestModel(History history)
    {
        _history = history;
    }

    public int ChangingCount { get; private set; }

    #region IntValue

    private int _IntValue;

    public int IntValue
    {
        get => _IntValue;
        set
        {
            if (this.SetEditableProperty(_history, v => SetField(ref _IntValue, v), _IntValue, value))
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
            if (this.SetEditableProperty(_history, v => SetField(ref _StringValue, v), _StringValue, value))
                ++ChangingCount;
        }
    }

    #endregion

    #region IntCollection

    private ObservableCollection<int> _IntCollection = new();

    public ObservableCollection<int> IntCollection
    {
        get => _IntCollection;
        set => this.SetEditableProperty(_history, v => SetField(ref _IntCollection, v), _IntCollection, value);
    }

    #endregion
    
    #region Collection

    private ObservableCollection<CollectionItem> _Collection = new();

    public ObservableCollection<CollectionItem> Collection
    {
        get => _Collection;
        set => this.SetEditableProperty(_history, v => _Collection = v, _Collection, value);
    }

    #endregion

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // ReSharper disable once UnusedMethodReturnValue.Local
    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}