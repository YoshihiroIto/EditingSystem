using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Jewelry.EditingSystem.Tests.TestModels;

public sealed class DirectFlagTestModel(History history) : IFlagTestModel
{
    public int ChangingCount { get; private set; }

    public bool IsA
    {
        get => _isA;
        set
        {
            if (this.SetEditableProperty(history, v => _isA = v, _isA, value))
                ++ChangingCount;
        }
    }

    public bool IsB
    {
        get => _isB;
        set
        {
            if (this.SetEditableProperty(history, v => _isB = v, _isB, value))
                ++ChangingCount;
        }
    }

    public bool IsC
    {
        get => _isC;
        set
        {
            if (this.SetEditableProperty(history, v => _isC = v, _isC, value))
                ++ChangingCount;
        }
    }

    private bool _isA;
    private bool _isB;
    private bool _isC;

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
