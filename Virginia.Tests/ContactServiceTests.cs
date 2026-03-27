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
            FirstName = "John", LastName = "Smith",
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
        Assert.Empty(detail.Notes);
    }

    [Fact]
    public async Task Create_TrimsWhitespace()
    {
        await using var h = await TestHarness.CreateAsync();
        var form = new ContactFormModel
        {
            FirstName = "  Alice  ", LastName = "  Smith  ",
            Emails = [new() { Label = " Work ", Address = " a@b.com " }]
        };

        var id = await h.Service.CreateAsync(form, CT);
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
    public async Task Update_ChangesName()
    {
        await using var h = await TestHarness.CreateAsync();
        var id = await h.Service.CreateAsync(
            new ContactFormModel { FirstName = "Old", LastName = "Name" }, CT);

        await h.Service.UpdateAsync(id,
            new ContactFormModel { FirstName = "New", LastName = "Name" }, CT);

        var detail = await h.Service.GetAsync(id, CT);
        Assert.Equal("New", detail!.FirstName);
    }

    [Fact]
    public async Task Update_AddsAndRemovesEmails()
    {
        await using var h = await TestHarness.CreateAsync();
        var form = new ContactFormModel
        {
            FirstName = "Test", LastName = "User",
            Emails =
            [
                new() { Label = "A", Address = "a@t.com" },
                new() { Label = "B", Address = "b@t.com" }
            ]
        };
        var id = await h.Service.CreateAsync(form, CT);
        var detail = await h.Service.GetAsync(id, CT);

        var updateForm = new ContactFormModel
        {
            FirstName = "Test", LastName = "User",
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
            FirstName = "P", LastName = "C",
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
        await h.Service.AddNoteAsync(id, "test note", "user1", "user@test.com", CT);

        await h.Service.DeleteAsync(id, CT);

        Assert.Equal(0, await h.Db.ContactEmails.CountAsync(CT));
        Assert.Equal(0, await h.Db.ContactPhones.CountAsync(CT));
        Assert.Equal(0, await h.Db.ContactAddresses.CountAsync(CT));
        Assert.Equal(0, await h.Db.ContactNotes.CountAsync(CT));
    }

    [Fact]
    public async Task Delete_NonExistent_DoesNotThrow()
    {
        await using var h = await TestHarness.CreateAsync();

        await h.Service.DeleteAsync(9999, CT);
    }

    // ─── List / Filtering ────────────────────────────────────────────────

    [Fact]
    public async Task List_FilterByName_MatchesPartial()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.Service.CreateAsync(new() { FirstName = "Alice", LastName = "Johnson" }, CT);
        await h.Service.CreateAsync(new() { FirstName = "Bob", LastName = "Jones" }, CT);
        await h.Service.CreateAsync(new() { FirstName = "Charlie", LastName = "Brown" }, CT);

        var result = await h.Service.ListAsync(new(Name: "Jo"), 1, 50, CT);

        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task List_FilterByEmail()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.Service.CreateAsync(new()
        {
            FirstName = "A", LastName = "B",
            Emails = [new() { Label = "W", Address = "alice@example.com" }]
        }, CT);
        await h.Service.CreateAsync(new() { FirstName = "C", LastName = "D" }, CT);

        var result = await h.Service.ListAsync(new(Email: "alice"), 1, 50, CT);

        Assert.Single(result.Items);
        Assert.Equal("A", result.Items[0].FirstName);
    }

    [Fact]
    public async Task List_FilterByPhone()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.Service.CreateAsync(new()
        {
            FirstName = "A", LastName = "B",
            Phones = [new() { Label = "M", Number = "757-555-0199" }]
        }, CT);
        await h.Service.CreateAsync(new() { FirstName = "C", LastName = "D" }, CT);

        var result = await h.Service.ListAsync(new(Phone: "0199"), 1, 50, CT);

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task List_FilterByCity()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.Service.CreateAsync(new()
        {
            FirstName = "A", LastName = "B",
            Addresses =
            [
                new()
                {
                    Label = "H", Street = "1 St", City = "Newport News",
                    State = "VA", PostalCode = "23601", Country = "US"
                }
            ]
        }, CT);
        await h.Service.CreateAsync(new()
        {
            FirstName = "C", LastName = "D",
            Addresses =
            [
                new()
                {
                    Label = "H", Street = "2 St", City = "Richmond",
                    State = "VA", PostalCode = "23220", Country = "US"
                }
            ]
        }, CT);

        var result = await h.Service.ListAsync(new(City: "Newport"), 1, 50, CT);

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task List_FilterByState()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.Service.CreateAsync(new()
        {
            FirstName = "VA", LastName = "Person",
            Addresses =
            [
                new()
                {
                    Label = "H", Street = "1 St", City = "A",
                    State = "VA", PostalCode = "23601", Country = "US"
                }
            ]
        }, CT);
        await h.Service.CreateAsync(new()
        {
            FirstName = "CA", LastName = "Person",
            Addresses =
            [
                new()
                {
                    Label = "H", Street = "2 St", City = "B",
                    State = "CA", PostalCode = "90210", Country = "US"
                }
            ]
        }, CT);

        var result = await h.Service.ListAsync(new(State: "VA"), 1, 50, CT);

        Assert.Single(result.Items);
        Assert.Equal("VA", result.Items[0].FirstName);
    }

    [Fact]
    public async Task List_FilterHasPhoto_True()
    {
        await using var h = await TestHarness.CreateAsync();
        var id1 = await h.Service.CreateAsync(new() { FirstName = "With", LastName = "Photo" }, CT);
        await h.Service.CreateAsync(new() { FirstName = "No", LastName = "Photo" }, CT);
        await h.Service.SetProfilePictureAsync(id1, [0xFF, 0xD8], "image/jpeg", CT);

        var result = await h.Service.ListAsync(new(HasPhoto: true), 1, 50, CT);

        Assert.Single(result.Items);
        Assert.Equal("With", result.Items[0].FirstName);
    }

    [Fact]
    public async Task List_FilterHasPhoto_False()
    {
        await using var h = await TestHarness.CreateAsync();
        var id1 = await h.Service.CreateAsync(new() { FirstName = "With", LastName = "Photo" }, CT);
        await h.Service.CreateAsync(new() { FirstName = "No", LastName = "Photo" }, CT);
        await h.Service.SetProfilePictureAsync(id1, [0xFF, 0xD8], "image/jpeg", CT);

        var result = await h.Service.ListAsync(new(HasPhoto: false), 1, 50, CT);

        Assert.Single(result.Items);
        Assert.Equal("No", result.Items[0].FirstName);
    }

    [Fact]
    public async Task List_CombinedFilters()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.Service.CreateAsync(new()
        {
            FirstName = "Alice", LastName = "Johnson",
            Addresses =
            [
                new()
                {
                    Label = "H", Street = "1 St", City = "Richmond",
                    State = "VA", PostalCode = "23220", Country = "US"
                }
            ]
        }, CT);
        await h.Service.CreateAsync(new()
        {
            FirstName = "Alice", LastName = "Jones",
            Addresses =
            [
                new()
                {
                    Label = "H", Street = "2 St", City = "Norfolk",
                    State = "VA", PostalCode = "23510", Country = "US"
                }
            ]
        }, CT);

        var result = await h.Service.ListAsync(
            new(Name: "Alice", City: "Richmond"), 1, 50, CT);

        Assert.Single(result.Items);
        Assert.Equal("Johnson", result.Items[0].LastName);
    }

    // ─── Pagination ──────────────────────────────────────────────────────

    [Fact]
    public async Task List_Pagination_WorksCorrectly()
    {
        await using var h = await TestHarness.CreateAsync();
        for (var i = 0; i < 25; i++)
            await h.Service.CreateAsync(new() { FirstName = $"U{i:D2}", LastName = "Test" }, CT);

        var p1 = await h.Service.ListAsync(new(), 1, 10, CT);
        var p2 = await h.Service.ListAsync(new(), 2, 10, CT);
        var p3 = await h.Service.ListAsync(new(), 3, 10, CT);

        Assert.Equal(25, p1.TotalCount);
        Assert.Equal(3, p1.TotalPages);
        Assert.Equal(10, p1.Items.Count);
        Assert.Equal(10, p2.Items.Count);
        Assert.Equal(5, p3.Items.Count);

        Assert.True(p1.HasNext);
        Assert.False(p1.HasPrevious);
        Assert.True(p2.HasPrevious);
        Assert.True(p2.HasNext);
        Assert.True(p3.HasPrevious);
        Assert.False(p3.HasNext);
    }

    [Fact]
    public async Task List_OrdersByLastNameThenFirst()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.Service.CreateAsync(new() { FirstName = "Zoe", LastName = "Adams" }, CT);
        await h.Service.CreateAsync(new() { FirstName = "Amy", LastName = "Adams" }, CT);
        await h.Service.CreateAsync(new() { FirstName = "Bob", LastName = "Baker" }, CT);

        var result = await h.Service.ListAsync(new(), 1, 50, CT);

        Assert.Equal("Amy", result.Items[0].FirstName);
        Assert.Equal("Zoe", result.Items[1].FirstName);
        Assert.Equal("Bob", result.Items[2].FirstName);
    }

    // ─── Profile pictures ────────────────────────────────────────────────

    [Fact]
    public async Task ProfilePicture_SetAndRetrieve()
    {
        await using var h = await TestHarness.CreateAsync();
        var id = await h.Service.CreateAsync(new() { FirstName = "P", LastName = "T" }, CT);
        byte[] data = [0x89, 0x50, 0x4E, 0x47];

        await h.Service.SetProfilePictureAsync(id, data, "image/png", CT);
        var photo = await h.Service.GetProfilePictureAsync(id, CT);

        Assert.NotNull(photo);
        Assert.Equal(data, photo.Data);
        Assert.Equal("image/png", photo.ContentType);
    }

    [Fact]
    public async Task ProfilePicture_Remove()
    {
        await using var h = await TestHarness.CreateAsync();
        var id = await h.Service.CreateAsync(new() { FirstName = "P", LastName = "T" }, CT);
        await h.Service.SetProfilePictureAsync(id, [0xFF], "image/jpeg", CT);

        await h.Service.RemoveProfilePictureAsync(id, CT);

        Assert.Null(await h.Service.GetProfilePictureAsync(id, CT));
    }

    [Fact]
    public async Task ProfilePicture_GetNonExistent_ReturnsNull()
    {
        await using var h = await TestHarness.CreateAsync();
        var id = await h.Service.CreateAsync(new() { FirstName = "P", LastName = "T" }, CT);

        Assert.Null(await h.Service.GetProfilePictureAsync(id, CT));
    }

    [Fact]
    public async Task ProfilePicture_SetForNonExistent_Throws()
    {
        await using var h = await TestHarness.CreateAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            h.Service.SetProfilePictureAsync(9999, [0xFF], "image/jpeg", CT));
    }

    // ─── Case-insensitive filtering ──────────────────────────────────────

    [Theory]
    [InlineData("linc")]
    [InlineData("LINC")]
    [InlineData("Linc")]
    [InlineData("lInC")]
    public async Task List_FilterByName_CaseInsensitive(string term)
    {
        await using var h = await TestHarness.CreateAsync();
        await h.Service.CreateAsync(
            new() { FirstName = "Abraham", LastName = "Lincoln" }, CT);

        var result = await h.Service.ListAsync(new(Name: term), 1, 50, CT);

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task List_FilterByEmail_CaseInsensitive()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.Service.CreateAsync(new()
        {
            FirstName = "A", LastName = "B",
            Emails = [new() { Label = "W", Address = "Alice@Example.COM" }]
        }, CT);

        var result = await h.Service.ListAsync(new(Email: "alice@example"), 1, 50, CT);

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task List_FilterByCity_CaseInsensitive()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.Service.CreateAsync(new()
        {
            FirstName = "A", LastName = "B",
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
            FirstName = "A", LastName = "B",
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
}
