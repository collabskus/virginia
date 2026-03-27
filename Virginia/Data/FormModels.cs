using System.ComponentModel.DataAnnotations;

namespace Virginia.Data;

// ─── Contact ─────────────────────────────────────────────────────────────────

public sealed class ContactFormModel
{
    [Required(ErrorMessage = "First name is required.")]
    [MaxLength(100, ErrorMessage = "First name cannot exceed 100 characters.")]
    public string FirstName { get; set; } = "";

    [Required(ErrorMessage = "Last name is required.")]
    [MaxLength(100, ErrorMessage = "Last name cannot exceed 100 characters.")]
    public string LastName { get; set; } = "";

    public List<EmailFormModel> Emails { get; set; } = [];
    public List<PhoneFormModel> Phones { get; set; } = [];
    public List<AddressFormModel> Addresses { get; set; } = [];

    public static ContactFormModel FromDetail(ContactDetailDto dto) => new()
    {
        FirstName = dto.FirstName,
        LastName = dto.LastName,
        Emails = [.. dto.Emails.Select(e => new EmailFormModel { Id = e.Id, Label = e.Label, Address = e.Address })],
        Phones = [.. dto.Phones.Select(p => new PhoneFormModel { Id = p.Id, Label = p.Label, Number = p.Number })],
        Addresses = [.. dto.Addresses
            .Select(a => new AddressFormModel
            {
                Id = a.Id, Label = a.Label, Street = a.Street,
                City = a.City, State = a.State,
                PostalCode = a.PostalCode, Country = a.Country
            })]
    };
}

// ─── Email ───────────────────────────────────────────────────────────────────

public sealed class EmailFormModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Label is required.")]
    [MaxLength(50)]
    public string Label { get; set; } = "Personal";

    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address format.")]
    [MaxLength(254, ErrorMessage = "Email cannot exceed 254 characters.")]
    public string Address { get; set; } = "";
}

// ─── Phone ───────────────────────────────────────────────────────────────────

public sealed class PhoneFormModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Label is required.")]
    [MaxLength(50)]
    public string Label { get; set; } = "Mobile";

    [Required(ErrorMessage = "Phone number is required.")]
    [RegularExpression(
        @"^[\+]?[\d\s\-\(\)\.]{7,30}$",
        ErrorMessage = "Enter a valid phone number (digits, spaces, dashes, parens).")]
    [MaxLength(30)]
    public string Number { get; set; } = "";
}

// ─── Address ─────────────────────────────────────────────────────────────────

public sealed class AddressFormModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Label is required.")]
    [MaxLength(50)]
    public string Label { get; set; } = "Home";

    [Required(ErrorMessage = "Street is required.")]
    [MaxLength(200, ErrorMessage = "Street cannot exceed 200 characters.")]
    public string Street { get; set; } = "";

    [Required(ErrorMessage = "City is required.")]
    [MaxLength(100, ErrorMessage = "City cannot exceed 100 characters.")]
    public string City { get; set; } = "";

    [MaxLength(100)]
    public string State { get; set; } = "";

    [Required(ErrorMessage = "Postal code is required.")]
    [RegularExpression(
        @"^[a-zA-Z0-9\s\-]{3,20}$",
        ErrorMessage = "Invalid postal code (3–20 alphanumeric characters, spaces, or dashes).")]
    [MaxLength(20)]
    public string PostalCode { get; set; } = "";

    [Required(ErrorMessage = "Country is required.")]
    [MaxLength(100)]
    public string Country { get; set; } = "US";
}

// ─── Login ───────────────────────────────────────────────────────────────────

public sealed class LoginFormModel
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Password is required.")]
    public string Password { get; set; } = "";

    public bool RememberMe { get; set; }
}

// ─── Register ────────────────────────────────────────────────────────────────

public sealed class RegisterFormModel
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    [MaxLength(256)]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
    [MaxLength(100)]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Please confirm your password.")]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = "";
}

// ─── Change Password ─────────────────────────────────────────────────────────

public sealed class ChangePasswordFormModel
{
    [Required(ErrorMessage = "Current password is required.")]
    public string CurrentPassword { get; set; } = "";

    [Required(ErrorMessage = "New password is required.")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
    [MaxLength(100)]
    public string NewPassword { get; set; } = "";

    [Required(ErrorMessage = "Please confirm your new password.")]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
    public string ConfirmNewPassword { get; set; } = "";
}
