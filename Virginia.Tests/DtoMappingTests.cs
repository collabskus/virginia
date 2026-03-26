using Virginia.Data;
using Xunit;

namespace Virginia.Tests;

public sealed class DtoMappingTests
{
    [Fact]
    public void ContactFormModel_FromDetail_MapsCorrectly()
    {
        var dto = new ContactDetailDto(
            Id: 1, FirstName: "Alice", LastName: "Smith",
            HasPhoto: true, ProfilePictureContentType: "image/png",
            CreatedAtUtc: DateTime.UtcNow, UpdatedAtUtc: DateTime.UtcNow,
            Emails: [new(1, "Work", "a@b.com"), new(2, "Home", "a@c.com")],
            Phones: [new(1, "Mobile", "555-0100")],
            Addresses:
            [
                new(1, "Home", "123 Main", "Richmond", "VA", "23220", "US")
            ]);

        var form = ContactFormModel.FromDetail(dto);

        Assert.Equal("Alice", form.FirstName);
        Assert.Equal("Smith", form.LastName);
        Assert.Equal(2, form.Emails.Count);
        Assert.Equal(1, form.Emails[0].Id);
        Assert.Equal("Work", form.Emails[0].Label);
        Assert.Single(form.Phones);
        Assert.Single(form.Addresses);
        Assert.Equal("Richmond", form.Addresses[0].City);
    }

    [Fact]
    public void PagedResult_CalculatesProperties()
    {
        var result = new PagedResult<int>([1, 2, 3], TotalCount: 25, Page: 2, PageSize: 10);

        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasPrevious);
        Assert.True(result.HasNext);
    }

    [Fact]
    public void PagedResult_FirstPage_NoPrevious()
    {
        var result = new PagedResult<int>([1], TotalCount: 5, Page: 1, PageSize: 10);

        Assert.False(result.HasPrevious);
        Assert.False(result.HasNext);
    }
}
