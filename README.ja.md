# EditingSystem
[![Biaui NuGet package](https://img.shields.io/nuget/v/Jewelry.EditingSystem)](https://www.nuget.org/packages/Jewelry.EditingSystem) [![Build status](https://ci.appveyor.com/api/projects/status/x42th0lpkuldqhg8?svg=true)](https://ci.appveyor.com/project/YoshihiroIto/editingsystem) [![MIT License](http://img.shields.io/badge/license-MIT-lightgray)](LICENSE)  

[English](README.md)

.NET 向けの使いやすい Undo/Redo システムです。

## インストール
```
PM> Install-Package Jewelry.EditingSystem
```

### CommunityToolkit.Mvvm との連携

`CommunityToolkit.Mvvm` によって生成された Observable プロパティも編集履歴の対象にしたい場合は、連携パッケージをインストールしてください。

```text
PM> Install-Package Jewelry.EditingSystem.CommunityToolkit.Mvvm
```

partial ViewModel に対して履歴を一度設定し、Undo 可能にしたい各 Observable プロパティに属性を付けます。フィールド宣言と C# 13 の partial プロパティ宣言の両方に対応しています。

```cs
using CommunityToolkit.Mvvm.ComponentModel;
using Jewelry.EditingSystem;
using Jewelry.EditingSystem.CommunityToolkit.Mvvm;

[EditingHistory(nameof(_history))]
public sealed partial class SampleViewModel : ObservableObject
{
    private readonly History _history;

    public SampleViewModel(History history)
    {
        _history = history;
    }

    [ObservableProperty]
    [Undoable]
    private string? name;

    [ObservableProperty]
    [Undoable]
    public partial int Count { get; set; }
}
```

`ObservableObject` の継承は必須ではありません。ViewModel がすでに別の基底クラスを継承している場合は、CommunityToolkit.Mvvm の `[INotifyPropertyChanged]` 属性を使用できます。EditingSystem は変更を履歴へ記録し、Undo / Redo 時にも `PropertyChanged` 通知を維持します。

```cs
[INotifyPropertyChanged]
[EditingHistory(nameof(history))]
public sealed partial class DerivedViewModel(History history) : ExistingViewModelBase
{
    [ObservableProperty]
    [Undoable]
    public partial int Count { get; set; }
}
```

1 引数版の `OnXChanging(T value)` と `OnXChanged(T value)` フックは、この連携機能によって予約されています。2 引数版の changing / changed フックはアプリケーションコードから引き続き利用できます。

### 安全な記録スコープ

`IDisposable` スコープを使うことで、アプリケーションコードが例外を送出した場合でも pause や batch の深さが確実に元へ戻ります。

```cs
using (history.Pause())
{
    LoadInitialValues();
}

using (history.Batch())
{
    model.Width = 100;
    model.Height = 200;
}

if (history.TryUndo())
{
    // 1つの操作が Undo されました。
}
```

スライダー、カラーピッカー、3D ギズモなど、連続的な UI 操作には coalescing batch を使用します。同じオブジェクトの同じプロパティに対する連続変更では、最初の古い値と最後の新しい値だけが保持されます。異なるプロパティや異なるオブジェクトは、それぞれ独立してまとめられます。

```cs
void OnDragStarted()
{
    history.BeginCoalescingBatch();
}

void OnDragChanged(Color colorValue, Vector3 position1, Vector3 position2)
{
    // 1回のドラッグ中に繰り返し呼び出されます。
    color.R = colorValue.R;
    color.G = colorValue.G;
    color.B = colorValue.B;

    selectedObject1.Position = position1;
    selectedObject2.Position = position2;
}

void OnDragCompleted()
{
    history.EndCoalescingBatch();
}

// 同等のスコープ形式:
using (history.CoalescingBatch())
{
    ApplyContinuousChanges();
}
```

途中の値は引き続き即座に適用されますが、Undo / Redo 時には最終的に得られた各プロパティ値がそれぞれ 1 回だけ適用されます。最終値が初期値と同じになったプロパティについては、履歴エントリは作成されません。

コレクション変更や明示的な `Push` 呼び出しなど、プロパティ変更以外のアクションは順序上の境界として扱われます。そのため、境界をまたいで同じプロパティが変更された場合、それらは別々の変更として保持されます。

`EditableModelBase`、direct-mode の `SetEditableProperty`、CommunityToolkit.Mvvm 連携では、プロパティキーが自動的に提供されます。`History` を直接使って変更を記録するコードでは、対象オブジェクトとプロパティ名を指定するオーバーロードを使用してください。

```cs
history.RecordAppliedPropertyChange(
    this,
    nameof(Value),
    value => Value = value,
    oldValue,
    newValue);
```

通常の batch と coalescing batch を互いにネストすることはできません。同種の通常 batch 同士、または同種の coalescing batch 同士のネストには対応しています。すべての中間操作とその順序をそのまま再生する必要がある場合は通常の `Batch` を、連続的なプロパティ編集には `CoalescingBatch` を使用してください。

長時間動作するエディターでは、デフォルトの無制限動作を変えることなく、保持する履歴数に上限を設定できます。

```cs
history.MaxUndoCount = 500;
```

### Undo 可能なコレクション操作

編集可能なコレクションプロパティに設定されたコレクションへの変更は、自動的に記録されます。`ObservableCollection<T>` では、通常の insert、remove、move 操作について、Undo / Redo 時にも差分通知が維持されます。

| 操作 | 初回通知 | Undo 通知 | Redo 通知 |
| --- | --- | --- | --- |
| `Add` / `Insert` | `Add` | `Remove` | `Add` |
| `Remove` / `RemoveAt` | `Remove` | `Add` | `Remove` |
| `Move` | `Move` | `Move` | `Move` |
| `Clear` | `Reset` | 要素ごとに 1 回の `Add` | 要素ごとに 1 回の `Remove` |
| `ClearEx(history)` | 要素ごとに 1 回の `Remove` | 要素ごとに 1 回の `Add` | 要素ごとに 1 回の `Remove` |

`ObservableCollection<T>.Clear()` は、最初に呼び出された時点で必ず `Reset` を通知します。EditingSystem はこの最初の通知を防ぐことはできませんが、Undo / Redo では要素単位で差分再生するため、追加の `Reset` は発生しません。

バインドされたコントロールに `Reset` を受け取らせたくない場合は `ClearEx` を使用してください。たとえば、`Reset` によって WPF の `ListBox` 全体が再描画されることを避けたい場合に有効です。`ClearEx` は要素ごとに `Remove` 通知を発生させますが、クリア操作全体は 1 つの Undo 操作として記録されます。

```cs
model.IntCollection.ClearEx(history);
history.Undo(); // Add 通知で要素を復元します。Reset は発生しません。
history.Redo(); // Remove 通知で要素を削除します。Reset は発生しません。
```

`Clear` は、対象コレクションがすでに `History` によって監視されている場合にのみ Undo 可能です。一方 `ClearEx` は、編集可能なプロパティに設定されていないコレクションに対しても使用できます。その代わり、大きなコレクションでは単一の `Reset` ではなく要素ごとに 1 回の通知が発生します。

`UnionWithEx`、`IntersectWithEx`、`ExceptWithEx`、`SymmetricExceptWithEx`、`RemoveWhereEx` も同様に、1 つの Undo 操作を直接記録し、監視対象プロパティに設定されていないコレクションでも使用できます。

## 使用例

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
    using var history = new History();
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
    using var history = new History();
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

### 通常の INotifyPropertyChanged オブジェクトのサポート

`INotifyPropertyChanged` を実装したオブジェクトでも、実装方法を問わず利用できます。

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
    using var history = new History();
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
    using var history = new History();
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

## 作者

Yoshihiro Ito  
Twitter: [https://twitter.com/yoiyoi322](https://twitter.com/yoiyoi322)  
Email: yo.i.jewelry.bab@gmail.com  

## ライセンス

MIT
