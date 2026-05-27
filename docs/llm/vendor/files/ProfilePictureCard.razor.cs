using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Virginia.Data;

namespace Virginia.Components.Pages.Contacts.Parts;

public sealed partial class ProfilePictureCard : ComponentBase
{
    private const int MaxBytes = 2 * 1024 * 1024;
    private static readonly string[] AllowedContentTypes =
        ["image/jpeg", "image/png", "image/webp"];

    private readonly string _idSuffix = Guid.NewGuid().ToString("N")[..8];

    [Parameter, EditorRequired] public int ContactId { get; set; }
    [Parameter] public ContactDetailDto? Detail { get; set; }
    [Parameter] public int PhotoVersion { get; set; }
    [Parameter] public bool Saving { get; set; }
    [Parameter] public EventCallback<(byte[] Data, string ContentType)> OnUpload { get; set; }
    [Parameter] public EventCallback OnRemove { get; set; }

    [Inject] private ILogger<ProfilePictureCard> Logger { get; set; } = default!;

    private string HeadingId => $"profile-pic-heading-{_idSuffix}";
    private string UploadId => $"profile-pic-upload-{_idSuffix}";
    private string? LocalError { get; set; }

    private async Task OnFileSelectedAsync(InputFileChangeEventArgs e)
    {
        LocalError = null;
        var file = e.File;

        if (file.Size > MaxBytes)
        {
            LocalError = "Image must be under 2 MB.";
            return;
        }

        if (!AllowedContentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            LocalError = "Only JPEG, PNG, and WebP images are supported.";
            return;
        }

        try
        {
            using var stream = file.OpenReadStream(MaxBytes);
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            await OnUpload.InvokeAsync((ms.ToArray(), file.ContentType));
        }
        catch (Exception ex)
        {
            LocalError = $"Upload failed: {ex.Message}";
            LogPhotoUploadFailed(Logger, ContactId, ex);
        }
    }

    private async Task OnRemoveClicked() => await OnRemove.InvokeAsync();

    [LoggerMessage(Level = LogLevel.Error,
        Message = "Photo upload failed for contact {ContactId}")]
    private static partial void LogPhotoUploadFailed(ILogger logger, int contactId, Exception ex);
}
