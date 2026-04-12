using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Virginia.Services;

public sealed class ContactTelemetry
{
    public const string ServiceName = "Virginia.Contacts";
    public static readonly ActivitySource Source = new(ServiceName);

    private readonly Counter<long> _created;
    private readonly Counter<long> _updated;
    private readonly Counter<long> _deleted;
    private readonly Histogram<double> _queryDuration;
    private readonly Histogram<double> _writeDuration;
    private readonly Counter<long> _bulkCreated;
    private readonly Counter<long> _bulkDeleted;

    public ContactTelemetry(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(ServiceName);
        _created = meter.CreateCounter<long>("contacts.created", "contacts", "Contacts created");
        _updated = meter.CreateCounter<long>("contacts.updated", "contacts", "Contacts updated");
        _deleted = meter.CreateCounter<long>("contacts.deleted", "contacts", "Contacts deleted");
        _queryDuration = meter.CreateHistogram<double>("contacts.query.duration", "ms", "Query duration");
        _writeDuration = meter.CreateHistogram<double>("contacts.write.duration", "ms", "Write duration");
        _bulkCreated = meter.CreateCounter<long>("contacts.bulk.created", "contacts", "Contacts bulk created");
        _bulkDeleted = meter.CreateCounter<long>("contacts.bulk.deleted", "contacts", "Contacts bulk deleted");
    }

    public void RecordContactCreated() => _created.Add(1);
    public void RecordContactUpdated() => _updated.Add(1);
    public void RecordContactDeleted() => _deleted.Add(1);
    public void RecordQueryDuration(double ms) => _queryDuration.Record(ms);
    public void RecordWriteDuration(double ms) => _writeDuration.Record(ms);
    public void RecordBulkCreated(long count) => _bulkCreated.Add(count);
    public void RecordBulkDeleted(long count) => _bulkDeleted.Add(count);
}
