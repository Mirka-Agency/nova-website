using System.Text.Json;
using CMS.Modules.Video.Application.Interfaces;
using CMS.Modules.Video.Application.VideoItems;
using Microsoft.Extensions.Caching.Memory;

namespace CMS.Modules.Video.Infrastructure.Services;

public sealed class VideoThumbnailResolver : IVideoThumbnailResolver
{
    private static readonly TimeSpan PositiveCacheTtl = TimeSpan.FromHours(24);
    private static readonly TimeSpan NegativeCacheTtl = TimeSpan.FromMinutes(30);
    private const string HttpClientName = "AparatThumbnail";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;

    public VideoThumbnailResolver(IHttpClientFactory httpClientFactory, IMemoryCache cache)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
    }

    public async Task<string?> ResolveAsync(string? videoUrl, CancellationToken cancellationToken = default)
    {
        var sync = VideoThumbnailUrl.Resolve(videoUrl);
        if (!string.IsNullOrWhiteSpace(sync))
            return sync;

        var aparatHash = VideoThumbnailUrl.TryGetAparatHash(videoUrl);
        if (string.IsNullOrWhiteSpace(aparatHash))
            return null;

        var cacheKey = $"cms:video:thumb:aparat:{aparatHash.ToLowerInvariant()}";
        if (_cache.TryGetValue(cacheKey, out string? cached))
            return string.IsNullOrWhiteSpace(cached) ? null : cached;

        var poster = await FetchAparatPosterAsync(aparatHash, cancellationToken);
        _cache.Set(
            cacheKey,
            poster ?? string.Empty,
            poster is null ? NegativeCacheTtl : PositiveCacheTtl);

        return poster;
    }

    private async Task<string?> FetchAparatPosterAsync(string hash, CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var response = await client.GetAsync(
                $"https://www.aparat.com/etc/api/video/videohash/{Uri.EscapeDataString(hash)}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
                return null;

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (!doc.RootElement.TryGetProperty("video", out var video))
                return null;

            if (video.TryGetProperty("big_poster", out var big) && big.ValueKind == JsonValueKind.String)
            {
                var url = big.GetString();
                if (!string.IsNullOrWhiteSpace(url))
                    return url.Trim();
            }

            if (video.TryGetProperty("small_poster", out var small) && small.ValueKind == JsonValueKind.String)
            {
                var url = small.GetString();
                if (!string.IsNullOrWhiteSpace(url))
                    return url.Trim();
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // HttpClient timeout
        }
        catch (HttpRequestException)
        {
        }
        catch (JsonException)
        {
        }

        return null;
    }
}
