using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;

namespace Jewelry.EditingSystem.Avalonia.Demo;

public sealed class BrowserUndoRedoBindings : IDisposable
{
    private readonly MainView _mainView;
    private readonly EventHandler<VisualTreeAttachmentEventArgs> _attachedHandler;
    private readonly EventHandler<VisualTreeAttachmentEventArgs> _detachedHandler;
    private KeyBinding? _undoBinding;
    private KeyBinding? _redoBinding;
    private TopLevel? _topLevel;
    private bool _isDisposed;

    public BrowserUndoRedoBindings(MainView mainView)
    {
        _mainView = mainView;
        _attachedHandler = (_, _) => Dispatcher.UIThread.Post(AttachBindings, DispatcherPriority.Loaded);
        _detachedHandler = (_, _) => DetachBindings();

        mainView.AttachedToVisualTree += _attachedHandler;
        mainView.DetachedFromVisualTree += _detachedHandler;
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        _mainView.AttachedToVisualTree -= _attachedHandler;
        _mainView.DetachedFromVisualTree -= _detachedHandler;
        DetachBindings();
    }

    private void AttachBindings()
    {
        if (_isDisposed ||
            TopLevel.GetTopLevel(_mainView) is not { } topLevel ||
            _mainView.DataContext is not MainViewViewModel viewModel)
            return;

        if (ReferenceEquals(_topLevel, topLevel))
            return;

        DetachBindings();

        _undoBinding ??= new KeyBinding { Gesture = new KeyGesture(Key.Z, KeyModifiers.Control) };
        _redoBinding ??= new KeyBinding { Gesture = new KeyGesture(Key.Y, KeyModifiers.Control) };
        _undoBinding.Command = viewModel.UndoCommand;
        _redoBinding.Command = viewModel.RedoCommand;

        _topLevel = topLevel;
        topLevel.KeyBindings.Add(_undoBinding);
        topLevel.KeyBindings.Add(_redoBinding);
        _mainView.Focus();
    }

    private void DetachBindings()
    {
        if (_topLevel is null)
            return;

        if (_undoBinding is { } undoBinding)
            _topLevel.KeyBindings.Remove(undoBinding);
        if (_redoBinding is { } redoBinding)
            _topLevel.KeyBindings.Remove(redoBinding);

        _topLevel = null;
    }
}
