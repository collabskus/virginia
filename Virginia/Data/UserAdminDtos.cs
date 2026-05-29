namespace Virginia.Data;

// ─── Admin user-management projection ────────────────────────────────────────

public sealed record UserAdminRow(
    string Id,
    string Email,
    bool IsAdmin,
    bool IsApproved,
    DateTime CreatedAtUtc)
{
    public string Role => IsAdmin ? "Admin" : "User";
}

// ─── Admin user filter ───────────────────────────────────────────────────────

public sealed record UserAdminFilter(
    string? Email = null,
    bool? IsApproved = null,
    bool? IsAdmin = null);

// ─── Bound from configuration (Admin:UsersPageSize) ──────────────────────────

public sealed class UserAdminOptions
{
    public const string SectionName = "Admin";

    // Default 50. Clamped 1–200 at read time by the service.
    public int UsersPageSize { get; set; } = 50;
}
