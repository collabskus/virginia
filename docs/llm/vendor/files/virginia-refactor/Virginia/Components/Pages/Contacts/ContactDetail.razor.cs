using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Virginia.Data;
using Virginia.Services;

namespace Virginia.Components.Pages.Contacts;

public sealed partial class ContactDetail : ComponentBase, IDisposable
{
    [Parameter] public int? Id { get; set; }

    [Inject] private IContactService ContactService { get; set; } = default!;
    [Inject] private IContactChangeNotifier Notifier { get; set; } = default!;
    [Inject] private IToastService Toasts { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] private ILogger<ContactDetail> Logger { get; set; } = default!;

    private bool IsNew => Id is null;
    private ContactFormModel Model { get; set; } = new();
    private EditContext EditContext { get; set; } = null!;
    private ContactDetailDto? Detail { get; set; }
    private bool Loading { get; set; } = true;
    private bool NotFound { get; set; }
    private bool Saving { get; set; }
    private bool Saved { get; set; }
    private string? Error { get; set; }
    private int PhotoVer { get; set; } = 1;
    private bool ConfirmingDelete { get; set; }
    private bool DeletedByOther { get; set; }

    // Per-circuit identifier so we can ignore real-time echoes of our own writes.
    private readonly Guid _originId = Guid.NewGuid();

    protected override async Task OnInitializedAsync()
    {
        EditContext = new EditContext(Model);

        if (!IsNew)
        {
            Detail = await ContactService.GetAsync(Id!.Value);
            if (Detail is null)
            {
                NotFound = true;
                Loading = false;
                return;
            }
            Model = ContactFormModel.FromDetail(Detail);
            EditContext = new EditContext(Model);

            Notifier.Changed += OnContactChanged;
        }

        Loading = false;
    }

    public void Dispose()
    {
        Notifier.Changed -= OnContactChanged;
    }

    // ─── Real-time updates ──────────────────────────────────────────────

    private void OnContactChanged(ContactChangeEvent evt)
    {
        if (IsNew || evt.ContactId != Id!.Value) return;
        if (evt.OriginId == _originId) return;

        _ = InvokeAsync(() => HandleChangeAsync(evt));
    }

    private async Task HandleChangeAsync(ContactChangeEvent evt)
    {
        switch (evt.Kind)
        {
            case ContactChangeKind.NoteAdded when evt.Note is not null:
                if (Detail is not null)
                {
                    var newNotes = new List<NoteDto>(Detail.Notes.Count + 1) { evt.Note };
                    newNotes.AddRange(Detail.Notes);
                    Detail = Detail with { Notes = newNotes };
                }
                Toasts.ShowInfo($"{evt.Note.CreatedByUserName} added a note.");
                break;

            case ContactChangeKind.PhotoChanged:
                Detail = await ContactService.GetAsync(Id!.Value);
                if (Detail is null) { DeletedByOther = true; break; }
                PhotoVer++;
                Toasts.ShowInfo("Profile picture was updated.");
                break;

            case ContactChangeKind.Updated:
                if (EditContext.IsModified())
                {
                    Toasts.ShowReloadWarning(
                        "Another user changed this contact. Reload to see their changes, or save to overwrite.",
                        onReload: () => _ = ReloadFromServerAsync());
                }
                else
                {
                    await ReloadFromServerAsync();
                    Toasts.ShowInfo("This contact was updated by another user.");
                }
                break;

            case ContactChangeKind.Deleted:
                DeletedByOther = true;
                break;
        }

        StateHasChanged();
    }

    private async Task ReloadFromServerAsync()
    {
        if (IsNew) return;

        var fresh = await ContactService.GetAsync(Id!.Value);
        if (fresh is null)
        {
            DeletedByOther = true;
            StateHasChanged();
            return;
        }

        Detail = fresh;
        Model = ContactFormModel.FromDetail(fresh);
        EditContext = new EditContext(Model);
        PhotoVer++;

        Toasts.DismissReloadToasts();
        StateHasChanged();
    }

    // ─── Save / Delete ──────────────────────────────────────────────────

    private async Task SaveAsync()
    {
        Saving = true;
        Saved = false;
        Error = null;

        try
        {
            if (IsNew)
            {
                var newId = await ContactService.CreateAsync(Model);
                LogContactCreated(Logger, newId);
                Nav.NavigateTo($"/contacts/{newId}");
            }
            else
            {
                await ContactService.UpdateAsync(Id!.Value, Model, originId: _originId);
                Detail = await ContactService.GetAsync(Id!.Value);
                Saved = true;
                Toasts.DismissReloadToasts();
                EditContext = new EditContext(Model);
                LogContactUpdated(Logger, Id!.Value);
            }
        }
        catch (Exception ex)
        {
            Error = $"Save failed: {ex.Message}";
            LogSaveFailed(Logger, Id, ex);
        }
        finally
        {
            Saving = false;
        }
    }

    private void StartDeleteConfirm() => ConfirmingDelete = true;
    private void CancelDeleteConfirm() => ConfirmingDelete = false;

    private async Task DeleteAsync()
    {
        Saving = true;
        Error = null;

        try
        {
            await ContactService.DeleteAsync(Id!.Value, originId: _originId);
            LogContactDeleted(Logger, Id!.Value);
            Nav.NavigateTo("/");
        }
        catch (Exception ex)
        {
            Error = $"Delete failed: {ex.Message}";
            LogDeleteFailed(Logger, Id!.Value, ex);
            Saving = false;
        }
    }

    // ─── Profile picture ────────────────────────────────────────────────

    private async Task OnPhotoUploadAsync((byte[] Data, string ContentType) upload)
    {
        Error = null;
        try
        {
            await ContactService.SetProfilePictureAsync(
                Id!.Value, upload.Data, upload.ContentType, originId: _originId);
            Detail = await ContactService.GetAsync(Id!.Value);
            PhotoVer++;
        }
        catch (Exception ex)
        {
            Error = $"Upload failed: {ex.Message}";
            LogPhotoSetFailed(Logger, Id!.Value, ex);
        }
    }

    private async Task OnPhotoRemoveAsync()
    {
        Error = null;
        try
        {
            await ContactService.RemoveProfilePictureAsync(Id!.Value, originId: _originId);
            Detail = await ContactService.GetAsync(Id!.Value);
            PhotoVer++;
        }
        catch (Exception ex)
        {
            Error = $"Remove failed: {ex.Message}";
            LogPhotoRemoveFailed(Logger, Id!.Value, ex);
        }
    }

    // ─── Notes ──────────────────────────────────────────────────────────

    private async Task OnAddNoteAsync(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return;
        Error = null;

        try
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            var userId = authState.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
            var userName = authState.User.Identity?.Name ?? "unknown";

            await ContactService.AddNoteAsync(
                Id!.Value, content, userId, userName, originId: _originId);
            Detail = await ContactService.GetAsync(Id!.Value);
        }
        catch (Exception ex)
        {
            Error = $"Failed to add note: {ex.Message}";
            LogNoteAddFailed(Logger, Id!.Value, ex);
        }
    }

    private void GoBack() => Nav.NavigateTo("/");

    // ─── Logging ────────────────────────────────────────────────────────

    [LoggerMessage(Level = LogLevel.Information, Message = "Created contact {ContactId}")]
    private static partial void LogContactCreated(ILogger logger, int contactId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Updated contact {ContactId}")]
    private static partial void LogContactUpdated(ILogger logger, int contactId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Deleted contact {ContactId}")]
    private static partial void LogContactDeleted(ILogger logger, int contactId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to save contact {ContactId}")]
    private static partial void LogSaveFailed(ILogger logger, int? contactId, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to delete contact {ContactId}")]
    private static partial void LogDeleteFailed(ILogger logger, int contactId, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to set photo for contact {ContactId}")]
    private static partial void LogPhotoSetFailed(ILogger logger, int contactId, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to remove photo for contact {ContactId}")]
    private static partial void LogPhotoRemoveFailed(ILogger logger, int contactId, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to add note to contact {ContactId}")]
    private static partial void LogNoteAddFailed(ILogger logger, int contactId, Exception ex);
}
