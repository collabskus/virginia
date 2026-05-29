using Virginia.Data;

namespace Virginia.Services;

/// <summary>
/// Admin-facing user operations. All queries are paged and projected so the
/// admin UI never materializes the entire user table or issues a per-user
/// role lookup (the N+1 pattern that previously collapsed the Blazor circuit
/// at high user counts).
/// </summary>
public interface IUserAdminService
{
    /// <summary>The effective, clamped page size from configuration.</summary>
    int PageSize { get; }

    /// <summary>
    /// One paged, filtered, role-resolved slice of users. Roles are resolved
    /// with a single set-based join, not one query per user.
    /// </summary>
    Task<PagedResult<UserAdminRow>> ListAsync(
        UserAdminFilter filter, int page, int pageSize, CancellationToken ct);

    /// <summary>Fetch a single row (used to refresh one row after an action).</summary>
    Task<UserAdminRow?> GetRowAsync(string userId, CancellationToken ct);

    Task ApproveAsync(string userId, CancellationToken ct);
    Task RevokeAsync(string userId, CancellationToken ct);
    Task PromoteToAdminAsync(string userId, CancellationToken ct);
    Task DeleteAsync(string userId, CancellationToken ct);
}
