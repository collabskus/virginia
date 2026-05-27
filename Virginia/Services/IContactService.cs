using Virginia.Data;

namespace Virginia.Services;

public interface IContactService
{
    Task<PagedResult<ContactListItem>> ListAsync(
        ContactFilter filter, int page, int pageSize, CancellationToken ct = default);

    Task<ContactDetailDto?> GetAsync(int id, CancellationToken ct = default);

    Task<int> CreateAsync(ContactFormModel form, CancellationToken ct = default);

    Task UpdateAsync(
        int id, ContactFormModel form,
        Guid? originId = null,
        CancellationToken ct = default);

    Task DeleteAsync(
        int id, Guid? originId = null,
        CancellationToken ct = default);

    Task SetProfilePictureAsync(
        int id, byte[] data, string contentType,
        Guid? originId = null,
        CancellationToken ct = default);

    Task<ProfilePictureResult?> GetProfilePictureAsync(int id, CancellationToken ct = default);

    Task RemoveProfilePictureAsync(
        int id, Guid? originId = null,
        CancellationToken ct = default);

    Task<int> AddNoteAsync(
        int contactId, string content, string userId, string userName,
        Guid? originId = null,
        CancellationToken ct = default);

    Task<int> CreateBulkAsync(int count, CancellationToken ct = default);

    Task<int> DeleteAllAsync(CancellationToken ct = default);
}
