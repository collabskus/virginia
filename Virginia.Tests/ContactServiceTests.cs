using Microsoft.EntityFrameworkCore;
using Virginia.Data;
using Xunit;

namespace Virginia.Tests;

public sealed class ContactServiceTests
{
    // ─── Create ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ReturnsPositiveId()
    {
        await using var h = await TestHarness.CreateAsync();

        var id = await h.Service.CreateAsync(
            new ContactFormModel { FirstName = "Jane", LastName = "Doe" });

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

        var id = await h.Service.CreateAsync(form);
        var detail = await h.Service.GetAsync(id);

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
            new ContactFormModel { FirstName = "Solo", LastName = "Contact" });
        var detail = await h.Service.GetAsync(id);

        Assert.NotNull(detail);
        Assert.Empty(detail.Emails);
        Assert.Empty(detail.Phones);
        Assert.Empty(detail.Addresses);
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

        var id = await h.Service.CreateAsync(form);
        var detail = await h.Service.GetAsync(id);

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
            new ContactFormModel { FirstName = "T", LastName = "S" });
        var detail = await h.Service.GetAsync(id);

        Assert.True(detail!.CreatedAtUtc >= before);
        Assert.True(detail.UpdatedAtUtc >= before);
        Assert.Equal(detail.CreatedAtUtc, detail.UpdatedAtUtc);
    }

    // ─── Get ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Get_NonExistent_ReturnsNull()
    {
        await using var h = await TestHarness.CreateAsync();

        Assert.Null(await h.Service.GetAsync(9999));
    }

    // ─── Update ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_ChangesName()
    {
        await using var h = await TestHarness.CreateAsync();
        var id = await h.Service.CreateAsync(
            new ContactFormModel { FirstName = "Old", LastName = "Name" });

        await h.Service.UpdateAsync(id,
            new ContactFormModel { FirstName = "New", LastName = "Name" });

        var detail = await h.Service.GetAsync(id);
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
        var id = await h.Service.CreateAsync(form);
        var detail = await h.Service.GetAsync(id);

        // Keep email A (by ID), remove B, add C
        var updateForm = new ContactFormModel
        {
            FirstName = "Test", LastName = "User",
            Emails =
            [
                new() { Id = detail!.Emails[0].Id, Label = "A2", Address = "a2@t.com" },
                new() { Label = "C", Address = "c@t.com" }
            ]
        };

        await h.Service.UpdateAsync(id, updateForm);
        var updated = await h.Service.GetAsync(id);

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
                new ContactFormModel { FirstName = "X", LastName = "Y" }));
    }

    [Fact]
    public async Task Update_BumpsTimestamp()
    {
        await using var h = await TestHarness.CreateAsync();
        var id = await h.Service.CreateAsync(
            new ContactFormModel { FirstName = "T", LastName = "S" });
        var before = (await h.Service.GetAsync(id))!.UpdatedAtUtc;

        await Task.Delay(50); // ensure clock ticks
        await h.Service.UpdateAsync(id,
            new ContactFormModel { FirstName = "T2", LastName = "S" });

        var after = (await h.Service.GetAsync(id))!.UpdatedAtUtc;
        Assert.True(after > before);
    }

    // ─── Delete ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_RemovesContact()
    {
        await using var h = await TestHarness.CreateAsync();
        var id = await h.Service.CreateAsync(
            new ContactFormModel { FirstName = "Gone", LastName = "Soon" });

        await h.Service.DeleteAsync(id);

        Assert.Null(await h.Service.GetAsync(id));
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
        var id = await h.Service.CreateAsync(form);

        await h.Service.DeleteAsync(id);

        Assert.Equal(0, await h.Db.ContactEmails.CountAsync());
        Assert.Equal(0, await h.Db.ContactPhones.CountAsync());
        Assert.Equal(0, await h.Db.ContactAddresses.CountAsync());
    }

    [Fact]
    public async Task Delete_NonExistent_DoesNotThrow()
    {
        await using var h = await TestHarness.CreateAsync();

        // Should not throw
        await h.Service.DeleteAsync(9999);
    }

    // ─── List / Filtering ────────────────────────────────────────────────

    [Fact]
    public async Task List_FilterByName_MatchesPartial()
    {
        await using var h = await TestHarness.CreateAsync();
        await h.Service.CreateAsync(new() { FirstName = "Alice", LastName = "Johnson" });
        await h.Service.CreateAsync(new() { FirstName = "Bob", LastName = "Jones" });
        await h.Service.CreateAsync(new() { FirstName = "Charlie", LastName = "Brown" });

        var result = await h.Service.ListAsync(new(Name: "Jo"), 1, 50);

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
        });
        await h.Service.CreateAsync(new() { FirstName = "C", LastName = "D" });

        var result = await h.Service.ListAsync(new(Email: "alice"), 1, 50);

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
        });
        await h.Service.CreateAsync(new() { FirstName = "C", LastName = "D" });

        var result = await h.Service.ListAsync(new(Phone: "0199"), 1, 50);

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
        });
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
        });

        var result = await h.Service.ListAsync(new(City: "Newport"), 1, 50);

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
        });
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
        });

        var result = await h.Service.ListAsync(new(State: "VA"), 1, 50);

        Assert.Single(result.Items);
        Assert.Equal("VA", result.Items[0].FirstName);
    }

    [Fact]
    public async Task List_FilterHasPhoto_True()
    {
        await using var h = await TestHarness.CreateAsync();
        var id1 = await h.Service.CreateAsync(new() { FirstName = "With", LastName = "Photo" });
        await h.Service.CreateAsync(new() { FirstName = "No", LastName = "Photo" });
        await h.Service.SetProfilePictureAsync(id1, [0xFF, 0xD8], "image/jpeg");

        var result = await h.Service.ListAsync(new(HasPhoto: true), 1, 50);

        Assert.Single(result.Items);
        Assert.Equal("With", result.Items[0].FirstName);
    }

    [Fact]
    public async Task List_FilterHasPhoto_False()
    {
        await using var h = await TestHarness.CreateAsync();
        var id1 = await h.Service.CreateAsync(new() { FirstName = "With", LastName = "Photo" });
        await h.Service.CreateAsync(new() { FirstName = "No", LastName = "Photo" });
        await h.Service.SetProfilePictureAsync(id1, [0xFF, 0xD8], "image/jpeg");

        var result = await h.Service.ListAsync(new(HasPhoto: false), 1, 50);

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
        });
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
        });

        var result = await h.Service.ListAsync(
            new(Name: "Alice", City: "Richmond"), 1, 50);

        Assert.Single(result.Items);
        Assert.Equal("Johnson", result.Items[0].LastName);
    }

    // ─── Pagination ──────────────────────────────────────────────────────

    [Fact]
    public async Task List_Pagination_WorksCorrectly()
    {
        await using var h = await TestHarness.CreateAsync();
        for (var i = 0; i < 25; i++)
            await h.Service.CreateAsync(new() { FirstName = $"U{i:D2}", LastName = "Test" });

        var p1 = await h.Service.ListAsync(new(), 1, 10);
        var p2 = await h.Service.ListAsync(new(), 2, 10);
        var p3 = await h.Service.ListAsync(new(), 3, 10);

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
        await h.Service.CreateAsync(new() { FirstName = "Zoe", LastName = "Adams" });
        await h.Service.CreateAsync(new() { FirstName = "Amy", LastName = "Adams" });
        await h.Service.CreateAsync(new() { FirstName = "Bob", LastName = "Baker" });

        var result = await h.Service.ListAsync(new(), 1, 50);

        Assert.Equal("Amy", result.Items[0].FirstName);
        Assert.Equal("Zoe", result.Items[1].FirstName);
        Assert.Equal("Bob", result.Items[2].FirstName);
    }

    // ─── Profile pictures ────────────────────────────────────────────────

    [Fact]
    public async Task ProfilePicture_SetAndRetrieve()
    {
        await using var h = await TestHarness.CreateAsync();
        var id = await h.Service.CreateAsync(new() { FirstName = "P", LastName = "T" });
        byte[] data = [0x89, 0x50, 0x4E, 0x47];

        await h.Service.SetProfilePictureAsync(id, data, "image/png");
        var photo = await h.Service.GetProfilePictureAsync(id);

        Assert.NotNull(photo);
        Assert.Equal(data, photo.Data);
        Assert.Equal("image/png", photo.ContentType);
    }

    [Fact]
    public async Task ProfilePicture_Remove()
    {
        await using var h = await TestHarness.CreateAsync();
        var id = await h.Service.CreateAsync(new() { FirstName = "P", LastName = "T" });
        await h.Service.SetProfilePictureAsync(id, [0xFF], "image/jpeg");

        await h.Service.RemoveProfilePictureAsync(id);

        Assert.Null(await h.Service.GetProfilePictureAsync(id));
    }

    [Fact]
    public async Task ProfilePicture_GetNonExistent_ReturnsNull()
    {
        await using var h = await TestHarness.CreateAsync();
        var id = await h.Service.CreateAsync(new() { FirstName = "P", LastName = "T" });

        Assert.Null(await h.Service.GetProfilePictureAsync(id));
    }

    [Fact]
    public async Task ProfilePicture_SetForNonExistent_Throws()
    {
        await using var h = await TestHarness.CreateAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            h.Service.SetProfilePictureAsync(9999, [0xFF], "image/jpeg"));
    }
}
```

---

## FILE: Virginia.Tests/FormValidationTests.cs

```csharp
using System.ComponentModel.DataAnnotations;
using Virginia.Data;
using Xunit;
using ValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;

namespace Virginia.Tests;

public sealed class FormValidationTests
{
    private static List<ValidationResult> Validate(object model)
    {
        var ctx = new ValidationContext(model);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, ctx, results, validateAllProperties: true);
        return results;
    }

    // ─── ContactFormModel ────────────────────────────────────────────────

    [Fact]
    public void Contact_RequiresFirstAndLastName()
    {
        var errors = Validate(new ContactFormModel { FirstName = "", LastName = "" });
        Assert.Contains(errors, e => e.MemberNames.Contains("FirstName"));
        Assert.Contains(errors, e => e.MemberNames.Contains("LastName"));
    }

    [Fact]
    public void Contact_ValidForm_Passes()
    {
        var errors = Validate(new ContactFormModel { FirstName = "A", LastName = "B" });
        Assert.Empty(errors);
    }

    [Fact]
    public void Contact_MaxLength_Enforced()
    {
        var errors = Validate(new ContactFormModel
        {
            FirstName = new string('A', 101),
            LastName = "B"
        });
        Assert.Contains(errors, e => e.MemberNames.Contains("FirstName"));
    }

    // ─── EmailFormModel ──────────────────────────────────────────────────

    [Fact]
    public void Email_RequiresValidAddress()
    {
        var errors = Validate(new EmailFormModel { Label = "Work", Address = "not-email" });
        Assert.Contains(errors, e => e.MemberNames.Contains("Address"));
    }

    [Fact]
    public void Email_ValidAddress_Passes()
    {
        var errors = Validate(new EmailFormModel { Label = "Work", Address = "a@b.com" });
        Assert.Empty(errors);
    }

    [Fact]
    public void Email_EmptyLabel_Fails()
    {
        var errors = Validate(new EmailFormModel { Label = "", Address = "a@b.com" });
        Assert.Contains(errors, e => e.MemberNames.Contains("Label"));
    }

    // ─── PhoneFormModel ──────────────────────────────────────────────────

    [Fact]
    public void Phone_EmptyNumber_Fails()
    {
        var errors = Validate(new PhoneFormModel { Label = "M", Number = "" });
        Assert.Contains(errors, e => e.MemberNames.Contains("Number"));
    }

    [Theory]
    [InlineData("555-0100")]
    [InlineData("(757) 555-0100")]
    [InlineData("+1 757 555 0100")]
    [InlineData("757.555.0100")]
    public void Phone_ValidFormats_Pass(string number)
    {
        var errors = Validate(new PhoneFormModel { Label = "M", Number = number });
        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("12")]
    [InlineData("!@#$%")]
    public void Phone_InvalidFormats_Fail(string number)
    {
        var errors = Validate(new PhoneFormModel { Label = "M", Number = number });
        Assert.Contains(errors, e => e.MemberNames.Contains("Number"));
    }

    // ─── AddressFormModel ────────────────────────────────────────────────

    [Fact]
    public void Address_RequiredFieldsMissing_Fails()
    {
        var errors = Validate(new AddressFormModel
        {
            Label = "Home", Street = "", City = "",
            PostalCode = "", Country = ""
        });

        Assert.Contains(errors, e => e.MemberNames.Contains("Street"));
        Assert.Contains(errors, e => e.MemberNames.Contains("City"));
        Assert.Contains(errors, e => e.MemberNames.Contains("PostalCode"));
        Assert.Contains(errors, e => e.MemberNames.Contains("Country"));
    }

    [Fact]
    public void Address_ValidData_Passes()
    {
        var errors = Validate(new AddressFormModel
        {
            Label = "Home", Street = "123 Main St", City = "Newport News",
            State = "VA", PostalCode = "23601", Country = "US"
        });
        Assert.Empty(errors);
    }

    [Fact]
    public void Address_StateOptional()
    {
        var errors = Validate(new AddressFormModel
        {
            Label = "Home", Street = "1 St", City = "X",
            State = "", PostalCode = "12345", Country = "US"
        });
        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("23601")]
    [InlineData("23601-1234")]
    [InlineData("SW1A 1AA")]
    [InlineData("H2X 3Y7")]
    public void Address_ValidPostalCodes_Pass(string code)
    {
        var errors = Validate(new AddressFormModel
        {
            Label = "H", Street = "1 St", City = "X",
            PostalCode = code, Country = "US"
        });
        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("!!!")]
    [InlineData("ab")]
    [InlineData("")]
    public void Address_InvalidPostalCodes_Fail(string code)
    {
        var errors = Validate(new AddressFormModel
        {
            Label = "H", Street = "1 St", City = "X",
            PostalCode = code, Country = "US"
        });
        Assert.Contains(errors, e => e.MemberNames.Contains("PostalCode"));
    }
}
