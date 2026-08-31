---
title: 記録スコープ
layout: ja-docs
---

# 記録スコープ

> [English version](/docs/recording-scopes/)

すべてのスコープは変更を即時に適用します。違いは履歴の記録方法と、スコープ終了時の扱いです。

| スコープ | 用途 | 結果 |
|---|---|---|
| `Transaction()` | 操作全体をコミットまたはロールバックしたい場合 | コミットすると一つの Undo 操作になり、コミットしなければ追跡済みの変更を戻します。 |
| `Batch()` | 一つのコマンドに属する複数の編集 | 変更を残したまま、一つの Undo 操作にまとめます。 |
| `CoalescingBatch()` | スライダー、ドラッグ、カラーピッカーなどの連続値 | 同じプロパティへの繰り返し書き込みは、最初の古い値と最後の新しい値だけを残します。 |
| `Pause()` | 読み込みや同期 | 変更は適用されますが、履歴には登録されません。 |

```csharp
using (history.CoalescingBatch())
{
    document.X = 100;
    document.X = 120;
    document.X = 140;
}

history.Undo(); // ジェスチャー開始前の値に戻る。
```

失敗時に追跡対象の状態を戻すなら `Transaction()`、まとめるだけなら `Batch()`、中間値が不要な連続操作なら `CoalescingBatch()` を使います。

## Transaction

追跡対象の変更をすべて一つの Undo 操作として確定するか、すべて戻す必要がある操作には Transaction を使います。`Commit()` せずに `using` スコープを抜けると自動的に Rollback されます。

```csharp
using var transaction = history.BeginTransaction();
document.X = 100;
document.Y = 200;
ValidateDocument();
transaction.Commit();
```

Transaction はネストできます。内側を Commit すると親へ統合され、内側を Rollback するとその変更だけを戻します。外側を Rollback した場合は、Commit 済みの内側 Transaction の変更も戻ります。Transaction 内では `Batch()` と `CoalescingBatch()` を使えますが、Batch または Pause の中で Transaction は開始できず、Transaction 中に Pause も開始できません。

最外 Transaction では `TransactionBeginning`、`TransactionCommitting`、`TransactionCommitted`、`TransactionRolledBack` が発生します。最初の二つの event handler が行う変更も同じ Transaction に含まれ、例外が起きると Rollback されます。対象は `History` に記録される変更であり、追跡していないフィールドやファイル、データベース、ネットワークなどの外部副作用は戻しません。

## Batch と Pause

`Batch()` は例外で本体を抜けても Rollback しません。変更は適用されたまま、Dispose 時に一つの Undo 操作として確定します。失敗時に状態を戻すなら Transaction を使います。

`CoalescingBatch()` も同様に Rollback しません。コレクション変更と明示的に Push した action は順序境界になるため、それらをまたいでプロパティ変更が統合されることはありません。

`Pause()` は初期化や同期のためのものです。Rollback ではなく、変更は適用されたままですが、`History` には Undo 用の action が残りません。
