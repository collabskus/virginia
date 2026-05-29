using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Virginia.Data;
using Virginia.Services;
using Xunit;

namespace Virginia.Tests;

/// <summary>
/// Spins up a fresh in-memory SQLite database with a fully wired ASP.NET Core
/// Identity stack (UserManager / RoleManager) plus the UserAdminService under
/// test. Uses only the packages the app already references — no Moq, no extra
/// test doubles. Each instance owns its own service provider and connection,
/// disposed at the end of the test.
/// </summary>
public sealed class UserAdminTestHarness : IAsyncDisposable
{
    private const string AdminRole = "Admin";
    private const string UserRole = "User";

    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;

    public AppDbContext Db { get; }
    public UserManager<AppUser> UserManager { get; }
    public RoleManager<IdentityRole> RoleManager { get; }
    public IUserAdminService Service { get; }

    private UserAdminTestHarness(
        SqliteConnection connection,
        ServiceProvider provider,
        IServiceScope scope,
        AppDbContext db,
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IUserAdminService service)
    {
        _connection = connection;
        _provider = provider;
        _scope = scope;
        Db = db;
        UserManager = userManager;
        RoleManager = roleManager;
        Service = service;
    }

    public static async Task<UserAdminTestHarness> CreateAsync(int configuredPageSize = 50)
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(o => o.UseSqlite(connection));

        services
            .AddIdentityCore<AppUser>(o =>
            {
                o.Password.RequireDigit = true;
                o.Password.RequireLowercase = true;
                o.Password.RequireUppercase = true;
                o.Password.RequireNonAlphanumeric = false;
                o.Password.RequiredLength = 8;
                o.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>();

        services.Configure<UserAdminOptions>(o => o.UsersPageSize = configuredPageSize);
        services.AddScoped<IUserAdminService, UserAdminService>();

        var provider = services.BuildServiceProvider();
        var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;

        var db = sp.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { AdminRole, UserRole })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var userManager = sp.GetRequiredService<UserManager<AppUser>>();
        var service = sp.GetRequiredService<IUserAdminService>();

        return new UserAdminTestHarness(
            connection, provider, scope, db, userManager, roleManager, service);
    }

    /// <summary>Create one user; returns its id. Assigned the "User" role.</summary>
    public async Task<string> CreateUserAsync(string email, bool approved)
    {
        var user = new AppUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            IsApproved = approved,
            CreatedAtUtc = DateTime.UtcNow
        };

        var result = await UserManager.CreateAsync(user, "Password123!");
        Assert.True(result.Succeeded,
            $"User creation failed: {string.Join(", ", result.Errors.Select(e => e.Description))}");

        await UserManager.AddToRoleAsync(user, UserRole);
        return user.Id;
    }

    /// <summary>Bulk-seed N approved users with deterministic emails.</summary>
    public async Task SeedUsersAsync(int count)
    {
        for (var i = 1; i <= count; i++)
            await CreateUserAsync($"seed-{i:D4}@example.com", approved: true);
    }

    public async Task AddToAdminRoleAsync(string userId)
    {
        var user = await UserManager.FindByIdAsync(userId);
        Assert.NotNull(user);
        if (await UserManager.IsInRoleAsync(user, UserRole))
            await UserManager.RemoveFromRoleAsync(user, UserRole);
        await UserManager.AddToRoleAsync(user, AdminRole);
    }

    public async ValueTask DisposeAsync()
    {
        _scope.Dispose();
        await _provider.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
