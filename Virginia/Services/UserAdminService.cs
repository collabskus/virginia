using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Virginia.Data;

namespace Virginia.Services;

public sealed partial class UserAdminService(
    AppDbContext db,
    UserManager<AppUser> userManager,
    IOptions<UserAdminOptions> options,
    ILogger<UserAdminService> logger) : IUserAdminService
{
    private const string AdminRole = "Admin";
    private const string UserRole = "User";
    private const int MaxPageSize = 200;

    // Clamp the configured page size into a sane band once, at construction.
    public int PageSize { get; } = Math.Clamp(options.Value.UsersPageSize, 1, MaxPageSize);

    // ── Read ──────────────────────────────────────────────────────────────

    public async Task<PagedResult<UserAdminRow>> ListAsync(
        UserAdminFilter filter, int page, int pageSize, CancellationToken ct)
    {
        var size = Math.Clamp(pageSize, 1, MaxPageSize);
        var pageNo = Math.Max(page, 1);

        // The set of user ids that hold the Admin role, expressed as an
        // IQueryable subquery so it executes server-side (a single SQL
        // statement), not one GetRolesAsync call per user.
        var adminUserIds =
            from ur in db.UserRoles
            join r in db.Roles on ur.RoleId equals r.Id
            where r.Name == AdminRole
            select ur.UserId;

        // Filter the ENTITY query first; project last (mirrors ContactService).
        var query = userManager.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Email))
        {
            var pattern = $"%{filter.Email.Trim()}%";
            // EF.Functions.Like is case-insensitive on SQLite (same approach
            // already used for contact filters).
            query = query.Where(u => u.Email != null && EF.Functions.Like(u.Email, pattern));
        }

        if (filter.IsApproved is { } approved)
            query = query.Where(u => u.IsApproved == approved);

        if (filter.IsAdmin is { } isAdmin)
        {
            query = isAdmin
                ? query.Where(u => adminUserIds.Contains(u.Id))
                : query.Where(u => !adminUserIds.Contains(u.Id));
        }

        var totalCount = await query.CountAsync(ct);

        // Stable ordering: pending first (so admins act on them), then email.
        var items = await query
            .OrderBy(u => u.IsApproved)
            .ThenBy(u => u.Email)
            .Skip((pageNo - 1) * size)
            .Take(size)
            .Select(u => new UserAdminRow(
                u.Id,
                u.Email ?? "",
                adminUserIds.Contains(u.Id),
                u.IsApproved,
                u.CreatedAtUtc))
            .ToListAsync(ct);

        Log.ListedUsers(logger, items.Count, totalCount, pageNo);

        return new PagedResult<UserAdminRow>(items, totalCount, pageNo, size);
    }

    public async Task<UserAdminRow?> GetRowAsync(string userId, CancellationToken ct)
    {
        var adminUserIds =
            from ur in db.UserRoles
            join r in db.Roles on ur.RoleId equals r.Id
            where r.Name == AdminRole
            select ur.UserId;

        return await userManager.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new UserAdminRow(
                u.Id,
                u.Email ?? "",
                adminUserIds.Contains(u.Id),
                u.IsApproved,
                u.CreatedAtUtc))
            .FirstOrDefaultAsync(ct);
    }

    // ── Mutations ───────────────────────────────────────────────────────────

    public async Task ApproveAsync(string userId, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null || user.IsApproved) return;

        user.IsApproved = true;
        await userManager.UpdateAsync(user);
        Log.Approved(logger, user.Email ?? userId);
    }

    public async Task RevokeAsync(string userId, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null || !user.IsApproved) return;

        user.IsApproved = false;
        await userManager.UpdateAsync(user);
        Log.Revoked(logger, user.Email ?? userId);
    }

    public async Task PromoteToAdminAsync(string userId, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return;

        if (await userManager.IsInRoleAsync(user, UserRole))
            await userManager.RemoveFromRoleAsync(user, UserRole);

        if (!await userManager.IsInRoleAsync(user, AdminRole))
            await userManager.AddToRoleAsync(user, AdminRole);

        user.IsApproved = true;
        await userManager.UpdateAsync(user);
        Log.Promoted(logger, user.Email ?? userId);
    }

    public async Task DeleteAsync(string userId, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return;

        var email = user.Email ?? userId;
        await userManager.DeleteAsync(user);
        Log.Deleted(logger, email);
    }

    // ── Source-generated logging (CA1848/CA1873) ────────────────────────────

    private static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Information,
            Message = "Listed {Count}/{Total} users (page {Page})")]
        public static partial void ListedUsers(
            ILogger logger, int count, int total, int page);

        [LoggerMessage(Level = LogLevel.Information, Message = "Approved user {Email}")]
        public static partial void Approved(ILogger logger, string email);

        [LoggerMessage(Level = LogLevel.Information,
            Message = "Revoked approval for user {Email}")]
        public static partial void Revoked(ILogger logger, string email);

        [LoggerMessage(Level = LogLevel.Information, Message = "Promoted user {Email} to Admin")]
        public static partial void Promoted(ILogger logger, string email);

        [LoggerMessage(Level = LogLevel.Information, Message = "Deleted user {Email}")]
        public static partial void Deleted(ILogger logger, string email);
    }
}
