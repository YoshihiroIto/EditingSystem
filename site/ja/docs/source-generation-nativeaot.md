---
title: Source Generator と NativeAOT
layout: ja-docs
---

# Source Generator と NativeAOT

> [English version](/docs/source-generation-nativeaot/)

既定の `[Undoable]` パターンでは、指定された `History`、partial プロパティ実装、`INotifyPropertyChanged` 対応、通知メソッドのアクセシビリティ、Undo/Redo setter コードをコンパイル時に解決します。

生成プロパティは NativeAOT フレンドリーです。setter と通知コードをコンパイル時に生成するため、通常の代入・通知経路を維持したまま、トリミングに配慮が必要なアプリケーションでも使えます。

任意の `ICollection<T>` 実装を履歴で再生するコレクションアダプターにも NativeAOT 対応の経路があります。リフレクションの適用範囲は [コレクションと通知](collections-and-notifications.md) を参照してください。
