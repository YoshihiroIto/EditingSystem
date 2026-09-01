# EditingSystem

[![Jewelry.EditingSystem NuGet package](https://img.shields.io/nuget/v/Jewelry.EditingSystem)](https://www.nuget.org/packages/Jewelry.EditingSystem) [![Build status](https://ci.appveyor.com/api/projects/status/x42th0lpkuldqhg8?svg=true)](https://ci.appveyor.com/project/YoshihiroIto/editingsystem) [![MIT License](http://img.shields.io/badge/license-MIT-lightgray)](LICENSE)

[English](README.md)

<p align="center">
  <img src="Resouces/icon.svg" alt="EditingSystem" width="160">
</p>

> 詳しいガイドは [yoshihiroito.github.io/EditingSystem/ja](https://yoshihiroito.github.io/EditingSystem/ja/) でも読めます。

NativeAOT フレンドリーな .NET 向け Undo/Redo ライブラリです。通常のプロパティ編集、連続編集、バッチ処理、コレクション変更を同じ `History` で扱えます。Source Generator はプロパティ setter をコンパイル時に生成し、任意の `ICollection<T>` 実装を扱うコレクションアダプターにも NativeAOT 対応の経路があります。

[ブラウザでデモを試す。](https://yoshihiroito.github.io/EditingSystem/demo/)

## インストール

```text
PM> Install-Package Jewelry.EditingSystem
```

## 基本パターン: `[Undoable]`

通常は、Undo/Redo したいプロパティを C# の partial プロパティとして宣言し、`[Undoable]` を付けます。

```cs
using Jewelry.EditingSystem;
using Jewelry.EditingSystem.Annotations;

[EditingHistory(nameof(history))]
public sealed partial class Document(History history)
{
    [Undoable]
    public partial string? Name { get; set; }

    [Undoable]
    public partial double X { get; set; }

    [Undoable]
    public partial double Y { get; set; }
}
```

Source Generator がプロパティの実装と履歴登録コードを生成します。

```cs
using var history = new History();
var document = new Document(history);

document.X = 100;
document.X = 200;

history.Undo(); // X == 100
history.Undo(); // X == 0
history.Redo(); // X == 100
```

`EditingHistory` には、非 null の `History` フィールド、プロパティ、または primary constructor parameter を指定できます。

### `INotifyPropertyChanged` との同時利用

`[Undoable]` は `INotifyPropertyChanged` を必須にしません。ただし対象型が `INotifyPropertyChanged` を実装している場合は、Source Generator が通知経路をコンパイル時に解決し、通常変更・Undo・Redo のすべてで `PropertyChanged` を発生させます。

```cs
using System.ComponentModel;
using Jewelry.EditingSystem;
using Jewelry.EditingSystem.Annotations;

[EditingHistory(nameof(history))]
public sealed partial class Document(History history) : INotifyPropertyChanged
{
    [Undoable]
    public partial string? Name { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
```

#### 通知メソッドを明示する

通知メソッド名が `RaisePropertyChanged`、`OnPropertyChanged`、`NotifyPropertyChanged` のいずれでもない場合は、`[EditingPropertyChanged]` で使用するメソッドを明示できます。

```cs
[EditingHistory(nameof(history))]
[EditingPropertyChanged(nameof(InvokePropertyChanged))]
public sealed partial class Document(History history) : INotifyPropertyChanged
{
    [Undoable]
    public partial string? Name { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void InvokePropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
```

`EditingPropertyChanged` で指定したメソッドは自動探索より優先され、通常変更・Undo・Redo のすべてで呼び出されます。指定できるのは、生成対象型からアクセス可能なインスタンス `void` メソッドで、引数は `string` または `PropertyChangedEventArgs` の1つです。ジェネリックメソッドは使用できません。

指定したメソッドが存在しない、シグネチャが異なる、またはアクセスできない場合は `JES006` error になります。

`EditingPropertyChanged` を指定していない場合、通知方法は次の順で探索します。

1. アクセス可能な `RaisePropertyChanged(string)`
2. アクセス可能な `OnPropertyChanged(string)`
3. アクセス可能な `NotifyPropertyChanged(string)`
4. アクセス可能な `RaisePropertyChanged(PropertyChangedEventArgs)`
5. アクセス可能な `OnPropertyChanged(PropertyChangedEventArgs)`
6. アクセス可能な `NotifyPropertyChanged(PropertyChangedEventArgs)`
7. 対象 partial class 自身が宣言している `PropertyChanged` event

基底クラス上の `protected` 通知メソッドも対象です。アクセシビリティは Roslyn がコンパイル時に判定します。`INotifyPropertyChanged` を実装しているのに通知経路を解決できない場合、Undo/Redo は有効なまま `JES005` warning を報告します。

## CommunityToolkit.Mvvm との連携

CommunityToolkit.Mvvm との連携は主要な利用方法の一つです。従来の使用方法はそのまま利用できます。

```text
PM> Install-Package Jewelry.EditingSystem.CommunityToolkit.Mvvm
```

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

    [Undoable, ObservableProperty]
    private string? name;

    [Undoable, ObservableProperty]
    public partial int Count { get; set; }
}
```

この連携では CommunityToolkit.Mvvm が生成する setter をそのまま Undo/Redo に利用します。そのため `PropertyChanged` / `PropertyChanging`、validation、command notification、recipient broadcast など CommunityToolkit.Mvvm の setter pipeline が Undo/Redo 時にも維持されます。

`ObservableObject` の継承は必須ではありません。既存の基底クラスがある場合は CommunityToolkit.Mvvm の `[INotifyPropertyChanged]` 属性も利用できます。

```cs
[INotifyPropertyChanged]
[EditingHistory(nameof(history))]
public sealed partial class DerivedViewModel(History history) : ExistingViewModelBase
{
    [Undoable, ObservableProperty]
    public partial int Count { get; set; }
}
```

CommunityToolkit.Mvvm 連携では、1 引数版の `OnXChanging(T value)` / `OnXChanged(T value)` を EditingSystem が使用します。アプリケーション固有処理には2引数版フックを利用してください。

> `Jewelry.EditingSystem.Annotations` の属性は EditingSystem 単体の partial property 用、`Jewelry.EditingSystem.CommunityToolkit.Mvvm` の属性は CommunityToolkit.Mvvm の `[ObservableProperty]` 連携用です。既存の CommunityToolkit.Mvvm コードは変更不要です。

## 記録スコープの使い分け

`Transaction`、`Batch`、`CoalescingBatch`、`Pause` は、いずれも `History` が変更をどう記録するかを制御しますが、目的が異なります。どのスコープでもモデルの変更はその場で適用されます。異なるのは、何を履歴へ残し、スコープ終了時にどう扱うかです。

| スコープ | 目的 | スコープ終了時 | 履歴上の結果 | 主な用途 |
| --- | --- | --- | --- | --- |
| `Transaction` | 記録対象の一連の変更を「全成功または全取消」にする | `Commit()` すれば全変更を維持し、未 Commit の Dispose または `Rollback()` では元に戻す | Commit 後にだけ1回の Undo | 検証に失敗する可能性がある操作、全体を取り消せる必要がある操作 |
| `Batch` | 複数の変更をひとまとまりにする | 例外でスコープを抜けた場合も変更は残る | すべての中間変更を含む1回の Undo | 複数オブジェクトの移動、関連する複数プロパティの更新 |
| `CoalescingBatch` | 連続編集をまとめ、冗長なプロパティ履歴を除く | 例外でスコープを抜けた場合も変更は残る | 1回の Undo。同じ対象・同じプロパティへの反復変更は最初の旧値と最後の新値だけを保持 | スライダー、カラーピッカー、ドラッグ、ギズモ |
| `Pause` | 一時的に履歴記録を止める | 変更は残るが、この `History` では元に戻せない | Undo 履歴を作らない | 初期化、読み込み、履歴へ含めない同期処理 |

Rollback が必要なら `Transaction`、まとめるだけなら `Batch`、高頻度な連続値には `CoalescingBatch`、Undo 対象にしない変更には `Pause` を選びます。

Transaction 内では `Batch()` と `CoalescingBatch()` を使用できます。Transaction 自体が1回の Undo として Commit されるため、単にまとめる目的だけなら内側の `Batch()` は不要です。内側の `CoalescingBatch()` は、中間値の再生を省くために有効です。Batch または Pause の中では Transaction を開始できず、Transaction 中に Pause を開始することもできません。

## Transaction

記録対象の変更をすべて1回の Undo として確定するか、すべて元に戻す必要がある操作には Transaction を使用します。`Commit()` せずに `using` スコープを抜けると自動的に Rollback されます。

```cs
using var transaction = history.BeginTransaction();

document.X = 100;
document.Y = 200;
ValidateDocument();

transaction.Commit();
```

Transaction はネストできます。内側の Transaction を Commit すると親へ統合され、Rollback すると内側を開始してからの変更だけが戻ります。外側を Rollback した場合は、Commit 済みの内側 Transaction の変更も戻ります。

Transaction 内では `Batch()` と `CoalescingBatch()` を使用できます。これらのスコープは Transaction を Commit または Rollback する前に終了してください。Batch または Pause の中では Transaction を開始できず、Transaction 中に Pause を開始することもできません。

最外 Transaction では `TransactionBeginning`、`TransactionCommitting`、`TransactionCommitted`、`TransactionRolledBack` が発生します。`TransactionBeginning` と `TransactionCommitting` の handler による変更も同じ Transaction に含まれ、いずれかの handler が例外を送出すると Transaction 全体が Rollback されます。

Transaction の対象は、生成プロパティ、手動 action、監視中のコレクションなど、`History` に記録される変更です。追跡されていないフィールドや、ファイル・データベース・ネットワークなどの外部副作用は Rollback されません。

## Batch

複数の変更を1回の Undo として扱う場合は `Batch()` を使います。

```cs
using (history.Batch())
{
    document.X = 100;
    document.Y = 200;
}
```

`Batch()` には Rollback 機能がありません。処理中に例外が発生しても、それまでの変更は適用されたままで、スコープの Dispose 時に1回の Undo として確定します。失敗時に元の状態へ戻す必要がある場合は `Transaction` を使用します。

## CoalescingBatch

スライダー、カラーピッカー、ドラッグ、3D ギズモなどの連続操作には `CoalescingBatch()` を使用します。

```cs
using (history.CoalescingBatch())
{
    document.X = 100;
    document.X = 120;
    document.X = 140;
}
```

同じ対象・同じプロパティへの連続変更は、最初の古い値と最後の新しい値だけにまとめられます。`[Undoable]` で生成されたプロパティは対象オブジェクトとプロパティ名を自動的に履歴キーとして使用します。

`CoalescingBatch()` も `Batch()` と同様に履歴をまとめる機能であり、処理中の例外による変更を Rollback しません。コレクション変更と明示的に Push した action は順序境界として残り、その境界を越えてプロパティ変更が結合されることはありません。

## Pause

`Pause()` を使うと、初期値ロードなどを履歴に残さず実行できます。

```cs
using (history.Pause())
{
    LoadInitialValues();
}
```

`Pause()` は Rollback 機能ではありません。Pause 中の変更は適用されたままですが、`History` には元へ戻すための action が記録されません。

## Dirty 状態と保存地点

`History.IsDirty` は、現在の履歴位置が最後に保存に成功した位置と異なるかを表します。新しい `History` の初期状態は false です。保存処理そのものはアプリケーション側で行い、`MarkSaved()` は保存が成功した後にだけ呼び出します。

```cs
await SaveDocumentAsync(document);
history.MarkSaved();
```

`MarkSaved()` の呼び出し直後は `IsDirty` が false になります。その後、新たに編集すると true になり、Undo で保存位置へ正確に戻ると false、そこから Redo すると再び true になります。Batch 内の変更は最外 Batch の終了時、Transaction 内の変更は最外 Transaction の Commit 成功時に Dirty になります。空操作や Rollback された Transaction は Dirty を変更しません。

Undo 履歴に含まれない外部副作用などは `MarkDirty()` で明示します。この Dirty 状態は Undo/Redo では解除されず、`MarkSaved()` まで維持されます。`Clear()` は Undo/Redo 履歴だけを破棄し、保存済みであることを意味しません。`Pause()` 中の変更は、履歴と自動 Dirty 追跡の両方から意図的に除外されます。

`IsDirty` の値が変化した場合だけ、`History` の `PropertyChanged` から通知されます。Batch または Transaction の実行中には `MarkSaved()` を呼べません。

## Undo 可能なコレクション操作

`History` が監視している `INotifyCollectionChanged` コレクションは、要素操作も Undo/Redo できます。`[Undoable]` プロパティにコレクションを代入すると、そのコレクションの変更が自動的に監視されます。

```cs
using System.Collections.ObjectModel;
using Jewelry.EditingSystem;
using Jewelry.EditingSystem.Annotations;

[EditingHistory(nameof(_history))]
public sealed partial class CollectionDocument
{
    private readonly History _history;

    public CollectionDocument(History history)
    {
        _history = history;
        using (history.Pause())
            Items = [];
    }

    [Undoable]
    public partial ObservableCollection<string> Items { get; set; }
}

using var history = new History();
var document = new CollectionDocument(history);

document.Items.Add("A");
document.Items.Add("B");

history.Undo(); // ["A"]
history.Redo(); // ["A", "B"]

document.Items.Move(1, 0); // ["B", "A"]
history.Undo();             // ["A", "B"]

document.Items.Clear();
history.Undo(); // ["A", "B"]
```

主な操作と通知は次のとおりです。

| 操作 | 初回通知 | Undo 通知 | Redo 通知 |
| --- | --- | --- | --- |
| `Add` / `Insert` | `Add` | `Remove` | `Add` |
| `Remove` / `RemoveAt` | `Remove` | `Add` | `Remove` |
| `Move` | `Move` | `Move` | `Move` |
| `Clear` | `Reset` | 要素ごとに `Add` | 要素ごとに `Remove` |
| `ClearEx(history)` | 要素ごとに `Remove` | 要素ごとに `Add` | 要素ごとに `Remove` |

`ClearEx`、`UnionWithEx`、`IntersectWithEx`、`ExceptWithEx`、`SymmetricExceptWithEx`、`RemoveWhereEx` は、コレクションプロパティとして監視されていない場合でも明示的に `History` へ記録できます。

```cs
var values = new List<int> { 1, 2, 3 };

values.ClearEx(history);
// values == []

history.Undo();
// values == [1, 2, 3]

history.Redo();
// values == []
```

セット操作も1回の Undo/Redo として記録できます。

```cs
using Jewelry.Collections;

var values = new ObservableHashSet<int>([10]);

values.UnionWithEx([20, 30], history);
// values == [10, 20, 30]

history.Undo();
// values == [10]

history.Redo();
// values == [10, 20, 30]
```

## 履歴数の制限

デフォルトでは履歴数に上限はありません。長時間動作するエディターでは上限を設定できます。

```cs
history.MaxUndoCount = 500;
```

## 手書き方式: 低レベル・オプション API

`[Undoable]` を基本パターンとして推奨します。

既存コードとの統合や、setter の制御を完全にアプリケーション側で持ちたい場合は、従来の `EditableModelBase` / `SetEditableProperty` / `RecordPropertyChange` / `RecordAppliedPropertyChange` も引き続き利用できます。

```cs
public sealed class ManualModel : EditableModelBase
{
    private int _value;

    public ManualModel(History history) : base(history)
    {
    }

    public int Value
    {
        get => _value;
        set => SetEditableProperty(v => _value = v, _value, value);
    }
}
```

### `EditableModelBase` を継承しない直接モード

別の基底クラスを継承する必要があるモデルでは、`INotifyPropertyChanged` を実装し、`History` を渡す `SetEditableProperty` 拡張メソッドを使えます。コールバックは通常編集・Undo・Redo で共通して使われるため、値の適用と通常の通知をここで行います。

```cs
public sealed class ExistingModel : SomeExistingBase, INotifyPropertyChanged
{
    private readonly History _history;
    private int _value;

    public ExistingModel(History history) => _history = history;

    public event PropertyChangedEventHandler? PropertyChanged;

    public int Value
    {
        get => _value;
        set => this.SetEditableProperty(_history, ApplyValue, _value, value);
    }

    private void ApplyValue(int value)
    {
        _value = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
    }
}
```

値を適用する前に記録する setter は、`history.RecordPropertyChange(this, nameof(Value), ApplyValue, _value, value)` を呼び、`true` のときだけ値を適用します。すでに値が適用された post-change hook では `RecordAppliedPropertyChange` を使います。どちらも Undo/Redo でフィールドと通知を復元するコールバックを指定してください。

この手書き方式は互換性のため維持しますが、一般的なプロパティでは `[Undoable]` の利用を優先してください。

## Source Generator と NativeAOT

`[Undoable]` の以下の処理はすべてコンパイル時に解決されます。

- `History` メンバーの解決
- partial property の実装生成
- `INotifyPropertyChanged` 実装判定
- `[EditingPropertyChanged]` で明示された通知メソッドの解決
- 通知メソッドの継承階層探索
- 通知メソッドのアクセシビリティ判定
- Undo/Redo setter の生成

生成プロパティは NativeAOT フレンドリーです。setter と通知コードをコンパイル時に生成します。任意の `ICollection<T>` 実装を扱うコレクションアダプターにも NativeAOT 対応の経路があります。リフレクションの適用範囲はサイトの説明を参照してください。

## Avalonia デモ

[`EditingSystem/Jewelry.EditingSystem.Avalonia.Demo`](EditingSystem/Jewelry.EditingSystem.Avalonia.Demo) に Avalonia 12 のデモがあります。CommunityToolkit.Mvvm 連携、Undo/Redo、`Batch`、`CoalescingBatch`、複数オブジェクト編集、Z-order の Undo/Redo などを確認できます。

![demo](site/img/demo00.png)

## Author

Yoshihiro Ito  
Twitter: [https://twitter.com/yoiyoi322](https://twitter.com/yoiyoi322)  
Email: yo.i.jewelry.bab@gmail.com

## License

MIT
