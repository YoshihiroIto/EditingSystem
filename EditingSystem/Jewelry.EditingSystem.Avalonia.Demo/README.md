# Jewelry.EditingSystem Avalonia Demo

Small 2D-editor-style demo for `Jewelry.EditingSystem`.

## Features

- Undo / Redo and live undo/redo counts
- Add objects and undo collection changes
- Ctrl+Click multi-selection
- Drag any selected object to move the whole selection
- Resize a selected object from eight edge/corner handles
- Edit opacity with a Slider
- Edit color with Avalonia `ColorView`
- `Batch()` examples: align multiple objects and delete multiple objects
- `CoalescingBatch()` examples: move, resize, opacity, and color drag operations

## Run

```shell
dotnet run --project Jewelry.EditingSystem.Avalonia.Demo/Jewelry.EditingSystem.Avalonia.Demo.csproj
```

The demo intentionally keeps interaction code in `MainWindow.axaml.cs` and undo-aware state in the ViewModel/model. The point is to show that ordinary Avalonia pointer handling can be wrapped by only `BeginCoalescingBatch()` / `EndCoalescingBatch()` while `[Undoable]` properties remain normal CommunityToolkit.Mvvm observable properties.
