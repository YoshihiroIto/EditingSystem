using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Jewelry.EditingSystem.Tests.TestModels;

public sealed class DirectFlagTestModel : IFlagTestModel
{
    private readonly History _history;

    public DirectFlagTestModel(History history)
    {
        _history = history;
    }

    public int ChangingCount { get; private set; }

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