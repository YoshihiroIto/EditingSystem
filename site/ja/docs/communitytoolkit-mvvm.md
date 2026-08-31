---
title: CommunityToolkit.Mvvm
layout: ja-docs
---

# CommunityToolkit.Mvvm 連携

> [English version](/docs/communitytoolkit-mvvm/)

CommunityToolkit.Mvvm の `[ObservableProperty]` Generator を使うモデルには、連携パッケージを導入します。

```shell
dotnet add package Jewelry.EditingSystem.CommunityToolkit.Mvvm
```

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using Jewelry.EditingSystem;
using Jewelry.EditingSystem.CommunityToolkit.Mvvm;

[EditingHistory(nameof(_history))]
public sealed partial class DocumentViewModel : ObservableObject
{
    private readonly History _history;

    public DocumentViewModel(History history) => _history = history;

    [Undoable, ObservableProperty]
    private string? name;
}
```

Undo と Redo は CommunityToolkit が生成する setter 経路を再利用し、プロパティ通知、検証、コマンド通知、recipient へのブロードキャストを維持します。`ObservableObject` の継承は必須ではなく、別の基底クラスがある型には `[INotifyPropertyChanged]` を使えます。

この連携では、1 引数版の `OnXChanging(T value)` と `OnXChanged(T value)` を EditingSystem が使用します。アプリケーション固有の処理には 2 引数版の hook を使ってください。`Jewelry.EditingSystem.Annotations` の属性は単体の partial property 用、`Jewelry.EditingSystem.CommunityToolkit.Mvvm` の属性は `[ObservableProperty]` 連携用です。既存の CommunityToolkit.Mvvm コードは変更不要です。
