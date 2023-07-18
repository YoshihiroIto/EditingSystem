using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Jewelry.EditingSystem.Tests.TestModels;

public sealed class DirectTestModel : ITestModel
{
    private readonly History _history;

    public DirectTestModel(History history)
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
        set => this.SetEditableProperty(_history, v => SetField(ref _IntCollection, v), _IntCollection, value);
    }

    #endregion
    
    #region Collection

    private ObservableCollection<CollectionItem> _Collection = new();

    public ObservableCollection<CollectionItem> Collection
    {
        get => _Collection;
        set => this.SetEditableProperty(_history, v => SetField(ref _Collection, v), _Collection, value);
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