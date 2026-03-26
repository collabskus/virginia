using Microsoft.EntityFrameworkCore;

namespace Virginia.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<ContactEmail> ContactEmails => Set<ContactEmail>();
    public DbSet<ContactPhone> ContactPhones => Set<ContactPhone>();
    public DbSet<ContactAddress> ContactAddresses => Set<ContactAddress>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Contact>(entity =>
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
        });

        modelBuilder.Entity<ContactEmail>(entity =>
        {
            entity.HasIndex(e => e.Address);
        });

        modelBuilder.Entity<ContactPhone>(entity =>
        {
            entity.HasIndex(p => p.Number);
        });

        modelBuilder.Entity<ContactAddress>(entity =>
        {
            entity.HasIndex(a => new { a.City, a.State });
        });
    }
}
