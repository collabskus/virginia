using System.Diagnostics.Metrics;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Virginia.Data;
using Virginia.Services;
using Xunit;

namespace Virginia.Tests;

/// <summary>
/// Creates a fresh in-memory SQLite database and wired-up service for each test.
/// </summary>
public sealed class TestHarness : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    public AppDbContext Db { get; }
    public IContactService Service { get; }

    private TestHarness(SqliteConnection connection, AppDbContext db, IContactService service)
    {
        _connection = connection;
        Db = db;
        Service = service;
    }

    public static async Task<TestHarness> CreateAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var meterFactory = new TestMeterFactory();
        var telemetry = new ContactTelemetry(meterFactory);
        IContactService service = new ContactService(
            db,
            NullLogger<ContactService>.Instance,
            telemetry);

        return new TestHarness(connection, db, service);
    }

    public async ValueTask DisposeAsync()
    {
        await Db.DisposeAsync();
        await _connection.DisposeAsync();
    }
}

/// <summary>
/// Minimal IMeterFactory for unit tests.
/// </summary>
internal sealed class TestMeterFactory : IMeterFactory
{
    private readonly List<Meter> _meters = [];

    public Meter Create(MeterOptions options)
    {
        var meter = new Meter(options);
        _meters.Add(meter);
        return meter;
    }

    public void Dispose()
    {
        foreach (var m in _meters) m.Dispose();
    }
}
