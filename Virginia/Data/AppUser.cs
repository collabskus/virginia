using Microsoft.AspNetCore.Identity;

namespace Virginia.Data;

public sealed class AppUser : IdentityUser
{
    public bool IsApproved { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
