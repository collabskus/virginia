using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Virginia.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole, string>(options)
{
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<ContactEmail> ContactEmails => Set<ContactEmail>();
    public DbSet<ContactPhone> ContactPhones => Set<ContactPhone>();
    public DbSet<ContactAddress> ContactAddresses => Set<ContactAddress>();
    public DbSet<ContactNote> ContactNotes => Set<ContactNote>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Contact>(entity =>
        {
            entity.HasIndex(c => new { c.LastName, c.FirstName });

            entity.HasMany(c => c.Emails)
                .WithOne(e => e.Contact)
                .HasForeignKey(e => e.ContactId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(c => c.Phones)
                .WithOne(p => p.Contact)
                .HasForeignKey(p => p.ContactId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(c => c.Addresses)
                .WithOne(a => a.Contact)
                .HasForeignKey(a => a.ContactId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(c => c.Notes)
                .WithOne(n => n.Contact)
                .HasForeignKey(n => n.ContactId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ContactEmail>(entity =>
        {
            entity.HasIndex(e => e.Address);
        });

        builder.Entity<ContactPhone>(entity =>
        {
            entity.HasIndex(p => p.Number);
        });

        builder.Entity<ContactAddress>(entity =>
        {
            entity.HasIndex(a => new { a.City, a.State });
        });

        builder.Entity<ContactNote>(entity =>
        {
            entity.HasIndex(n => n.ContactId);
        });
    }
}
