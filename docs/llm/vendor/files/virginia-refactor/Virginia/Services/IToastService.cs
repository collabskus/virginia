namespace Virginia.Services;

/// <summary>
/// Per-circuit (scoped) toast queue. Components on the page call
/// <see cref="Show"/> / <see cref="ShowWithReload"/> / <see cref="Dismiss"/>;
/// the shared <c>ToastGroup</c> component subscribes to <see cref="Changed"/>
/// and re-renders.
/// </summary>
public interface IToastService
{
    IReadOnlyList<Toast> Toasts { get; }
    event Action? Changed;

    /// <summary>Adds an auto-dismissing informational toast.</summary>
    void ShowInfo(string message);

    /// <summary>Adds an auto-dismissing success toast.</summary>
    void ShowSuccess(string message);

    /// <summary>
    /// Adds a sticky warning toast with an inline "Reload" action.
    /// Only one reload-toast can be active at a time; calling this when
    /// one already exists is a no-op.
    /// </summary>
    void ShowReloadWarning(string message, Action onReload);

    /// <summary>Adds a sticky error toast (manual dismiss only).</summary>
    void ShowError(string message);

    void Dismiss(int id);

    /// <summary>Removes all reload-action toasts (e.g. after a successful reload).</summary>
    void DismissReloadToasts();
}

public enum ToastSeverity { Info, Success, Warn, Error }

public sealed record Toast(
    int Id,
    string Message,
    ToastSeverity Severity,
    Action? OnReload)
{
    public bool HasReload => OnReload is not null;
    public string SeverityClass => Severity switch
    {
        ToastSeverity.Info => "info",
        ToastSeverity.Success => "success",
        ToastSeverity.Warn => "warn",
        ToastSeverity.Error => "error",
        _ => "info"
    };

    public string SeverityLabel => Severity switch
    {
        ToastSeverity.Info => "Information",
        ToastSeverity.Success => "Success",
        ToastSeverity.Warn => "Warning",
        ToastSeverity.Error => "Error",
        _ => "Information"
    };
}
