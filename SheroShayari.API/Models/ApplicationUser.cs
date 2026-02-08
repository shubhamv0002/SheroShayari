using Microsoft.AspNetCore.Identity;

namespace SheroShayari.API.Models;

/// <summary>
/// Extended IdentityUser with additional application-specific properties.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// User's full name.
    /// </summary>
    public string? FullName { get; set; }

    /// <summary>
    /// User's preferred language for Shayari generation.
    /// </summary>
    public string PreferredLanguage { get; set; } = "Hindi";

    /// <summary>
    /// Timestamp when the user was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property: Shayaris created by this user.
    /// </summary>
    public ICollection<Shayari> Shayaris { get; set; } = new List<Shayari>();
}
