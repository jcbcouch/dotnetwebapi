using Microsoft.AspNetCore.Identity;

namespace webapi.Models;

public class ApplicationUser : IdentityUser
{
    // Add custom properties here if needed
    public string? DisplayName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
