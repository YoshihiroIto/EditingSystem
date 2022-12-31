# EditingSystem
[![Biaui NuGet package](https://img.shields.io/nuget/v/Jewelry.EditingSystem)](https://www.nuget.org/packages/Jewelry.EditingSystem) [![Build status](https://ci.appveyor.com/api/projects/status/x42th0lpkuldqhg8?svg=true)](https://ci.appveyor.com/project/YoshihiroIto/editingsystem) ![.NET Standard Version: >= 2.0](https://img.shields.io/badge/.NET%20Standard-%3E%3D%202.0-brightgreen)  [![MIT License](http://img.shields.io/badge/license-MIT-lightgray)](LICENSE)  

Easy to use undo/redo system for .NET Standard.


## Install
```
PM> Install-Package Jewelry.EditingSystem
```


## Example

```cs
using Jewelry.EditingSystem;

public class TestModel : EditableModelBase
{
    public TestModel(History history) : base(history)
    {
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

    private ObservableCollection<int> _IntCollection = new();

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

### Support plane INotifyPropertyChanged object

Can be implemented in any way for INotifyPropertyChanged objects.

```cs
public sealed class TestModel : INotifyPropertyChanged
{
    private readonly History _history;

    public TestModel(History history)
    {
        _history = history;
    }
    
    #region IntValue

    private int _IntValue;

    public int IntValue
    {
        get => _IntValue;
        set => this.SetEditableProperty(_history, v => SetField(ref _IntValue, v), _IntValue, value);
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


    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
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


## Author

Yoshihiro Ito  
Twitter: [https://twitter.com/yoiyoi322](https://twitter.com/yoiyoi322)  
Email: yo.i.jewelry.bab@gmail.com  


## License

MIT


