---
title: Dirty 状態と保存地点
layout: ja-docs
---

# Dirty 状態と保存地点

> [English version](/docs/dirty-state/)

`History.IsDirty` は、現在の履歴位置が最後に保存に成功した位置と異なるかを表します。`MarkSaved()` は保存処理が成功した後にだけ呼び出してください。

```csharp
await SaveDocumentAsync(document);
history.MarkSaved();
```

`MarkSaved()` の直後は `IsDirty` が `false` です。編集すると `true` になり、Undo で保存位置へ正確に戻ると再び `false`、Redo すると `true` になります。Batch 中の変更は最外 Batch の終了時、Transaction 中の変更は最外 Transaction のコミット成功時に Dirty になります。

Undo action に含まれない外部副作用などには `MarkDirty()` を使います。この明示的な Dirty 状態は Undo/Redo では解除されず、`MarkSaved()` まで維持されます。`Clear()` は Undo/Redo エントリを破棄しますが、保存済みにはしません。`Pause()` 中の変更は履歴と自動 Dirty 追跡の両方から意図的に除外されます。

`IsDirty` は値が変化したときだけ `PropertyChanged` を通知します。Batch または Transaction の実行中に `MarkSaved()` は呼べません。
