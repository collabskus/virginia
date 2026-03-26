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


await using var scope = app.Services.CreateAsyncScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
await db.Database.EnsureCreatedAsync();

app.MapDefaultEndpoints();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();

// ── Minimal API: profile photo endpoint ──────────────────────────────────────
app.MapGet("/api/contacts/{id:int}/photo", async (
    int id,
    IContactService svc,
    CancellationToken ct) =>
{
    var result = await svc.GetProfilePictureAsync(id, ct);
    return result is null
        ? Results.NotFound()
        : Results.File(result.Data, result.ContentType);
}).CacheOutput(p => p.Expire(TimeSpan.FromMinutes(5)).SetVaryByRouteValue("id"));

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

