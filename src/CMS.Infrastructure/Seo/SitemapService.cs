using CMS.Application.Seo;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.FeatureManagement;

namespace CMS.Infrastructure.Seo;

public sealed class SitemapService : ISitemapService
{
    private const string IndexCacheKeyPrefix = "cms:sitemap:index:";
    private const string SegmentCacheKeyPrefix = "cms:sitemap:segment:";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    private readonly IEnumerable<ISitemapUrlProvider> _providers;
    private readonly IFeatureManager _features;
    private readonly IMemoryCache _cache;

    public SitemapService(
        IEnumerable<ISitemapUrlProvider> providers,
        IFeatureManager features,
        IMemoryCache cache)
    {
        _providers = providers;
        _features = features;
        _cache = cache;
    }

    public async Task<string> BuildIndexXmlAsync(string baseUrl, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);

        var root = baseUrl.Trim().TrimEnd('/');
        var cacheKey = IndexCacheKeyPrefix + root.ToLowerInvariant();

        if (_cache.TryGetValue(cacheKey, out string? cached) && !string.IsNullOrEmpty(cached))
            return cached;

        var paths = new List<string>();
        foreach (var provider in await GetActiveProvidersAsync(cancellationToken))
        {
            var segment = NormalizeSegment(provider.Segment);
            paths.Add($"/sitemaps/{segment}");
        }

        paths.Sort(StringComparer.OrdinalIgnoreCase);
        MoveSegmentFirst(paths, "pages");

        var xml = SitemapIndexXmlBuilder.Build(root, paths);
        _cache.Set(cacheKey, xml, CacheDuration);
        return xml;
    }

    public async Task<string?> BuildSegmentXmlAsync(
        string segment,
        string baseUrl,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);
        if (string.IsNullOrWhiteSpace(segment))
            return null;

        var normalizedSegment = NormalizeSegment(segment);
        var root = baseUrl.Trim().TrimEnd('/');
        var cacheKey = SegmentCacheKeyPrefix + normalizedSegment + ":" + root.ToLowerInvariant();

        if (_cache.TryGetValue(cacheKey, out string? cached) && !string.IsNullOrEmpty(cached))
            return cached;

        var provider = (await GetActiveProvidersAsync(cancellationToken))
            .FirstOrDefault(p => string.Equals(NormalizeSegment(p.Segment), normalizedSegment, StringComparison.OrdinalIgnoreCase));

        if (provider is null)
            return null;

        var entries = await provider.GetEntriesAsync(cancellationToken);
        var xml = SitemapXmlBuilder.Build(root, entries);
        _cache.Set(cacheKey, xml, CacheDuration);
        return xml;
    }

    private async Task<IReadOnlyList<ISitemapUrlProvider>> GetActiveProvidersAsync(CancellationToken cancellationToken)
    {
        var active = new List<ISitemapUrlProvider>();
        foreach (var provider in _providers)
        {
            if (!string.IsNullOrWhiteSpace(provider.RequiredFeature)
                && !await _features.IsEnabledAsync(provider.RequiredFeature))
            {
                continue;
            }

            active.Add(provider);
        }

        return active;
    }

    private static string NormalizeSegment(string segment)
    {
        var trimmed = segment.Trim().Trim('/');
        if (trimmed.Length == 0)
            throw new InvalidOperationException("Sitemap segment must not be empty.");

        return trimmed.ToLowerInvariant();
    }

    private static void MoveSegmentFirst(List<string> paths, string segment)
    {
        var target = $"/sitemaps/{segment}";
        var index = paths.FindIndex(p => string.Equals(p, target, StringComparison.OrdinalIgnoreCase));
        if (index <= 0)
            return;

        var item = paths[index];
        paths.RemoveAt(index);
        paths.Insert(0, item);
    }
}

public sealed class StaticPagesSitemapUrlProvider : ISitemapUrlProvider
{
    public string Segment => "pages";

    public string? RequiredFeature => null;

    public Task<IReadOnlyList<SitemapUrlEntry>> GetEntriesAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        IReadOnlyList<SitemapUrlEntry> entries =
        [
            new("/", today, SitemapChangeFrequency.Daily, 1.0),
            new("/about-us", today, SitemapChangeFrequency.Monthly, 0.8),
            new("/contact-us", today, SitemapChangeFrequency.Monthly, 0.8)
        ];
        return Task.FromResult(entries);
    }
}
