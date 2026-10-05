namespace CMS.Infrastructure.Cache;

public sealed class CacheOptions
{
    public const string SectionName = "Cache";

    /// <summary>
    /// Redis connection string for shared cache and Data Protection keys.
    /// Required in Production; optional in Development/Testing (falls back to memory).
    /// </summary>
    public string? RedisConnectionString { get; set; }

    /// <summary>
    /// TTL in seconds for public content query cache (lists, details, popups).
    /// </summary>
    public int PublicContentSeconds { get; set; } = 90;
}
