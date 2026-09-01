---
title: 手動モード
layout: ja-docs
---

# 手動モード

> [English version](/docs/manual-mode/)

新しいプロパティには `[Undoable]` を推奨します。既存コードや セッターの制御が必要な場合は、`EditableModelBase`、`SetEditableProperty`、`RecordPropertyChange`、`RecordAppliedPropertyChange` を引き続き使えます。

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

既存の基底クラスがあるモデルも、履歴へ参加できます。`INotifyPropertyChanged` を実装し、`History` を渡す `SetEditableProperty` 拡張メソッドを使います。コールバックは通常編集・アンドゥ・リドゥのすべてで使われるため、フィールドの更新と通常の通知はここで行います。

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

値を適用する前に記録するセッター経路では、`history.RecordPropertyChange(this, nameof(X), ApplyX, _x, value)` を使い、`true` のときだけ `ApplyX(value)` を呼びます。値がすでに適用済みの変更後フックでは `RecordAppliedPropertyChange` を使います。どちらも アンドゥ/リドゥで同じフィールド更新と通知を行うコールバックを指定してください。

レガシーな セッターの移行時や、パーシャルプロパティでは表せない セッター経路には手動モードを選択してください。それ以外では、通常編集と アンドゥ/リドゥの経路を揃えられる生成済みモデルを優先します。
