namespace CMS.Application.Seo;

public enum SitemapChangeFrequency
{
    Always,
    Hourly,
    Daily,
    Weekly,
    Monthly,
    Yearly,
    Never
}

/// <summary>
/// One public URL for the XML sitemap. Only published/active content should be contributed.
/// </summary>
public sealed record SitemapUrlEntry(
    string RelativePath,
    DateTime? LastModifiedUtc = null,
    SitemapChangeFrequency? ChangeFrequency = null,
    double? Priority = null);

public interface ISitemapUrlProvider
{
    /// <summary>
    /// URL segment for this provider's sub-sitemap (e.g. "blog" → /sitemaps/blog).
    /// </summary>
    string Segment { get; }

    /// <summary>
    /// Feature flag required for this provider (e.g. Blog). Null means always include.
    /// </summary>
    string? RequiredFeature { get; }

    Task<IReadOnlyList<SitemapUrlEntry>> GetEntriesAsync(CancellationToken cancellationToken = default);
}

public interface ISitemapService
{
    Task<string> BuildIndexXmlAsync(string baseUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds a urlset for one segment, or null if the segment is unknown or disabled.
    /// </summary>
    Task<string?> BuildSegmentXmlAsync(
        string segment,
        string baseUrl,
        CancellationToken cancellationToken = default);
}
