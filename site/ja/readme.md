---
title: EditingSystem
layout: simple
og_type: website
---

<div class="text-center py-4 py-md-5">
  <img src="../img/EditingSystem.png" alt="EditingSystem" width="160" height="160" class="rounded-4 mb-3">
  <h1>.NET エディターのための Undo/Redo</h1>
  <p class="lead mx-auto" style="max-width: 48rem;">生成プロパティ、連続操作、Transaction、監視可能コレクションを扱う、NativeAOT フレンドリーな .NET 向け Undo/Redo。</p>
  <p>
    <a class="btn btn-primary btn-lg me-2 mb-2" href="docs/getting-started/"><i class="bi bi-rocket-takeoff me-2" aria-hidden="true"></i>はじめる</a>
    <a class="btn btn-outline-secondary btn-lg me-2 mb-2" href="https://www.nuget.org/packages/Jewelry.EditingSystem"><i class="bi bi-box-seam me-2" aria-hidden="true"></i>NuGet</a>
    <a class="btn btn-outline-secondary btn-lg mb-2" href="https://github.com/YoshihiroIto/EditingSystem"><i class="bi bi-github me-2" aria-hidden="true"></i>GitHub</a>
  </p>
</div>

```shell
dotnet add package Jewelry.EditingSystem
```

<div class="row row-cols-1 row-cols-md-2 g-4 my-2">
  <div class="col">
    <div class="card h-100 bg-transparent border-secondary">
      <div class="card-body">
        <h2 class="h3 card-title"><i class="bi bi-lightning-charge text-info me-2" aria-hidden="true"></i>生成プロパティ</h2>
        <p class="card-text"><code>[Undoable]</code> partial プロパティは、通常編集、Undo、Redo を同じ生成済み setter 経路で処理します。</p>
      </div>
    </div>
  </div>
  <div class="col">
    <div class="card h-100 bg-transparent border-secondary">
      <div class="card-body">
        <h2 class="h3 card-title"><i class="bi bi-sliders text-info me-2" aria-hidden="true"></i>連続編集</h2>
        <p class="card-text"><code>CoalescingBatch()</code> はスライダー、ドラッグ、カラーピッカーを一つの意味のある Undo 操作にまとめます。</p>
      </div>
    </div>
  </div>
  <div class="col">
    <div class="card h-100 bg-transparent border-secondary">
      <div class="card-body">
        <h2 class="h3 card-title"><i class="bi bi-layers text-info me-2" aria-hidden="true"></i>安全な一括編集</h2>
        <p class="card-text"><code>Transaction()</code> は追跡済みの編集をまとめて確定または復元し、<code>Batch()</code> は変更を残したまままとめます。</p>
      </div>
    </div>
  </div>
  <div class="col">
    <div class="card h-100 bg-transparent border-secondary">
      <div class="card-body">
        <h2 class="h3 card-title"><i class="bi bi-collection text-info me-2" aria-hidden="true"></i>コレクションと MVVM</h2>
        <p class="card-text">監視可能コレクションの変更を追跡し、CommunityToolkit.Mvvm の通知、検証、コマンド、メッセージングを維持します。</p>
      </div>
    </div>
  </div>
</div>

## 簡単な例

```csharp
using Jewelry.EditingSystem;
using Jewelry.EditingSystem.Annotations;

[EditingHistory(nameof(history))]
public sealed partial class Document(History history)
{
    [Undoable]
    public partial double X { get; set; }
}

using var history = new History();
var document = new Document(history);
document.X = 100;
history.Undo(); // X == 0
```

## Avalonia デモ

![EditingSystem Avalonia demo](../img/demo00.png)

> [English version](../)
