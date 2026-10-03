using Microsoft.AspNetCore.Identity;

namespace CMS.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }

    /// <summary>Optional custom avatar URL; when empty, UI falls back to Gravatar.</summary>
    public string? AvatarUrl { get; set; }

    public bool IsProfileComplete => !string.IsNullOrWhiteSpace(FullName);
}

