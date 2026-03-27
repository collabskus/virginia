namespace Virginia.Data;

// ─── List projection ─────────────────────────────────────────────────────────

public sealed record ContactListItem(
    int Id,
    string FirstName,
    string LastName,
    bool HasPhoto,
    string? PrimaryEmail,
    string? PrimaryPhone,
    string? PrimaryCity,
    DateTime CreatedAtUtc);

// ─── Detail projection ───────────────────────────────────────────────────────

public sealed record ContactDetailDto(
    int Id,
    string FirstName,
    string LastName,
    bool HasPhoto,
    string? ProfilePictureContentType,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    List<EmailDto> Emails,
    List<PhoneDto> Phones,
    List<AddressDto> Addresses,
    List<NoteDto> Notes);

public sealed record EmailDto(int Id, string Label, string Address);
public sealed record PhoneDto(int Id, string Label, string Number);

public sealed record AddressDto(
    int Id, string Label, string Street,
    string City, string State, string PostalCode, string Country);

public sealed record NoteDto(
    int Id, string Content, string CreatedByUserName, DateTime CreatedAtUtc);

// ─── Paging ──────────────────────────────────────────────────────────────────

public sealed record PagedResult<T>(
    List<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}

// ─── Filter ──────────────────────────────────────────────────────────────────

public sealed record ContactFilter(
    string? Name = null,
    string? Email = null,
    string? Phone = null,
    string? City = null,
    string? State = null,
    bool? HasPhoto = null);

// ─── Profile picture result ──────────────────────────────────────────────────

public sealed record ProfilePictureResult(byte[] Data, string ContentType);
