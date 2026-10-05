using Microsoft.Extensions.Caching.Memory;

namespace CMS.Application.Caching;

/// <summary>
/// Short-lived memory cache for anonymous public content queries.
/// TTL keeps pages fast while content changes appear within about a minute.
/// </summary>
public static class PublicContentCache
{
    public static readonly TimeSpan DefaultDuration = TimeSpan.FromSeconds(90);

    public static async Task<T> GetOrCreateAsync<T>(
        IMemoryCache cache,
        string key,
        Func<CancellationToken, Task<T>> factory,
        CancellationToken cancellationToken,
        TimeSpan? duration = null)
        where T : class
    {
        if (cache.TryGetValue(key, out T? cached) && cached is not null)
            return cached;

        var value = await factory(cancellationToken);
        cache.Set(key, value, duration ?? DefaultDuration);
        return value;
    }

    public static async Task<T?> GetOrCreateNullableAsync<T>(
        IMemoryCache cache,
        string key,
        Func<CancellationToken, Task<T?>> factory,
        CancellationToken cancellationToken,
        TimeSpan? duration = null)
        where T : class
    {
        if (cache.TryGetValue(key, out CacheBox<T>? box) && box is not null)
            return box.Value;

        var value = await factory(cancellationToken);
        cache.Set(key, new CacheBox<T>(value), duration ?? DefaultDuration);
        return value;
    }

    private sealed class CacheBox<T>(T? value) where T : class
    {
        public T? Value { get; } = value;
    }
}
