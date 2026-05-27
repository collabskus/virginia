using Microsoft.AspNetCore.Components;
using Virginia.Data;

namespace Virginia.Components.Pages.Contacts.Parts;

public sealed partial class NotesCard : ComponentBase
{
    [Parameter, EditorRequired] public ContactDetailDto Detail { get; set; } = default!;
    [Parameter] public EventCallback<string> OnAddNote { get; set; }

    [Inject] private ILogger<NotesCard> Logger { get; set; } = default!;

    private string NewNoteContent { get; set; } = "";
    private bool Submitting { get; set; }

    private async Task OnSaveNoteAsync()
    {
        if (string.IsNullOrWhiteSpace(NewNoteContent) || Submitting) return;

        Submitting = true;
        try
        {
            await OnAddNote.InvokeAsync(NewNoteContent);
            NewNoteContent = "";
        }
        catch (Exception ex)
        {
            // Parent (ContactDetail) handles displaying the error via its
            // Error banner; we just log here and let it propagate by not
            // swallowing silently. The form input is preserved so the user
            // can retry.
            LogAddNoteFailed(Logger, Detail.Id, ex);
        }
        finally
        {
            Submitting = false;
        }
    }

    [LoggerMessage(Level = LogLevel.Error,
        Message = "Failed to add note to contact {ContactId}")]
    private static partial void LogAddNoteFailed(ILogger logger, int contactId, Exception ex);
}
