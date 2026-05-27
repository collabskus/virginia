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
        CancellationToken ct = default, Guid? originId = null);

    Task DeleteAsync(
        int id, CancellationToken ct = default, Guid? originId = null);

    Task SetProfilePictureAsync(
        int id, byte[] data, string contentType,
        CancellationToken ct = default, Guid? originId = null);

    Task<ProfilePictureResult?> GetProfilePictureAsync(int id, CancellationToken ct = default);

    Task RemoveProfilePictureAsync(
        int id, CancellationToken ct = default, Guid? originId = null);

    Task<int> AddNoteAsync(
        int contactId, string content, string userId, string userName,
        CancellationToken ct = default, Guid? originId = null);

    Task<int> CreateBulkAsync(int count, CancellationToken ct = default);

    Task<int> DeleteAllAsync(CancellationToken ct = default);
}
