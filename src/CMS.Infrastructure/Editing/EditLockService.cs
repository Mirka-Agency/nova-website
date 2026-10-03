using System.Text.Json;
using CMS.Application.Editing;
using Microsoft.Extensions.Caching.Distributed;

namespace CMS.Infrastructure.Editing;

public sealed class EditLockService : IEditLockService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IDistributedCache _cache;

    public EditLockService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public TimeSpan LockTtl { get; } = TimeSpan.FromMinutes(1);

    public async Task<EditLockAcquireResult> TryAcquireAsync(
        string entityType,
        string entityId,
        string userId,
        string userDisplayName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityType);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var existing = await GetAsync(entityType, entityId, cancellationToken);
        if (existing is not null
            && !string.Equals(existing.UserId, userId, StringComparison.Ordinal))
        {
            return new EditLockAcquireResult(false, existing);
        }

        var lockInfo = new EditLockInfo(
            entityType.Trim(),
            entityId.Trim(),
            userId,
            string.IsNullOrWhiteSpace(userDisplayName) ? userId : userDisplayName.Trim(),
            DateTimeOffset.UtcNow,
            Guid.NewGuid().ToString("N"));

        await SetAsync(lockInfo, cancellationToken);

        // Mitigate a rare race: another writer may have overwritten immediately after us.
        var verified = await GetAsync(entityType, entityId, cancellationToken);
        if (verified is null
            || !string.Equals(verified.LockToken, lockInfo.LockToken, StringComparison.Ordinal)
            || !string.Equals(verified.UserId, userId, StringComparison.Ordinal))
        {
            return new EditLockAcquireResult(false, verified ?? lockInfo);
        }

        return new EditLockAcquireResult(true, lockInfo);
    }

    public async Task<bool> HeartbeatAsync(
        string entityType,
        string entityId,
        string userId,
        string lockToken,
        CancellationToken cancellationToken = default)
    {
        var existing = await GetAsync(entityType, entityId, cancellationToken);
        if (existing is null
            || !string.Equals(existing.UserId, userId, StringComparison.Ordinal)
            || !string.Equals(existing.LockToken, lockToken, StringComparison.Ordinal))
        {
            return false;
        }

        var refreshed = existing with { AcquiredAtUtc = DateTimeOffset.UtcNow };
        await SetAsync(refreshed, cancellationToken);
        return true;
    }

    public async Task ReleaseAsync(
        string entityType,
        string entityId,
        string userId,
        string lockToken,
        CancellationToken cancellationToken = default)
    {
        var existing = await GetAsync(entityType, entityId, cancellationToken);
        if (existing is null)
            return;

        if (!string.Equals(existing.UserId, userId, StringComparison.Ordinal)
            || !string.Equals(existing.LockToken, lockToken, StringComparison.Ordinal))
        {
            return;
        }

        await _cache.RemoveAsync(CacheKey(entityType, entityId), cancellationToken);
    }

    public async Task<EditLockInfo?> GetAsync(
        string entityType,
        string entityId,
        CancellationToken cancellationToken = default)
    {
        var json = await _cache.GetStringAsync(CacheKey(entityType, entityId), cancellationToken);
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<EditLockInfo>(json, JsonOptions);
        }
        catch (JsonException)
        {
            await _cache.RemoveAsync(CacheKey(entityType, entityId), cancellationToken);
            return null;
        }
    }

    public async Task<bool> IsHeldByAsync(
        string entityType,
        string entityId,
        string userId,
        string? lockToken = null,
        CancellationToken cancellationToken = default)
    {
        var existing = await GetAsync(entityType, entityId, cancellationToken);
        if (existing is null
            || !string.Equals(existing.UserId, userId, StringComparison.Ordinal))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(lockToken))
            return true;

        return string.Equals(existing.LockToken, lockToken, StringComparison.Ordinal);
    }

    private async Task SetAsync(EditLockInfo lockInfo, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(lockInfo, JsonOptions);
        await _cache.SetStringAsync(
            CacheKey(lockInfo.EntityType, lockInfo.EntityId),
            json,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = LockTtl
            },
            cancellationToken);
    }

    private static string CacheKey(string entityType, string entityId) =>
        $"edit-lock:{entityType.Trim().ToLowerInvariant()}:{entityId.Trim().ToLowerInvariant()}";
}
