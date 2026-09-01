---
title: ダーティー状態と保存ポイント
layout: ja-docs
---

# ダーティー状態と保存ポイント

> [English version](/docs/dirty-state/)

`History.IsDirty` は、現在の履歴位置が最後に保存に成功した位置と異なるかを表します。`MarkSaved()` は保存処理が成功した後にだけ呼び出してください。

```csharp
await SaveDocumentAsync(document);
history.MarkSaved();
```

`MarkSaved()` の直後は `IsDirty` が `false` です。編集すると `true` になり、アンドゥで保存位置へ正確に戻ると再び `false`、Redo すると `true` になります。バッチ中の変更は最外 バッチの終了時、トランザクション中の変更は最外トランザクションのコミット成功時に ダーティーになります。

アンドゥアクションに含まれない外部副作用などには `MarkDirty()` を使います。この明示的な ダーティー状態はアンドゥ/リドゥでは解除されず、`MarkSaved()` まで維持されます。`Clear()` はアンドゥ/リドゥエントリを破棄しますが、保存済みにはしません。`Pause()` 中の変更は履歴と自動 ダーティー追跡の両方から意図的に除外されます。

`IsDirty` は値が変化したときだけ `PropertyChanged` を通知します。バッチまたはトランザクションの実行中に `MarkSaved()` は呼べません。
