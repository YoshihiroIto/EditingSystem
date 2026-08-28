using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using Jewelry.Collections;

namespace Jewelry.EditingSystem.Tests.TestModels;

public sealed class DirectBasicTestModel(History history) : IBasicTestModel
{
    public int ChangingCount { get; private set; }

    #region IntValue

    private int _IntValue;

    public int IntValue
    {
        get => _IntValue;
        set
        {
            if (this.SetEditableProperty(history, v => SetField(ref _IntValue, v), _IntValue, value))
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
            if (this.SetEditableProperty(history, v => SetField(ref _StringValue, v), _StringValue, value))
                ++ChangingCount;
        }
    }

    #endregion

    #region IntCollection

    private ObservableCollection<int> _IntCollection = new();

    public ObservableCollection<int> IntCollection
    {
        get => _IntCollection;
        set => this.SetEditableProperty(history, v => SetField(ref _IntCollection, v), _IntCollection, value);
    }

    #endregion
    
    #region Collection

    private ObservableCollection<CollectionItem> _Collection = new();

    public ObservableCollection<CollectionItem> Collection
    {
        get => _Collection;
        set => this.SetEditableProperty(history, v => SetField(ref _Collection, v), _Collection, value);
    }

    #endregion

    #region IntSet

    private ObservableHashSet<int> _IntSet = new();

    public ObservableHashSet<int> IntSet
    {
        get => _IntSet;
        set => this.SetEditableProperty(history, v => SetField(ref _IntSet, v), _IntSet, value);
    }

    #endregion

    #region IntDictionary

    private ObservableDictionary<string, int> _IntDictionary = new();

    public ObservableDictionary<string, int> IntDictionary
    {
        get => _IntDictionary;
        set => this.SetEditableProperty(history, v => SetField(ref _IntDictionary, v), _IntDictionary, value);
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
