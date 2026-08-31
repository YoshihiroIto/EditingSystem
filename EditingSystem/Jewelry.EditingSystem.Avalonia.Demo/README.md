# Jewelry.EditingSystem Avalonia Demo

Small 2D-editor-style demo for `Jewelry.EditingSystem`.

## Features

- Undo / Redo and live undo/redo counts
- Save-point-based dirty state with a title `*`, `Saved` / `Unsaved changes`, and `Mark Saved`
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

`Mark Saved` simulates a successful save by calling `History.MarkSaved()`; the demo does not write a file. Edit an object to make the title and toolbar show the unsaved state, mark that position as saved, edit again, and use Undo/Redo to see `History.IsDirty` change automatically when the current history position crosses the save point.
