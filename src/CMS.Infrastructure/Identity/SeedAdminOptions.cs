namespace CMS.Infrastructure.Identity;

public class SeedAdminOptions
{
    public const string SectionName = "Seed:Admin";

    public string Email { get; set; } = "admin@local.test";
    public string Password { get; set; } = string.Empty;
    public string? FullName { get; set; } = "مدیر سیستم";

    /// <summary>
    /// When true, resets the seed admin password on every startup (intended for local Development only).
    /// </summary>
    public bool SyncPasswordOnStartup { get; set; }
}
