using System.ComponentModel.DataAnnotations;

namespace Virginia.Data;

// ─── Aggregate Root ──────────────────────────────────────────────────────────

public sealed class Contact
{
    public int Id { get; set; }

    [MaxLength(100)]
    public required string FirstName { get; set; }

    [MaxLength(100)]
    public required string LastName { get; set; }

    public byte[]? ProfilePicture { get; set; }

    [MaxLength(50)]
    public string? ProfilePictureContentType { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public List<ContactEmail> Emails { get; set; } = [];
    public List<ContactPhone> Phones { get; set; } = [];
    public List<ContactAddress> Addresses { get; set; } = [];
    public List<ContactNote> Notes { get; set; } = [];

    public string FullName => $"{FirstName} {LastName}".Trim();
}

// ─── Child Entities ──────────────────────────────────────────────────────────

public sealed class ContactEmail
{
    public int Id { get; set; }
    public int ContactId { get; set; }

    [MaxLength(50)]
    public required string Label { get; set; }

    [MaxLength(254)]
    public required string Address { get; set; }

    public Contact Contact { get; set; } = null!;
}

public sealed class ContactPhone
{
    public int Id { get; set; }
    public int ContactId { get; set; }

    [MaxLength(50)]
    public required string Label { get; set; }

    [MaxLength(30)]
    public required string Number { get; set; }

    public Contact Contact { get; set; } = null!;
}

public sealed class ContactAddress
{
    public int Id { get; set; }
    public int ContactId { get; set; }

    [MaxLength(50)]
    public required string Label { get; set; }

    [MaxLength(200)]
    public required string Street { get; set; }

    [MaxLength(100)]
    public required string City { get; set; }

    [MaxLength(100)]
    public string State { get; set; } = "";

    [MaxLength(20)]
    public required string PostalCode { get; set; }

    [MaxLength(100)]
    public required string Country { get; set; }

    public Contact Contact { get; set; } = null!;
}

public sealed class ContactNote
{
    public int Id { get; set; }
    public int ContactId { get; set; }

    [MaxLength(4000)]
    public required string Content { get; set; }

    [MaxLength(450)]
    public required string CreatedByUserId { get; set; }

    [MaxLength(256)]
    public required string CreatedByUserName { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public Contact Contact { get; set; } = null!;
}
