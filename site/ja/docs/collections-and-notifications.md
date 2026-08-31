---
title: コレクションと通知
layout: ja-docs
---

# コレクションと通知

> [English version](/docs/collections-and-notifications/)

`INotifyCollectionChanged` を実装するコレクションを `[Undoable]` プロパティに代入すると、コレクション変更の追跡が接続されます。追加、削除、移動、クリアはプロパティ編集と同じ履歴に入ります。

```csharp
using System.Collections.ObjectModel;

[EditingHistory(nameof(history))]
public sealed partial class Document(History history)
{
    [Undoable]
    public partial ObservableCollection<string> Items { get; set; } = [];
}

document.Items.Add("A");
history.Undo(); // Items は再び空になる。
```

`INotifyPropertyChanged` は必須ではありません。対象型が実装している場合、Generator は対応する通知メソッドをコンパイル時に解決し、通常変更、Undo、Redo すべてで通知を発生させます。通知メソッド名が独自の場合は `[EditingPropertyChanged]` で指定します。

## 操作と通知

| 操作 | 初回通知 | Undo 通知 | Redo 通知 |
|---|---|---|---|
| `Add` / `Insert` | `Add` | `Remove` | `Add` |
| `Remove` / `RemoveAt` | `Remove` | `Add` | `Remove` |
| `Move` | `Move` | `Move` | `Move` |
| `Clear` | `Reset` | 要素ごとに `Add` | 要素ごとに `Remove` |
| `ClearEx(history)` | 要素ごとに `Remove` | 要素ごとに `Add` | 要素ごとに `Remove` |

`ClearEx`、`UnionWithEx`、`IntersectWithEx`、`ExceptWithEx`、`SymmetricExceptWithEx`、`RemoveWhereEx` は、コレクションが監視対象プロパティに代入されていない場合でも明示的に変更を記録します。

```csharp
var values = new List<int> { 1, 2, 3 };
values.ClearEx(history);
history.Undo(); // [1, 2, 3]
```

Set 操作も一つの Undo/Redo action として記録できます。

```csharp
using Jewelry.Collections;

var values = new ObservableHashSet<int>([10]);
values.UnionWithEx([20, 30], history);
history.Undo(); // [10]
history.Redo(); // [10, 20, 30]
```
