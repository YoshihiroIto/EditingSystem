---
title: 手動モード
layout: ja-docs
---

# 手動モード

> [English version](/docs/manual-mode/)

新しいプロパティには `[Undoable]` を推奨します。既存コードや setter の制御が必要な場合は、`EditableModelBase`、`SetEditableProperty`、`RecordPropertyChange`、`RecordAppliedPropertyChange` を引き続き使えます。

```csharp
public sealed class ManualDocument : EditableModelBase
{
    private double _x;

    public ManualDocument(History history) : base(history) { }

    public double X
    {
        get => _x;
        set => SetEditableProperty(v => _x = v, _x, value);
    }
}
```

レガシーな setter の移行時や、partial プロパティでは表せない setter 経路には手動モードを選択してください。それ以外では、通常編集と Undo/Redo の経路を揃えられる generated model を優先します。

## 履歴数の制限

履歴数の既定値に上限はありません。長時間動作するエディターでは上限を設定できます。

```csharp
history.MaxUndoCount = 500;
```

## Source Generator と NativeAOT

Generator は、指定された `History`、partial プロパティ実装、`INotifyPropertyChanged` 対応、通知メソッドのアクセシビリティ、Undo/Redo setter コードをコンパイル時に解決します。実行時リフレクションや動的コード生成を使わないため、基本パターンは NativeAOT アプリケーションに適しています。
