---
title: Source generation and NativeAOT
---

# Source generation and NativeAOT

> [日本語版](/ja/docs/source-generation-nativeaot/)

The default `[Undoable]` pattern resolves the configured `History`, partial-property implementation, `INotifyPropertyChanged` support, notification method accessibility, and undo/redo setter code at compile time.

Generated properties are NativeAOT-friendly. Their setter and notification code is emitted at compile time, preserving the normal assignment and notification paths in trimming-sensitive applications.

The collection adapter also provides a NativeAOT-safe path when history replays an arbitrary `ICollection<T>` implementation. See [Collections and notifications](collections-and-notifications.md) for its reflection boundary.
