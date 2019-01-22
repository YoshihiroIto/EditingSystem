# EditingSystem

Easy to use undo/redo system for .NET Standard.


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


    #region IntCollection

    private ObservableCollection<int> _IntCollection;

    public ObservableCollection<int> IntCollection
    {
        get => _IntCollection;
        set => SetEditableProperty(v => _IntCollection = v, _IntCollection, value);
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

public void Collection()
{
    var history = new History();
    var model = new TestModel(history);

    model.IntCollection = new ObservableCollection<int>();



    model.IntCollection.Add(100);
    model.IntCollection.Add(101);
    model.IntCollection.Add(102);
    model.IntCollection.Add(103);



    model.IntCollection.RemoveAt(3);
    Assert.True(model.IntCollection.SequenceEqual(new[] {100, 101, 102}));



    history.Undo();
    Assert.True(model.IntCollection.SequenceEqual(new[] {100, 101, 102, 103}));



    history.Redo();
    Assert.True(model.IntCollection.SequenceEqual(new[] {100, 101, 102}));
}

```
