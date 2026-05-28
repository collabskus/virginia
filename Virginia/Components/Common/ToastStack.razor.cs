using Microsoft.AspNetCore.Components;
using Virginia.Services;

namespace Virginia.Components.Common;

public sealed partial class ToastGroup : ComponentBase, IDisposable
{
    [Inject] private IToastService Toasts { get; set; } = default!;

    protected override void OnInitialized()
    {
        Toasts.Changed += OnToastsChanged;
    }

    private void OnToastsChanged() => InvokeAsync(StateHasChanged);

    private void InvokeReload(Toast toast)
    {
        toast.OnReload?.Invoke();
        Toasts.Dismiss(toast.Id);
    }

    private static string RoleFor(ToastSeverity severity) => severity switch
    {
        ToastSeverity.Error => "alert",
        ToastSeverity.Warn => "alert",
        _ => "status"
    };

    private static string LiveFor(ToastSeverity severity) => severity switch
    {
        ToastSeverity.Error => "assertive",
        ToastSeverity.Warn => "assertive",
        _ => "polite"
    };

    public void Dispose()
    {
        Toasts.Changed -= OnToastsChanged;
    }
}
