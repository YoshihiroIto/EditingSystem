---
title: はじめに
layout: ja-docs
---

# はじめに

> [English version](/docs/getting-started/)

コアパッケージを導入し、モデルに `History` を渡して、Undo/Redo の対象にする generated partial プロパティへ印を付けます。

```shell
dotnet add package Jewelry.EditingSystem
```

```csharp
using Jewelry.EditingSystem;
using Jewelry.EditingSystem.Annotations;

[EditingHistory(nameof(history))]
public sealed partial class Document(History history)
{
    [Undoable]
    public partial string? Name { get; set; }

    [Undoable]
    public partial double X { get; set; }
}

using var history = new History();
var document = new Document(history);

document.X = 100;
document.X = 200;
history.Undo(); // 100
history.Redo(); // 200
```

`EditingHistory` には、non-null の `History` フィールド、プロパティ、または primary constructor parameter を指定できます。Generator が setter 実装と記録コードを生成するため、実行時リフレクションや動的コード生成は不要です。

続けて、まとまった編集のための [記録スコープ](recording-scopes.md) を選びます。

## `INotifyPropertyChanged` との同時利用

通知は必須ではありません。対象型が `INotifyPropertyChanged` を実装している場合、Generator は通知経路をコンパイル時に解決し、通常変更、Undo、Redo のすべてで維持します。

```csharp
using System.ComponentModel;

[EditingHistory(nameof(history))]
public sealed partial class Document(History history) : INotifyPropertyChanged
{
    [Undoable]
    public partial string? Name { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
```

通知メソッド名が異なる場合は `[EditingPropertyChanged]` で明示します。対象はアクセス可能なインスタンス `void` メソッドで、引数は `string` または `PropertyChangedEventArgs` を一つ取ります。明示したメソッドが不正なら `JES006` になります。

属性を指定しない場合、Generator は `RaisePropertyChanged`、`OnPropertyChanged`、`NotifyPropertyChanged` を、まず `string` 引数、次に `PropertyChangedEventArgs` 引数で探索します。対象 partial class 自身の `PropertyChanged` event も利用でき、基底クラスで継承した `protected` メソッドも対応します。通知経路を見つけられなくても Undo/Redo は動作し、`JES005` warning が報告されます。
