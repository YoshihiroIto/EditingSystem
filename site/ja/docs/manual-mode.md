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

## `EditableModelBase` を継承しない直接モード

既存の基底クラスがあるモデルも、履歴へ参加できます。`INotifyPropertyChanged` を実装し、`History` を渡す `SetEditableProperty` 拡張メソッドを使います。コールバックは通常編集・Undo・Redo のすべてで使われるため、フィールドの更新と通常の通知はここで行います。

```csharp
using System.ComponentModel;

public sealed class ExistingDocument : SomeExistingBase, INotifyPropertyChanged
{
    private readonly History _history;
    private double _x;

    public ExistingDocument(History history) => _history = history;

    public event PropertyChangedEventHandler? PropertyChanged;

    public double X
    {
        get => _x;
        set => this.SetEditableProperty(_history, ApplyX, _x, value);
    }

    private void ApplyX(double value)
    {
        _x = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(X)));
    }
}
```

値が変わらないとき、`SetEditableProperty` は `false` を返します。実際に変更されたときだけ実行する追加処理がある場合は、戻り値が `true` のときだけ行ってください。

値を適用する前に記録する setter 経路では、`history.RecordPropertyChange(this, nameof(X), ApplyX, _x, value)` を使い、`true` のときだけ `ApplyX(value)` を呼びます。値がすでに適用済みの post-change hook では `RecordAppliedPropertyChange` を使います。どちらも Undo/Redo で同じフィールド更新と通知を行うコールバックを指定してください。

レガシーな setter の移行時や、partial プロパティでは表せない setter 経路には手動モードを選択してください。それ以外では、通常編集と Undo/Redo の経路を揃えられる generated model を優先します。
