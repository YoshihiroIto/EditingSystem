---
title: 履歴数の制限
layout: ja-docs
---

# 履歴数の制限

> [English version](/docs/history-limits/)

履歴数の既定値に上限はありません。長時間動作するエディターでは、Undo エントリ数を指定してメモリ使用量を制限できます。

```csharp
history.MaxUndoCount = 500;
```

上限はアプリケーションの編集頻度と各操作のサイズに応じて選択してください。この設定は記録済みの Undo エントリに適用され、現在のモデル状態を変更するものではありません。
