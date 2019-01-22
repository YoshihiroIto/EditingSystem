# EditingSystem

Simple implementation of undo/redo system for .NET Standard.


## Example

```cs

public class TestModel : EditableModelBase
{
    public TestModel(History history)
    {
        SetupEditingSystem(history);
    }

    #region IntValue

    private int _IntValue;

    public int IntValue
    {
        get => _IntValue;
        set => SetEditableProperty(v => _IntValue = v, _IntValue, value);
    }

    #endregion
}

public void Basic()
{
    var history = new History();
    var model = new TestModel(history);




    model.IntValue = 123;
    model.IntValue = 456;
    model.IntValue = 789;




    history.Undo();
    Assert.Equal(456, model.IntValue);

    history.Undo();
    Assert.Equal(123, model.IntValue);

    history.Undo();
    Assert.Equal(0, model.IntValue);




    history.Redo();
    Assert.Equal(123, model.IntValue);

    history.Redo();
    Assert.Equal(456, model.IntValue);

    history.Redo();
    Assert.Equal(789, model.IntValue);
}

```
