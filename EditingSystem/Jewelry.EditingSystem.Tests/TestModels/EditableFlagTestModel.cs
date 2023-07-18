using System.Collections.ObjectModel;

namespace Jewelry.EditingSystem.Tests.TestModels;

public sealed class EditableFlagTestModel : EditableModelBase, IFlagTestModel
{
    public EditableFlagTestModel(History history) : base(history)
    {
    }

    public int ChangingCount { get; private set; }

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
}