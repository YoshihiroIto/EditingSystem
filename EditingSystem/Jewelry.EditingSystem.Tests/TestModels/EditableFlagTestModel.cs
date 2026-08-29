namespace Jewelry.EditingSystem.Tests.TestModels;

public sealed class EditableFlagTestModel(History history) : EditableModelBase(history), IFlagTestModel
{
    public int ChangingCount { get; private set; }

    public bool IsA
    {
        get => _isA;
        set
        {
            if (SetEditableProperty(v => _isA = v, _isA, value))
                ++ChangingCount;
        }
    }

    public bool IsB
    {
        get => _isB;
        set
        {
            if (SetEditableProperty(v => _isB = v, _isB, value))
                ++ChangingCount;
        }
    }

    public bool IsC
    {
        get => _isC;
        set
        {
            if (SetEditableProperty(v => _isC = v, _isC, value))
                ++ChangingCount;
        }
    }

    private bool _isA;
    private bool _isB;
    private bool _isC;
}
