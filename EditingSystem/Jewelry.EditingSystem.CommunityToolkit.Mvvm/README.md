# Jewelry.EditingSystem.CommunityToolkit.Mvvm

`Jewelry.EditingSystem` と `CommunityToolkit.Mvvm` を組み合わせるための第一級の連携パッケージです。

EditingSystem では次の2つを主要な利用パターンとして扱います。

- `Jewelry.EditingSystem` の standalone `[Undoable]` partial property
- `CommunityToolkit.Mvvm` の `[Undoable, ObservableProperty]`

このパッケージは後者を提供します。従来の CommunityToolkit.Mvvm 連携コードはそのまま利用できます。

```cs
using CommunityToolkit.Mvvm.ComponentModel;
using Jewelry.EditingSystem;
using Jewelry.EditingSystem.CommunityToolkit.Mvvm;

[EditingHistory(nameof(_history))]
public sealed partial class SampleViewModel : ObservableObject
{
    private readonly History _history;

    public SampleViewModel(History history) => _history = history;

    [Undoable, ObservableProperty]
    private string? name;

    [Undoable, ObservableProperty]
    public partial int Count { get; set; }
}
```

Undo / Redo では CommunityToolkit.Mvvm が生成したプロパティ setter を再利用します。そのため、CommunityToolkit.Mvvm の setter pipeline に含まれる `PropertyChanging` / `PropertyChanged`、validation、command notification、recipient broadcast などが Undo / Redo 時にも維持されます。

`ObservableObject` の継承は必須ではありません。すでに別の基底クラスを継承している partial ViewModel では CommunityToolkit.Mvvm の `[INotifyPropertyChanged]` 属性も利用できます。

```cs
[INotifyPropertyChanged]
[EditingHistory(nameof(history))]
public sealed partial class DerivedViewModel(History history) : ExistingViewModelBase
{
    [Undoable, ObservableProperty]
    public partial int Count { get; set; }
}
```

CommunityToolkit.Mvvm 連携では、1引数版の `OnXChanging(T value)` と `OnXChanged(T value)` フックを EditingSystem が使用します。アプリケーション固有処理には2引数版の changing / changed フックを利用してください。

`EditingHistory` には非 null の `History` フィールド、プロパティ、または primary constructor parameter を指定できます。

## Standalone `[Undoable]` との使い分け

CommunityToolkit.Mvvm の Observable property が必要ないモデルでは、`Jewelry.EditingSystem` 本体だけで standalone `[Undoable]` partial property を利用できます。

```cs
using Jewelry.EditingSystem;
using Jewelry.EditingSystem.Annotations;

[EditingHistory(nameof(history))]
public sealed partial class Model(History history)
{
    [Undoable]
    public partial int Value { get; set; }
}
```

名前の衝突と既存コードへの影響を避けるため、standalone 用属性は `Jewelry.EditingSystem.Annotations`、CommunityToolkit.Mvvm 連携用属性は従来どおり `Jewelry.EditingSystem.CommunityToolkit.Mvvm` に配置しています。

手書きの `SetEditableProperty(...)` 方式も互換性・特殊用途向けの低レベル API として残りますが、通常は上記2つの属性ベースのパターンを優先してください。
