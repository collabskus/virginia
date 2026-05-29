using Virginia.Data;
using Xunit;

namespace Virginia.Tests;

public sealed class UserAdminServiceTests
{
    private static CancellationToken CT => TestContext.Current.CancellationToken;

    // ─── List / paging ───────────────────────────────────────────────────

    [Fact]
    public async Task List_Empty_ReturnsZero()
    {
        await using var h = await UserAdminTestHarness.CreateAsync();

        var page = await h.Service.ListAsync(new UserAdminFilter(), 1, 50, CT);

        Assert.Equal(0, page.TotalCount);
        Assert.Empty(page.Items);
        Assert.Equal(0, page.TotalPages);
    }

    [Fact]
    public async Task List_RespectsPageSize()
    {
        await using var h = await UserAdminTestHarness.CreateAsync();
        await h.SeedUsersAsync(120);

        var page = await h.Service.ListAsync(new UserAdminFilter(), 1, 50, CT);

        Assert.Equal(120, page.TotalCount);
        Assert.Equal(50, page.Items.Count);
        Assert.Equal(3, page.TotalPages);
        Assert.True(page.HasNext);
        Assert.False(page.HasPrevious);
    }

    [Fact]
    public async Task List_SecondPage_ReturnsNextSlice()
    {
        await using var h = await UserAdminTestHarness.CreateAsync();
        await h.SeedUsersAsync(120);

        var p1 = await h.Service.ListAsync(new UserAdminFilter(), 1, 50, CT);
        var p2 = await h.Service.ListAsync(new UserAdminFilter(), 2, 50, CT);

        Assert.Equal(50, p2.Items.Count);
        Assert.True(p2.HasPrevious);
        var overlap = p1.Items.Select(i => i.Id).Intersect(p2.Items.Select(i => i.Id));
        Assert.Empty(overlap);
    }

    [Fact]
    public async Task List_LastPage_MayBePartial()
    {
        await using var h = await UserAdminTestHarness.CreateAsync();
        await h.SeedUsersAsync(120);

        var p3 = await h.Service.ListAsync(new UserAdminFilter(), 3, 50, CT);

        Assert.Equal(20, p3.Items.Count);
        Assert.False(p3.HasNext);
    }

    [Fact]
    public async Task List_PageSizeClampedToCeiling()
    {
        await using var h = await UserAdminTestHarness.CreateAsync();
        await h.SeedUsersAsync(10);

        // Request an absurd page size; service clamps to 200.
        var page = await h.Service.ListAsync(new UserAdminFilter(), 1, 99999, CT);

        Assert.Equal(200, page.PageSize);
        Assert.Equal(10, page.Items.Count);
    }

    // ─── Filtering ───────────────────────────────────────────────────────

    [Fact]
    public async Task List_FilterByEmail_IsCaseInsensitive()
    {
        await using var h = await UserAdminTestHarness.CreateAsync();
        await h.CreateUserAsync("Alice@example.com", approved: true);
        await h.CreateUserAsync("bob@example.com", approved: true);

        var page = await h.Service.ListAsync(
            new UserAdminFilter(Email: "ALICE"), 1, 50, CT);

        Assert.Equal(1, page.TotalCount);
        Assert.Equal("Alice@example.com", page.Items[0].Email);
    }

    [Fact]
    public async Task List_FilterByPending_ExcludesApproved()
    {
        await using var h = await UserAdminTestHarness.CreateAsync();
        await h.CreateUserAsync("pending@example.com", approved: false);
        await h.CreateUserAsync("approved@example.com", approved: true);

        var page = await h.Service.ListAsync(
            new UserAdminFilter(IsApproved: false), 1, 50, CT);

        Assert.Equal(1, page.TotalCount);
        Assert.Equal("pending@example.com", page.Items[0].Email);
        Assert.False(page.Items[0].IsApproved);
    }

    [Fact]
    public async Task List_PendingOrderedBeforeApproved()
    {
        await using var h = await UserAdminTestHarness.CreateAsync();
        await h.CreateUserAsync("zapproved@example.com", approved: true);
        await h.CreateUserAsync("apending@example.com", approved: false);

        var page = await h.Service.ListAsync(new UserAdminFilter(), 1, 50, CT);

        // Pending first regardless of email sort.
        Assert.False(page.Items[0].IsApproved);
    }

    // ─── Role resolution (the former N+1) ────────────────────────────────

    [Fact]
    public async Task List_ResolvesAdminFlag_WithoutPerUserQuery()
    {
        await using var h = await UserAdminTestHarness.CreateAsync();
        var adminId = await h.CreateUserAsync("admin2@example.com", approved: true);
        await h.AddToAdminRoleAsync(adminId);
        await h.CreateUserAsync("plain@example.com", approved: true);

        var page = await h.Service.ListAsync(new UserAdminFilter(), 1, 50, CT);

        var admin = page.Items.Single(i => i.Email == "admin2@example.com");
        var plain = page.Items.Single(i => i.Email == "plain@example.com");
        Assert.True(admin.IsAdmin);
        Assert.Equal("Admin", admin.Role);
        Assert.False(plain.IsAdmin);
        Assert.Equal("User", plain.Role);
    }

    [Fact]
    public async Task List_FilterByAdmin_ReturnsOnlyAdmins()
    {
        await using var h = await UserAdminTestHarness.CreateAsync();
        var adminId = await h.CreateUserAsync("theadmin@example.com", approved: true);
        await h.AddToAdminRoleAsync(adminId);
        await h.CreateUserAsync("nonadmin@example.com", approved: true);

        var page = await h.Service.ListAsync(
            new UserAdminFilter(IsAdmin: true), 1, 50, CT);

        Assert.Equal(1, page.TotalCount);
        Assert.True(page.Items[0].IsAdmin);
    }

    // ─── Mutations ───────────────────────────────────────────────────────

    [Fact]
    public async Task Approve_SetsApprovedTrue()
    {
        await using var h = await UserAdminTestHarness.CreateAsync();
        var id = await h.CreateUserAsync("toapprove@example.com", approved: false);

        await h.Service.ApproveAsync(id, CT);
        var row = await h.Service.GetRowAsync(id, CT);

        Assert.NotNull(row);
        Assert.True(row.IsApproved);
    }

    [Fact]
    public async Task Revoke_SetsApprovedFalse()
    {
        await using var h = await UserAdminTestHarness.CreateAsync();
        var id = await h.CreateUserAsync("torevoke@example.com", approved: true);

        await h.Service.RevokeAsync(id, CT);
        var row = await h.Service.GetRowAsync(id, CT);

        Assert.NotNull(row);
        Assert.False(row.IsApproved);
    }

    [Fact]
    public async Task Promote_MakesUserAdminAndApproved()
    {
        await using var h = await UserAdminTestHarness.CreateAsync();
        var id = await h.CreateUserAsync("topromote@example.com", approved: false);

        await h.Service.PromoteToAdminAsync(id, CT);
        var row = await h.Service.GetRowAsync(id, CT);

        Assert.NotNull(row);
        Assert.True(row.IsAdmin);
        Assert.True(row.IsApproved);
        Assert.Equal("Admin", row.Role);
    }

    [Fact]
    public async Task Delete_RemovesUser()
    {
        await using var h = await UserAdminTestHarness.CreateAsync();
        var id = await h.CreateUserAsync("todelete@example.com", approved: true);

        await h.Service.DeleteAsync(id, CT);

        Assert.Null(await h.Service.GetRowAsync(id, CT));
    }

    [Fact]
    public async Task Approve_NonExistent_DoesNotThrow()
    {
        await using var h = await UserAdminTestHarness.CreateAsync();

        // Should be a no-op, not an exception.
        await h.Service.ApproveAsync("missing-id", CT);
    }

    [Fact]
    public async Task GetRow_NonExistent_ReturnsNull()
    {
        await using var h = await UserAdminTestHarness.CreateAsync();

        Assert.Null(await h.Service.GetRowAsync("nope", CT));
    }

    [Fact]
    public async Task PageSize_FromOptions_IsClamped()
    {
        await using var h = await UserAdminTestHarness.CreateAsync(configuredPageSize: 5000);

        // 5000 is clamped down to the 200 ceiling.
        Assert.Equal(200, h.Service.PageSize);
    }
}
