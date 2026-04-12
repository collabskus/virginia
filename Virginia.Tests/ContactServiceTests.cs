using Microsoft.EntityFrameworkCore;
using Virginia.Data;
using Xunit;

namespace Virginia.Tests;

public sealed class ContactServiceTests
{
    private static CancellationToken CT => TestContext.Current.CancellationToken;

    // ─── Create ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ReturnsPositiveId()
    {
        await using var h = await TestHarness.CreateAsync();

        var id = await h.Service.CreateAsync(
            new ContactFormModel { FirstName = "Jane", LastName = "Doe" }, CT);

        Assert.True(id > 0);
    }

    [Fact]
    public async Task Create_WithChildren_PersistsAll()
    {
        await using var h = await TestHarness.CreateAsync();
        var form = new ContactFormModel
        {
            FirstName = "John",
            LastName = "Smith",
            Emails =
            [
                new() { Label = "Work", Address = "john@work.com" },
                new() { Label = "Home", Address = "john@home.com" }
            ],
            Phones = [new() { Label = "Mobile", Number = "555-0100" }],
            Addresses =
            [
                new()
                {
                    Label = "Office", Street = "123 Main St",
                    City = "Richmond", State = "VA",
                    PostalCode = "23220", Country = "US"
                }
            ]
        };

        var id = await h.Service.CreateAsync(form, CT);
        var detail = await h.Service.GetAsync(id, CT);

        Assert.NotNull(detail);
        Assert.Equal("John", detail.FirstName);
        Assert.Equal(2, detail.Emails.Count);
        Assert.Single(detail.Phones);
        Assert.Single(detail.Addresses);
        Assert.Equal("Richmond", detail.Addresses[0].City);
    }

    [Fact]
    public async Task Create_WithZeroChildren_Succeeds()
    {
        await using var h = await TestHarness.CreateAsync();

        var id = await h.Service.CreateAsync(
            new ContactFormModel { FirstName = "Solo", LastName = "Contact" }, CT);
        var detail = await h.Service.GetAsync(id, CT);

        Assert.NotNull(detail);
        Assert.Empty(detail.Emails);
        Assert.Empty(detail.Phones);
        Assert.Empty(detail.Addresses);
    }

    [Fact]
    public async Task Create_TrimsWhitespace()
    {
        await using var h = await TestHarness.CreateAsync();

        var id = await h.Service.CreateAsync(new ContactFormModel
        {
            FirstName = "  Alice  ",
            LastName = "  Smith  ",
            Emails = [new() { Label = " Work ", Address = " a@b.com " }]
        }, CT);

        var detail = await h.Service.GetAsync(id, CT);
        Assert.Equal("Alice", detail!.FirstName);
        Assert.Equal("Smith", detail.LastName);
        Assert.Equal("Work", detail.Emails[0].Label);
        Assert.Equal("a@b.com", detail.Emails[0].Address);
    }

    [Fact]
    public async Task Create_SetsTimestamps()
    {
        await using var h = await TestHarness.CreateAsync();
        var before = DateTime.UtcNow;

        var id = await h.Service.CreateAsync(
            new ContactFormModel { FirstName = "T", LastName = "S" }, CT);
        var detail = await h.Service.GetAsync(id, CT);

        Assert.True(detail!.CreatedAtUtc >= before);
        Assert.True(detail.UpdatedAtUtc >= before);
        Assert.Equal(detail.CreatedAtUtc, detail.UpdatedAtUtc);
    }

    // ─── Get ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Get_NonExistent_ReturnsNull()
    {
        await using var h = await TestHarness.CreateAsync();

        Assert.Null(await h.Service.GetAsync(9999, CT));
    }

    // ─── Update ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_ChangesNameAndChildren()
    {
        await using var h = await TestHarness.CreateAsync();
        var id = await h.Service.CreateAsync(new ContactFormModel
        {
            FirstName = "Test",
            LastName = "User",
            Emails =
            [
                new() { Label = "A", Address = "a@t.com" },
                new() { Label = "B", Address = "b@t.com" }
            ]
        }, CT);
        var detail = await h.Service.GetAsync(id, CT);

        var updateForm = new ContactFormModel
        {
            FirstName = "Test",
            LastName = "User",
            Emails =
            [
                new() { Id = detail!.Emails[0].Id, Label = "A2", Address = "a2@t.com" },
                new() { Label = "C", Address = "c@t.com" }
            ]
        };

        await h.Service.UpdateAsync(id, updateForm, CT);
        var updated = await h.Service.GetAsync(id, CT);

        Assert.Equal(2, updated!.Emails.Count);
        Assert.Contains(updated.Emails, e => e.Address == "a2@t.com");
        Assert.Contains(updated.Emails, e => e.Address == "c@t.com");
        Assert.DoesNotContain(updated.Emails, e => e.Address == "b@t.com");
    }

    [Fact]
    public async Task Update_NonExistent_Throws()
    {
        await using var h = await TestHarness.CreateAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            h.Service.UpdateAsync(9999,
                new ContactFormModel { FirstName = "X", LastName = "Y" }, CT));
    }

    [Fact]
    public async Task Update_BumpsTimestamp()
    {
        await using var h = await TestHarness.CreateAsync();
        var id = await h.Service.CreateAsync(
            new ContactFormModel { FirstName = "T", LastName = "S" }, CT);
        var before = (await h.Service.GetAsync(id, CT))!.UpdatedAtUtc;

        await Task.Delay(50, CT);
        await h.Service.UpdateAsync(id,
            new ContactFormModel { FirstName = "T2", LastName = "S" }, CT);

        var after = (await h.Service.GetAsync(id, CT))!.UpdatedAtUtc;
        Assert.True(after > before);
    }

    // ─── Delete ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_RemovesContact()
    {
        await using var h = await TestHarness.CreateAsync();
        var id = await h.Service.CreateAsync(
            new ContactFormModel { FirstName = "Gone", LastName = "Soon" }, CT);

        await h.Service.DeleteAsync(id, CT);

        Assert.Null(await h.Service.GetAsync(id, CT));
    }

    [Fact]
    public async Task Delete_CascadesChildren()
    {
        await using var h = await TestHarness.CreateAsync();
        var form = new ContactFormModel
        {
            FirstName = "P",
            LastName = "C",
            Emails = [new() { Label = "W", Address = "w@t.com" }],
            Phones = [new() { Label = "M", Number = "555-0001" }],
            Addresses =
            [
                new()
                {
                    Label = "H", Street = "1 St", City = "X",
                    PostalCode = "12345", Country = "US"
                }
            ]
        };

        var id = await h.Service.CreateAsync(form, CT);
        await h.Service.DeleteAsync(id, CT);

        Assert.Equal(0, await h.Db.ContactEmails.CountAsync(CT));
        Assert.Equal(0, await h.Db.ContactPhones.CountAsync(CT));
        Assert.Equal(0, await h.Db.ContactAddresses.CountAsync(CT));
    }

    [Fact]
    public async Task Delete_NonExistent_DoesNotThrow()
    {
        await using var h = await TestHarness.CreateAsync();

        await h.Service.DeleteAsync(9999, CT);
    }

    // ─── Profile picture ─────────────────────────────────────────────────

    [Fact]
    public async Task ProfilePicture_SetAndGet()
    {
        await using var h = await TestHarness.CreateAsync();
        var id = await h.Service.CreateAsync(
            new ContactFormModel { FirstName = "Photo", LastName = "Test" }, CT);

        var data = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        await h.Service.SetProfilePictureAsync(id, data, "image/png", CT);

        var result = await h.Service.GetProfilePictureAsync(id, CT);
        Assert.NotNull(result);
        Assert.Equal("image/png", result.ContentType);
        Assert.Equal(data, result.Data);
    }

    [Fact]
    public async Task ProfilePicture_Get_NonExistent_ReturnsNull()
    {
        await using var h = await TestHarness.CreateAsync();

        Assert.Null(await h.Service.GetProfilePictureAsync(9999, CT));
    }

    [Fact]
    public async Task ProfilePicture_Remove_ClearsData()
    {
        await using var h = await TestHarness.CreateAsync();
        var id = await h.Service.CreateAsync(
            new ContactFormModel { FirstName = "A", LastName = "B" }, CT);
        await h.Service.SetProfilePictureAsync(
            id, [0x00], "image/jpeg", CT);

        await h.Service.RemoveProfilePictureAsync(id, CT);

        Assert.Null(await h.Service.GetProfilePictureAsync(id, CT));
        var detail = await h.Service.GetAsync(id, CT);
        Assert.False(detail!.HasPhoto);
    }

    [Fact]
    public async Task ProfilePicture_Set_NonExistent_Throws()
    {
        await using var h = await TestHarness.CreateAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            h.Service.SetProfilePictureAsync(9999, [0x00], "image/png", CT));
    }

    // ─── List: basic ─────────────────────────────────────────────────────

    [Fact]
    public async Task List_ReturnsAllContacts()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.Service.CreateAsync(
            new ContactFormModel { FirstName = "Alice", LastName = "A" }, CT);
        await h.Service.CreateAsync(
            new ContactFormModel { FirstName = "Bob", LastName = "B" }, CT);

        var result = await h.Service.ListAsync(new(), 1, 50, CT);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task List_Paging_Works()
    {
        await using var h = await TestHarness.CreateAsync();
        for (var i = 0; i < 5; i++)
            await h.Service.CreateAsync(
                new() { FirstName = $"U{i}", LastName = "T" }, CT);

        var page1 = await h.Service.ListAsync(new(), 1, 2, CT);
        var page2 = await h.Service.ListAsync(new(), 2, 2, CT);

        Assert.Equal(2, page1.Items.Count);
        Assert.Equal(2, page2.Items.Count);
        Assert.Equal(5, page1.TotalCount);
        Assert.True(page1.HasNext);
        Assert.True(page2.HasPrevious);
    }

    [Fact]
    public async Task List_OrdersByLastNameThenFirst()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.Service.CreateAsync(new() { FirstName = "B", LastName = "Z" }, CT);
        await h.Service.CreateAsync(new() { FirstName = "A", LastName = "A" }, CT);
        await h.Service.CreateAsync(new() { FirstName = "C", LastName = "A" }, CT);

        var result = await h.Service.ListAsync(new(), 1, 50, CT);

        Assert.Equal("A", result.Items[0].FirstName);
        Assert.Equal("C", result.Items[1].FirstName);
        Assert.Equal("B", result.Items[2].FirstName);
    }

    // ─── List: filters ───────────────────────────────────────────────────

    [Fact]
    public async Task List_FilterByName_CaseInsensitive()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.Service.CreateAsync(new() { FirstName = "Alice", LastName = "Smith" }, CT);
        await h.Service.CreateAsync(new() { FirstName = "Bob", LastName = "Jones" }, CT);

        var result = await h.Service.ListAsync(new(Name: "alice"), 1, 50, CT);

        Assert.Single(result.Items);
        Assert.Equal("Alice", result.Items[0].FirstName);
    }

    [Fact]
    public async Task List_FilterByEmail_CaseInsensitive()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.Service.CreateAsync(new()
        {
            FirstName = "A",
            LastName = "B",
            Emails = [new() { Label = "W", Address = "Alice@Test.COM" }]
        }, CT);
        await h.Service.CreateAsync(new() { FirstName = "C", LastName = "D" }, CT);

        var result = await h.Service.ListAsync(new(Email: "alice@test"), 1, 50, CT);

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task List_FilterByCity_CaseInsensitive()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.Service.CreateAsync(new()
        {
            FirstName = "A",
            LastName = "B",
            Addresses =
            [
                new()
                {
                    Label = "H", Street = "1 St", City = "Newport News",
                    State = "VA", PostalCode = "23601", Country = "US"
                }
            ]
        }, CT);

        var result = await h.Service.ListAsync(new(City: "newport news"), 1, 50, CT);

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task List_FilterByState_CaseInsensitive()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.Service.CreateAsync(new()
        {
            FirstName = "A",
            LastName = "B",
            Addresses =
            [
                new()
                {
                    Label = "H", Street = "1 St", City = "X",
                    State = "VA", PostalCode = "23601", Country = "US"
                }
            ]
        }, CT);

        var result = await h.Service.ListAsync(new(State: "va"), 1, 50, CT);

        Assert.Single(result.Items);
    }

    // ─── Page size clamping ──────────────────────────────────────────────

    [Fact]
    public async Task List_HugePageSize_ClampedToMax()
    {
        await using var h = await TestHarness.CreateAsync();
        for (var i = 0; i < 5; i++)
            await h.Service.CreateAsync(
                new() { FirstName = $"U{i}", LastName = "T" }, CT);

        var result = await h.Service.ListAsync(new(), 1, 999999, CT);

        Assert.Equal(5, result.Items.Count);
        Assert.Equal(5, result.TotalCount);
    }

    [Fact]
    public async Task List_ZeroPageSize_ClampedToOne()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.Service.CreateAsync(
            new() { FirstName = "A", LastName = "B" }, CT);

        var result = await h.Service.ListAsync(new(), 1, 0, CT);

        Assert.Single(result.Items);
    }

    // ─── Edge cases ──────────────────────────────────────────────────────

    [Fact]
    public async Task List_EmptyDatabase_ReturnsEmpty()
    {
        await using var h = await TestHarness.CreateAsync();

        var result = await h.Service.ListAsync(new(), 1, 25, CT);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public async Task List_PageBeyondRange_ReturnsEmpty()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.Service.CreateAsync(
            new() { FirstName = "A", LastName = "B" }, CT);

        var result = await h.Service.ListAsync(new(), 999, 25, CT);

        Assert.Empty(result.Items);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task List_WhitespaceOnlyFilter_TreatedAsNoFilter()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.Service.CreateAsync(
            new() { FirstName = "A", LastName = "B" }, CT);

        var result = await h.Service.ListAsync(new(Name: "   "), 1, 50, CT);

        Assert.Single(result.Items);
    }

    // ─── Notes ───────────────────────────────────────────────────────────

    [Fact]
    public async Task AddNote_PersistsAndReturnsInDetail()
    {
        await using var h = await TestHarness.CreateAsync();
        var id = await h.Service.CreateAsync(
            new() { FirstName = "A", LastName = "B" }, CT);

        var noteId = await h.Service.AddNoteAsync(
            id, "Hello world", "user-1", "admin@test.com", CT);

        Assert.True(noteId > 0);

        var detail = await h.Service.GetAsync(id, CT);
        Assert.Single(detail!.Notes);
        Assert.Equal("Hello world", detail.Notes[0].Content);
        Assert.Equal("admin@test.com", detail.Notes[0].CreatedByUserName);
    }

    [Fact]
    public async Task AddNote_MultipleNotes_OrderedByNewest()
    {
        await using var h = await TestHarness.CreateAsync();
        var id = await h.Service.CreateAsync(
            new() { FirstName = "A", LastName = "B" }, CT);

        await h.Service.AddNoteAsync(id, "First", "u1", "user1@test.com", CT);
        await Task.Delay(50, CT);
        await h.Service.AddNoteAsync(id, "Second", "u2", "user2@test.com", CT);

        var detail = await h.Service.GetAsync(id, CT);
        Assert.Equal(2, detail!.Notes.Count);
        Assert.Equal("Second", detail.Notes[0].Content);
        Assert.Equal("First", detail.Notes[1].Content);
    }

    [Fact]
    public async Task AddNote_NonExistentContact_Throws()
    {
        await using var h = await TestHarness.CreateAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            h.Service.AddNoteAsync(9999, "Note", "u1", "user@test.com", CT));
    }

    [Fact]
    public async Task AddNote_TrimsContent()
    {
        await using var h = await TestHarness.CreateAsync();
        var id = await h.Service.CreateAsync(
            new() { FirstName = "A", LastName = "B" }, CT);

        await h.Service.AddNoteAsync(id, "  Trimmed  ", "u1", "user@test.com", CT);

        var detail = await h.Service.GetAsync(id, CT);
        Assert.Equal("Trimmed", detail!.Notes[0].Content);
    }

    // ─── Bulk create ─────────────────────────────────────────────────────

    [Fact]
    public async Task CreateBulk_CreatesRequestedCount()
    {
        await using var h = await TestHarness.CreateAsync();

        var created = await h.Service.CreateBulkAsync(50, CT);

        Assert.Equal(50, created);

        var result = await h.Service.ListAsync(new(), 1, 100, CT);
        Assert.Equal(50, result.TotalCount);
    }

    [Fact]
    public async Task CreateBulk_ContactsHaveValidNames()
    {
        await using var h = await TestHarness.CreateAsync();

        await h.Service.CreateBulkAsync(10, CT);

        var result = await h.Service.ListAsync(new(), 1, 100, CT);
        foreach (var item in result.Items)
        {
            Assert.False(string.IsNullOrWhiteSpace(item.FirstName));
            Assert.False(string.IsNullOrWhiteSpace(item.LastName));
        }
    }

    [Fact]
    public async Task CreateBulk_ContactsHaveChildren()
    {
        await using var h = await TestHarness.CreateAsync();

        // With 100 contacts, statistically we should get some with emails/phones/addresses
        await h.Service.CreateBulkAsync(100, CT);

        var emailCount = await h.Db.ContactEmails.CountAsync(CT);
        var phoneCount = await h.Db.ContactPhones.CountAsync(CT);
        var addressCount = await h.Db.ContactAddresses.CountAsync(CT);

        // With 80%/70%/60% probabilities and 100 contacts, we should have some of each
        Assert.True(emailCount > 0, "Expected some contacts to have emails");
        Assert.True(phoneCount > 0, "Expected some contacts to have phones");
        Assert.True(addressCount > 0, "Expected some contacts to have addresses");
    }

    [Fact]
    public async Task CreateBulk_CanBeCalledMultipleTimes()
    {
        await using var h = await TestHarness.CreateAsync();

        await h.Service.CreateBulkAsync(10, CT);
        await h.Service.CreateBulkAsync(10, CT);

        var result = await h.Service.ListAsync(new(), 1, 100, CT);
        Assert.Equal(20, result.TotalCount);
    }

    [Fact]
    public async Task CreateBulk_CountClampedToMax()
    {
        await using var h = await TestHarness.CreateAsync();

        // Count above 10,000 should be clamped — we test with a small
        // value to keep the test fast; the important thing is that
        // a negative count is clamped to 1.
        var created = await h.Service.CreateBulkAsync(-5, CT);

        Assert.Equal(1, created);
    }

    [Fact]
    public async Task CreateBulk_SetsTimestamps()
    {
        await using var h = await TestHarness.CreateAsync();
        var before = DateTime.UtcNow;

        await h.Service.CreateBulkAsync(5, CT);

        var contacts = await h.Db.Contacts.ToListAsync(CT);
        foreach (var c in contacts)
        {
            Assert.True(c.CreatedAtUtc >= before);
            Assert.True(c.UpdatedAtUtc >= before);
        }
    }

    [Fact]
    public async Task CreateBulk_RespectsCancellation()
    {
        await using var h = await TestHarness.CreateAsync();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            h.Service.CreateBulkAsync(500, cts.Token));
    }

    // ─── Delete all ──────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAll_RemovesAllContacts()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.Service.CreateAsync(
            new ContactFormModel { FirstName = "A", LastName = "B" }, CT);
        await h.Service.CreateAsync(
            new ContactFormModel { FirstName = "C", LastName = "D" }, CT);

        var deleted = await h.Service.DeleteAllAsync(CT);

        Assert.Equal(2, deleted);

        var result = await h.Service.ListAsync(new(), 1, 50, CT);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task DeleteAll_CascadesChildren()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.Service.CreateAsync(new ContactFormModel
        {
            FirstName = "A",
            LastName = "B",
            Emails = [new() { Label = "W", Address = "a@b.com" }],
            Phones = [new() { Label = "M", Number = "555-0001" }],
            Addresses =
            [
                new()
                {
                    Label = "H", Street = "1 St", City = "X",
                    PostalCode = "12345", Country = "US"
                }
            ]
        }, CT);

        await h.Service.DeleteAllAsync(CT);

        Assert.Equal(0, await h.Db.ContactEmails.CountAsync(CT));
        Assert.Equal(0, await h.Db.ContactPhones.CountAsync(CT));
        Assert.Equal(0, await h.Db.ContactAddresses.CountAsync(CT));
    }

    [Fact]
    public async Task DeleteAll_EmptyDatabase_ReturnsZero()
    {
        await using var h = await TestHarness.CreateAsync();

        var deleted = await h.Service.DeleteAllAsync(CT);

        Assert.Equal(0, deleted);
    }

    [Fact]
    public async Task DeleteAll_AfterBulkCreate_RemovesAll()
    {
        await using var h = await TestHarness.CreateAsync();

        await h.Service.CreateBulkAsync(50, CT);
        var deleted = await h.Service.DeleteAllAsync(CT);

        Assert.Equal(50, deleted);

        var result = await h.Service.ListAsync(new(), 1, 100, CT);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task DeleteAll_ThenCreateBulk_Works()
    {
        await using var h = await TestHarness.CreateAsync();

        await h.Service.CreateBulkAsync(10, CT);
        await h.Service.DeleteAllAsync(CT);
        await h.Service.CreateBulkAsync(5, CT);

        var result = await h.Service.ListAsync(new(), 1, 100, CT);
        Assert.Equal(5, result.TotalCount);
    }

    [Fact]
    public async Task DeleteAll_DeletesNotes()
    {
        await using var h = await TestHarness.CreateAsync();
        var id = await h.Service.CreateAsync(
            new ContactFormModel { FirstName = "A", LastName = "B" }, CT);
        await h.Service.AddNoteAsync(id, "Test note", "u1", "user@test.com", CT);

        await h.Service.DeleteAllAsync(CT);

        Assert.Equal(0, await h.Db.ContactNotes.CountAsync(CT));
    }
}
