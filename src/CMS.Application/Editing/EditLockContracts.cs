namespace CMS.Application.Editing;

public static class EditLockEntityTypes
{
    public const string Product = "product";
    public const string ShopCategory = "shop-category";
    public const string BlogPost = "blog-post";
    public const string NewsArticle = "news-article";
    public const string ServiceItem = "service-item";
    public const string VideoItem = "video-item";
    public const string TeamItem = "team-item";
}

public sealed record EditLockInfo(
    string EntityType,
    string EntityId,
    string UserId,
    string UserDisplayName,
    DateTimeOffset AcquiredAtUtc,
    string LockToken);

public sealed record EditLockAcquireResult(bool Acquired, EditLockInfo Lock);

/// <summary>
/// Soft edit locks (WordPress-style). Locks expire automatically after the TTL
/// when the holder stops sending heartbeats (closed tab, lost connection, etc.).
/// </summary>
public interface IEditLockService
{
    TimeSpan LockTtl { get; }

    Task<EditLockAcquireResult> TryAcquireAsync(
        string entityType,
        string entityId,
        string userId,
        string userDisplayName,
        CancellationToken cancellationToken = default);

    Task<bool> HeartbeatAsync(
        string entityType,
        string entityId,
        string userId,
        string lockToken,
        CancellationToken cancellationToken = default);

    Task ReleaseAsync(
        string entityType,
        string entityId,
        string userId,
        string lockToken,
        CancellationToken cancellationToken = default);

    Task<EditLockInfo?> GetAsync(
        string entityType,
        string entityId,
        CancellationToken cancellationToken = default);

    Task<bool> IsHeldByAsync(
        string entityType,
        string entityId,
        string userId,
        string? lockToken = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Resolves the current admin user and acquires/checks/releases edit locks.
/// </summary>
public interface IAdminEditLockAccessor
{
    string? CurrentUserId { get; }

    Task<EditLockAcquireResult?> TryAcquireForCurrentUserAsync(
        string entityType,
        Guid entityId,
        CancellationToken cancellationToken = default);

    Task<bool> CurrentUserHoldsAsync(
        string entityType,
        Guid entityId,
        string? lockToken = null,
        CancellationToken cancellationToken = default);

    Task<bool> HeartbeatForCurrentUserAsync(
        string entityType,
        Guid entityId,
        string lockToken,
        CancellationToken cancellationToken = default);

    Task ReleaseForCurrentUserAsync(
        string entityType,
        Guid entityId,
        string lockToken,
        CancellationToken cancellationToken = default);
}

/// <summary>Applies lock state onto MVC ViewBag (dynamic).</summary>
public static class EditLockViewBag
{
    public static void Apply(dynamic viewBag, EditLockAcquireResult result, string entityType, Guid entityId)
    {
        viewBag.EditLockOwned = result.Acquired;
        viewBag.EditLockToken = result.Acquired ? result.Lock.LockToken : null;
        viewBag.EditLockEntityType = entityType;
        viewBag.EditLockEntityId = entityId.ToString("D");
        viewBag.EditLockLockedByName = result.Acquired ? null : result.Lock.UserDisplayName;
    }
}
