---
title: EditingSystem
layout: simple
og_type: website
---

<div class="text-center py-4 py-md-5">
  <img src="img/EditingSystem.png" alt="EditingSystem" width="160" height="160" class="rounded-4 mb-3">
  <h1>Undo/redo for modern .NET editors</h1>
  <p class="lead mx-auto" style="max-width: 48rem;">NativeAOT-friendly undo/redo for modern .NET editors: source-generated properties, continuous gestures, transactions, and observable collections.</p>
  <p>
    <a class="btn btn-primary btn-lg me-2 mb-2" href="docs/getting-started/"><i class="bi bi-rocket-takeoff me-2" aria-hidden="true"></i>Get started</a>
    <a class="btn btn-outline-secondary btn-lg me-2 mb-2" href="https://www.nuget.org/packages/Jewelry.EditingSystem"><i class="bi bi-box-seam me-2" aria-hidden="true"></i>NuGet</a>
    <a class="btn btn-outline-secondary btn-lg mb-2" href="https://github.com/YoshihiroIto/EditingSystem"><i class="bi bi-github me-2" aria-hidden="true"></i>GitHub</a>
  </p>
</div>

```shell
dotnet add package Jewelry.EditingSystem
```

<div class="row row-cols-1 row-cols-md-2 g-4 my-2 editing-feature-grid">
  <div class="col">
    <div class="card h-100 editing-feature-card">
      <div class="card-header">
        <i class="bi bi-lightning-charge" aria-hidden="true"></i>
        <h2>Generated properties</h2>
      </div>
      <div class="card-body">
        <p class="card-text"><code>[Undoable]</code> partial properties record normal edits, Undo, and Redo through the same generated setter path.</p>
      </div>
    </div>
  </div>
  <div class="col">
    <div class="card h-100 editing-feature-card">
      <div class="card-header">
        <i class="bi bi-sliders" aria-hidden="true"></i>
        <h2>Continuous edits</h2>
      </div>
      <div class="card-body">
        <p class="card-text"><code>CoalescingBatch()</code> reduces a slider, drag, or color-picker gesture to one meaningful Undo step.</p>
      </div>
    </div>
  </div>
  <div class="col">
    <div class="card h-100 editing-feature-card">
      <div class="card-header">
        <i class="bi bi-layers" aria-hidden="true"></i>
        <h2>Safe grouped edits</h2>
      </div>
      <div class="card-body">
        <p class="card-text"><code>Transaction()</code> commits or restores tracked edits as a unit; <code>Batch()</code> groups changes that should remain applied.</p>
      </div>
    </div>
  </div>
  <div class="col">
    <div class="card h-100 editing-feature-card">
      <div class="card-header">
        <i class="bi bi-collection" aria-hidden="true"></i>
        <h2>Collections and MVVM</h2>
      </div>
      <div class="card-body">
        <p class="card-text">Track observable collection changes and preserve CommunityToolkit.Mvvm notifications, validation, commands, and messaging.</p>
      </div>
    </div>
  </div>
</div>

<div class="editing-example-card">
  <div class="editing-feature-card card">
    <div class="card-header">
      <i class="bi bi-code-slash" aria-hidden="true"></i>
      <h2>Quick example</h2>
    </div>
    <div class="card-body">

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

</div>
</div>
</div>

<div class="editing-example-card">
  <div class="editing-feature-card card">
    <div class="card-header">
      <i class="bi bi-window-desktop" aria-hidden="true"></i>
      <h2>Avalonia demo</h2>
    </div>
    <div class="card-body">

![EditingSystem Avalonia demo](img/demo00.png)

</div>
</div>
</div>

> [日本語版はこちら](ja/)
