namespace Virginia.Services;

public sealed partial class ToastService : IToastService, IDisposable
{
    private readonly List<Toast> _toasts = [];
    private readonly Lock _gate = new();
    private readonly ILogger<ToastService> _logger;
    private int _nextId = 1;
    private bool _disposed;

    public ToastService(ILogger<ToastService> logger)
    {
        _logger = logger;
    }

    public IReadOnlyList<Toast> Toasts
    {
        get
        {
            lock (_gate)
            {
                // Snapshot to avoid race between background dismiss and the
                // renderer iterating.
                return _toasts.ToArray();
            }
        }
    }

    public event Action? Changed;

    public void ShowInfo(string message)
    {
        var toast = Add(message, ToastSeverity.Info, onReload: null);
        ScheduleAutoDismiss(toast.Id, TimeSpan.FromSeconds(5));
    }

    public void ShowSuccess(string message)
    {
        var toast = Add(message, ToastSeverity.Success, onReload: null);
        ScheduleAutoDismiss(toast.Id, TimeSpan.FromSeconds(4));
    }

    public void ShowError(string message) =>
        Add(message, ToastSeverity.Error, onReload: null);

    public void ShowReloadWarning(string message, Action onReload)
    {
        lock (_gate)
        {
            if (_toasts.Any(t => t.HasReload))
            {
                LogReloadToastSuppressed(_logger, message);
                return;
            }
            var toast = new Toast(_nextId++, message, ToastSeverity.Warn, onReload);
            _toasts.Add(toast);
        }
        Changed?.Invoke();
    }

    public void Dismiss(int id)
    {
        bool changed;
        lock (_gate)
        {
            changed = _toasts.RemoveAll(t => t.Id == id) > 0;
        }
        if (changed) Changed?.Invoke();
    }

    public void DismissReloadToasts()
    {
        bool changed;
        lock (_gate)
        {
            changed = _toasts.RemoveAll(t => t.HasReload) > 0;
        }
        if (changed) Changed?.Invoke();
    }

    private Toast Add(string message, ToastSeverity severity, Action? onReload)
    {
        Toast toast;
        lock (_gate)
        {
            toast = new Toast(_nextId++, message, severity, onReload);
            _toasts.Add(toast);
        }
        Changed?.Invoke();
        return toast;
    }

    private void ScheduleAutoDismiss(int id, TimeSpan after)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(after).ConfigureAwait(false);
                if (_disposed) return;
                Dismiss(id);
            }
            catch (Exception ex)
            {
                LogAutoDismissFailed(_logger, id, ex);
            }
        });
    }

    public void Dispose()
    {
        _disposed = true;
        Changed = null;
    }

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "Suppressed duplicate reload-toast: {Message}")]
    private static partial void LogReloadToastSuppressed(ILogger logger, string message);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Auto-dismiss for toast {ToastId} threw")]
    private static partial void LogAutoDismissFailed(ILogger logger, int toastId, Exception ex);
}
