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
