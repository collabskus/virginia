using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Virginia.Components;
using Virginia.Data;
using Virginia.ServiceDefaults;
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
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Admin", policy => policy.RequireRole("Admin"))
    .AddPolicy("Approved", policy => policy.RequireClaim("approved", "True"));

builder.Services.AddCascadingAuthenticationState();

// ── Application services ─────────────────────────────────────────────────────
builder.Services.AddScoped<IContactService, ContactService>();
builder.Services.AddScoped<IToastService, ToastService>();
builder.Services.AddSingleton<ContactTelemetry>();
builder.Services.AddSingleton<IContactChangeNotifier, ContactChangeNotifier>();

// ── Register custom OTEL sources/meters ──────────────────────────────────────
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(ContactTelemetry.ServiceName))
    .WithMetrics(metrics => metrics.AddMeter(ContactTelemetry.ServiceName));

// ── Blazor ───────────────────────────────────────────────────────────────────
// HubOptions / CircuitOptions tuning: the default per-client parallel-invocation
// cap is 1, which serializes every operation on a circuit. Under heavy real-time
// fan-out (many circuits each re-rendering on every cross-user note/edit push),
// that single-slot dispatch — combined with the default disconnected-circuit
// retention of 100 — is what starts to wedge at ~100 concurrent live circuits.
// Raising the parallel cap and the timeouts gives the dispatcher room to keep
// up; raising MaxRetainedDisconnectedCircuits avoids evicting circuits that
// briefly disconnect during a large batched fan-out.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddHubOptions(options =>
    {
        // Allow a circuit to process several queued invocations concurrently
        // instead of strictly one-at-a-time. This is the single most important
        // change for high concurrent-circuit counts.
        options.MaximumParallelInvocationsPerClient = 10;

        // Give SignalR more tolerance before declaring a busy circuit dead.
        options.ClientTimeoutInterval = TimeSpan.FromMinutes(2);
        options.HandshakeTimeout = TimeSpan.FromSeconds(30);
        options.KeepAliveInterval = TimeSpan.FromSeconds(15);

        // Larger inbound message cap for safety under load (default 32 KB).
        options.MaximumReceiveMessageSize = 512 * 1024;
    })
    .AddCircuitOptions(options =>
    {
        // Default is 100. Raising this prevents eviction of circuits that
        // briefly drop while the host is saturated mid-fan-out.
        options.MaxRetainedDisconnectedCircuits = 256;

        // Keep a disconnected circuit recoverable a bit longer than default.
        options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(5);

        // Surface real errors to the client during testing/diagnosis.
        options.DetailedErrors = true;
    });

var app = builder.Build();

//// ══════════════════════════════════════════════════════════════════════════════
//// ██  ONE-TIME DEPLOYMENT: Delete this entire block after successful deploy  ██
//// ══════════════════════════════════════════════════════════════════════════════
//{
//    var connStr = app.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=virginia.db";
//    var dbPath = connStr.Replace("Data Source=", "", StringComparison.OrdinalIgnoreCase).Trim();
//    if (File.Exists(dbPath))
//    {
//        File.Delete(dbPath);
//        Console.WriteLine($"*** ONE-TIME: Deleted existing database at {dbPath} ***");
//    }
//}
//// ══════════════════════════════════════════════════════════════════════════════
//// ██  END ONE-TIME DEPLOYMENT BLOCK — DELETE ABOVE AFTER SUCCESSFUL DEPLOY  ██
//// ══════════════════════════════════════════════════════════════════════════════

// ── Apply migrations + seed ──────────────────────────────────────────────────
await using (var scope = app.Services.CreateAsyncScope())
{
    //var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    //await db.Database.MigrateAsync();

    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();

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
