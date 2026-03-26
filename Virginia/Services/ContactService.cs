using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Virginia.Data;

namespace Virginia.Services;

public sealed partial class ContactService(
    AppDbContext db,
    ILogger<ContactService> logger,
    ContactTelemetry telemetry) : IContactService
{
    // ─── List ────────────────────────────────────────────────────────────

    public async Task<PagedResult<ContactListItem>> ListAsync(
        ContactFilter filter, int page, int pageSize, CancellationToken ct)
    {
        using var activity = ContactTelemetry.Source.StartActivity("ListContacts");
        activity?.SetTag("filter.name", filter.Name);
        activity?.SetTag("filter.city", filter.City);
        activity?.SetTag("filter.state", filter.State);
        activity?.SetTag("page", page);
        activity?.SetTag("pageSize", pageSize);

        var sw = Stopwatch.StartNew();

        var query = db.Contacts.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Name))
        {
            var term = filter.Name.Trim();
            query = query.Where(c =>
                c.FirstName.Contains(term) || c.LastName.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(filter.Email))
        {
            var term = filter.Email.Trim();
            query = query.Where(c =>
                c.Emails.Any(e => e.Address.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(filter.Phone))
        {
            var term = filter.Phone.Trim();
            query = query.Where(c =>
                c.Phones.Any(p => p.Number.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(filter.City))
        {
            var term = filter.City.Trim();
            query = query.Where(c =>
                c.Addresses.Any(a => a.City.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(filter.State))
        {
            var term = filter.State.Trim();
            query = query.Where(c =>
                c.Addresses.Any(a => a.State.Contains(term)));
        }

        if (filter.HasPhoto == true)
            query = query.Where(c => c.ProfilePicture != null);
        else if (filter.HasPhoto == false)
            query = query.Where(c => c.ProfilePicture == null);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(c => c.LastName)
            .ThenBy(c => c.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new ContactListItem(
                c.Id,
                c.FirstName,
                c.LastName,
                c.ProfilePicture != null,
                c.Emails.OrderBy(e => e.Id).Select(e => e.Address).FirstOrDefault(),
                c.Phones.OrderBy(p => p.Id).Select(p => p.Number).FirstOrDefault(),
                c.Addresses.OrderBy(a => a.Id).Select(a => a.City).FirstOrDefault(),
                c.CreatedAtUtc))
            .ToListAsync(ct);

        sw.Stop();

        activity?.SetTag("result.count", items.Count);
        activity?.SetTag("result.totalCount", totalCount);
        telemetry.RecordQueryDuration(sw.Elapsed.TotalMilliseconds);

        Log.ListedContacts(logger, items.Count, totalCount, sw.Elapsed.TotalMilliseconds, page);

        return new PagedResult<ContactListItem>(items, totalCount, page, pageSize);
    }

    // ─── Get ─────────────────────────────────────────────────────────────

    public async Task<ContactDetailDto?> GetAsync(int id, CancellationToken ct)
    {
        using var activity = ContactTelemetry.Source.StartActivity("GetContact");
        activity?.SetTag("contact.id", id);

        var sw = Stopwatch.StartNew();

        var c = await db.Contacts
            .AsNoTracking()
            .Include(x => x.Emails.OrderBy(e => e.Id))
            .Include(x => x.Phones.OrderBy(p => p.Id))
            .Include(x => x.Addresses.OrderBy(a => a.Id))
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        sw.Stop();
        telemetry.RecordQueryDuration(sw.Elapsed.TotalMilliseconds);

        if (c is null)
        {
            Log.ContactNotFound(logger, id, sw.Elapsed.TotalMilliseconds);
            return null;
        }

        Log.RetrievedContact(logger, id, sw.Elapsed.TotalMilliseconds);

        return new ContactDetailDto(
            c.Id, c.FirstName, c.LastName,
            c.ProfilePicture is not null, c.ProfilePictureContentType,
            c.CreatedAtUtc, c.UpdatedAtUtc,
            [.. c.Emails.Select(e => new EmailDto(e.Id, e.Label, e.Address))],
            [.. c.Phones.Select(p => new PhoneDto(p.Id, p.Label, p.Number))],
            [.. c.Addresses.Select(a => new AddressDto(
                a.Id, a.Label, a.Street, a.City, a.State, a.PostalCode, a.Country))]);
    }

    // ─── Create ──────────────────────────────────────────────────────────

    public async Task<int> CreateAsync(ContactFormModel form, CancellationToken ct)
    {
        using var activity = ContactTelemetry.Source.StartActivity("CreateContact");
        var sw = Stopwatch.StartNew();

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var now = DateTime.UtcNow;
            var contact = new Contact
            {
                FirstName = form.FirstName.Trim(),
                LastName = form.LastName.Trim(),
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                Emails = [.. form.Emails.Select(e => new ContactEmail
                {
                    Label = e.Label.Trim(),
                    Address = e.Address.Trim()
                })],
                Phones = [.. form.Phones.Select(p => new ContactPhone
                {
                    Label = p.Label.Trim(),
                    Number = p.Number.Trim()
                })],
                Addresses = [.. form.Addresses.Select(a => new ContactAddress
                {
                    Label = a.Label.Trim(),
                    Street = a.Street.Trim(),
                    City = a.City.Trim(),
                    State = a.State.Trim(),
                    PostalCode = a.PostalCode.Trim(),
                    Country = a.Country.Trim()
                })]
            };

            db.Contacts.Add(contact);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            sw.Stop();
            activity?.SetTag("contact.id", contact.Id);
            telemetry.RecordContactCreated();
            telemetry.RecordWriteDuration(sw.Elapsed.TotalMilliseconds);

            Log.CreatedContact(logger, contact.Id, contact.FullName,
                contact.Emails.Count, contact.Phones.Count, contact.Addresses.Count,
                sw.Elapsed.TotalMilliseconds);

            return contact.Id;
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            Log.FailedToCreateContact(logger, ex);
            throw;
        }
    }

    // ─── Update ──────────────────────────────────────────────────────────

    public async Task UpdateAsync(int id, ContactFormModel form, CancellationToken ct)
    {
        using var activity = ContactTelemetry.Source.StartActivity("UpdateContact");
        activity?.SetTag("contact.id", id);
        var sw = Stopwatch.StartNew();

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var contact = await db.Contacts
                .Include(c => c.Emails)
                .Include(c => c.Phones)
                .Include(c => c.Addresses)
                .FirstOrDefaultAsync(c => c.Id == id, ct)
                ?? throw new InvalidOperationException($"Contact {id} not found.");

            contact.FirstName = form.FirstName.Trim();
            contact.LastName = form.LastName.Trim();
            contact.UpdatedAtUtc = DateTime.UtcNow;

            SyncChildren(contact.Emails, form.Emails,
                (e, m) => e.Id == m.Id && m.Id != 0,
                (e, m) => { e.Label = m.Label.Trim(); e.Address = m.Address.Trim(); },
                m => new ContactEmail
                {
                    ContactId = id,
                    Label = m.Label.Trim(),
                    Address = m.Address.Trim()
                });

            SyncChildren(contact.Phones, form.Phones,
                (e, m) => e.Id == m.Id && m.Id != 0,
                (e, m) => { e.Label = m.Label.Trim(); e.Number = m.Number.Trim(); },
                m => new ContactPhone
                {
                    ContactId = id,
                    Label = m.Label.Trim(),
                    Number = m.Number.Trim()
                });

            SyncChildren(contact.Addresses, form.Addresses,
                (e, m) => e.Id == m.Id && m.Id != 0,
                (e, m) =>
                {
                    e.Label = m.Label.Trim();
                    e.Street = m.Street.Trim();
                    e.City = m.City.Trim();
                    e.State = m.State.Trim();
                    e.PostalCode = m.PostalCode.Trim();
                    e.Country = m.Country.Trim();
                },
                m => new ContactAddress
                {
                    ContactId = id,
                    Label = m.Label.Trim(),
                    Street = m.Street.Trim(),
                    City = m.City.Trim(),
                    State = m.State.Trim(),
                    PostalCode = m.PostalCode.Trim(),
                    Country = m.Country.Trim()
                });

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            sw.Stop();
            telemetry.RecordContactUpdated();
            telemetry.RecordWriteDuration(sw.Elapsed.TotalMilliseconds);

            Log.UpdatedContact(logger, id, contact.FullName, sw.Elapsed.TotalMilliseconds);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            await tx.RollbackAsync(ct);
            Log.FailedToUpdateContact(logger, id, ex);
            throw;
        }
    }

    // ─── Delete ──────────────────────────────────────────────────────────

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        using var activity = ContactTelemetry.Source.StartActivity("DeleteContact");
        activity?.SetTag("contact.id", id);
        var sw = Stopwatch.StartNew();

        var rows = await db.Contacts.Where(c => c.Id == id).ExecuteDeleteAsync(ct);

        sw.Stop();

        if (rows == 0)
        {
            Log.DeleteContactNotFound(logger, id);
            return;
        }

        telemetry.RecordContactDeleted();
        telemetry.RecordWriteDuration(sw.Elapsed.TotalMilliseconds);
        Log.DeletedContact(logger, id, sw.Elapsed.TotalMilliseconds);
    }

    // ─── Profile picture ─────────────────────────────────────────────────

    public async Task SetProfilePictureAsync(
        int id, byte[] data, string contentType, CancellationToken ct)
    {
        using var activity = ContactTelemetry.Source.StartActivity("SetProfilePicture");
        activity?.SetTag("contact.id", id);
        activity?.SetTag("picture.bytes", data.Length);
        activity?.SetTag("picture.contentType", contentType);
        var sw = Stopwatch.StartNew();

        var rows = await db.Contacts.Where(c => c.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.ProfilePicture, data)
                .SetProperty(c => c.ProfilePictureContentType, contentType)
                .SetProperty(c => c.UpdatedAtUtc, DateTime.UtcNow), ct);

        sw.Stop();

        if (rows == 0)
            throw new InvalidOperationException($"Contact {id} not found.");

        Log.SetProfilePicture(logger, id, data.Length, sw.Elapsed.TotalMilliseconds);
    }

    public async Task<ProfilePictureResult?> GetProfilePictureAsync(int id, CancellationToken ct)
    {
        var result = await db.Contacts
            .AsNoTracking()
            .Where(c => c.Id == id && c.ProfilePicture != null)
            .Select(c => new { c.ProfilePicture, c.ProfilePictureContentType })
            .FirstOrDefaultAsync(ct);

        if (result?.ProfilePicture is null)
            return null;

        return new ProfilePictureResult(
            result.ProfilePicture,
            result.ProfilePictureContentType ?? "image/jpeg");
    }

    public async Task RemoveProfilePictureAsync(int id, CancellationToken ct)
    {
        using var activity = ContactTelemetry.Source.StartActivity("RemoveProfilePicture");
        activity?.SetTag("contact.id", id);
        var sw = Stopwatch.StartNew();

        await db.Contacts.Where(c => c.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.ProfilePicture, (byte[]?)null)
                .SetProperty(c => c.ProfilePictureContentType, (string?)null)
                .SetProperty(c => c.UpdatedAtUtc, DateTime.UtcNow), ct);

        sw.Stop();
        Log.RemovedProfilePicture(logger, id, sw.Elapsed.TotalMilliseconds);
    }

    // ─── Private helper ──────────────────────────────────────────────────

    private void SyncChildren<TEntity, TModel>(
        List<TEntity> entities,
        List<TModel> models,
        Func<TEntity, TModel, bool> match,
        Action<TEntity, TModel> update,
        Func<TModel, TEntity> create) where TEntity : class
    {
        var toRemove = entities.Where(e => !models.Any(m => match(e, m))).ToList();
        foreach (var item in toRemove)
        {
            entities.Remove(item);
            db.Remove(item);
        }

        foreach (var model in models)
        {
            var existing = entities.FirstOrDefault(e => match(e, model));
            if (existing is not null)
                update(existing, model);
            else
                entities.Add(create(model));
        }
    }

    // ─── Source-generated log messages (CA1848 / CA1873 compliant) ────────

    private static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Information,
            Message = "Listed {Count}/{Total} contacts in {ElapsedMs:F1}ms (page {Page})")]
        public static partial void ListedContacts(
            ILogger logger, int count, int total, double elapsedMs, int page);

        [LoggerMessage(Level = LogLevel.Warning,
            Message = "Contact {Id} not found ({ElapsedMs:F1}ms)")]
        public static partial void ContactNotFound(
            ILogger logger, int id, double elapsedMs);

        [LoggerMessage(Level = LogLevel.Information,
            Message = "Retrieved contact {Id} in {ElapsedMs:F1}ms")]
        public static partial void RetrievedContact(
            ILogger logger, int id, double elapsedMs);

        [LoggerMessage(Level = LogLevel.Information,
            Message = "Created contact {Id} ({Name}) with {Emails}e/{Phones}p/{Addresses}a in {ElapsedMs:F1}ms")]
        public static partial void CreatedContact(
            ILogger logger, int id, string name,
            int emails, int phones, int addresses, double elapsedMs);

        [LoggerMessage(Level = LogLevel.Error,
            Message = "Failed to create contact")]
        public static partial void FailedToCreateContact(
            ILogger logger, Exception ex);

        [LoggerMessage(Level = LogLevel.Information,
            Message = "Updated contact {Id} ({Name}) in {ElapsedMs:F1}ms")]
        public static partial void UpdatedContact(
            ILogger logger, int id, string name, double elapsedMs);

        [LoggerMessage(Level = LogLevel.Error,
            Message = "Failed to update contact {Id}")]
        public static partial void FailedToUpdateContact(
            ILogger logger, int id, Exception ex);

        [LoggerMessage(Level = LogLevel.Warning,
            Message = "Delete: contact {Id} not found")]
        public static partial void DeleteContactNotFound(
            ILogger logger, int id);

        [LoggerMessage(Level = LogLevel.Information,
            Message = "Deleted contact {Id} in {ElapsedMs:F1}ms")]
        public static partial void DeletedContact(
            ILogger logger, int id, double elapsedMs);

        [LoggerMessage(Level = LogLevel.Information,
            Message = "Set profile picture for contact {Id} ({Bytes} bytes) in {ElapsedMs:F1}ms")]
        public static partial void SetProfilePicture(
            ILogger logger, int id, int bytes, double elapsedMs);

        [LoggerMessage(Level = LogLevel.Information,
            Message = "Removed profile picture for contact {Id} in {ElapsedMs:F1}ms")]
        public static partial void RemovedProfilePicture(
            ILogger logger, int id, double elapsedMs);
    }
}
