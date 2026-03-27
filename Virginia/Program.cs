using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Virginia.Components;
using Virginia.Data;
using Virginia.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Aspire service defaults (OTEL, health checks, resilience, discovery) ─────
builder.AddServiceDefaults();

// ── EF Core + SQLite ─────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=virginia.db"));

// ── ASP.NET Core Identity ────────────────────────────────────────────────────
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 8;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddScoped<IUserClaimsPrincipalFactory<AppUser>, AppClaimsPrincipalFactory>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/account/login";
    options.AccessDeniedPath = "/account/login";
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;
});

// ── Authorization policies ───────────────────────────────────────────────────
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("Approved", policy => policy.RequireClaim("approved", "True"));
});

builder.Services.AddCascadingAuthenticationState();

// ── Application services ─────────────────────────────────────────────────────
builder.Services.AddScoped<IContactService, ContactService>();
builder.Services.AddSingleton<ContactTelemetry>();

// ── Register custom OTEL sources/meters ──────────────────────────────────────
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(ContactTelemetry.ServiceName))
    .WithMetrics(metrics => metrics.AddMeter(ContactTelemetry.ServiceName));

// ── Blazor ───────────────────────────────────────────────────────────────────
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// ── Apply migrations + seed ──────────────────────────────────────────────────
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    string[] roles = ["Admin", "User"];
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
    var adminSection = app.Configuration.GetSection("AdminUser");
    var adminEmail = adminSection["Email"] ?? "admin@virginia.local";
    var adminPassword = adminSection["Password"] ?? "Admin123!";

    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser is null)
    {
        adminUser = new AppUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            IsApproved = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        var createResult = await userManager.CreateAsync(adminUser, adminPassword);
        if (createResult.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
}

app.MapDefaultEndpoints();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// ── Minimal API: profile photo endpoint (authenticated) ──────────────────────
app.MapGet("/api/contacts/{id:int}/photo", async (
    int id,
    IContactService svc,
    CancellationToken ct) =>
{
    var result = await svc.GetProfilePictureAsync(id, ct);
    return result is null
        ? Results.NotFound()
        : Results.File(result.Data, result.ContentType);
})
.RequireAuthorization("Approved")
.CacheOutput(p => p.Expire(TimeSpan.FromMinutes(5)).SetVaryByRouteValue("id"));

// ── Minimal API: logout ──────────────────────────────────────────────────────
app.MapPost("/account/perform-logout", async (
    SignInManager<AppUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Redirect("/account/login");
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
