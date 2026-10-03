using System.Security.Claims;
using CMS.Application.Editing;
using CMS.Application.Users;
using Microsoft.AspNetCore.Http;

namespace CMS.Infrastructure.Editing;

public sealed class AdminEditLockAccessor : IAdminEditLockAccessor
{
    private readonly IEditLockService _editLocks;
    private readonly IContentAuthorLookup _authors;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AdminEditLockAccessor(
        IEditLockService editLocks,
        IContentAuthorLookup authors,
        IHttpContextAccessor httpContextAccessor)
    {
        _editLocks = editLocks;
        _authors = authors;
        _httpContextAccessor = httpContextAccessor;
    }

    public string? CurrentUserId =>
        _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

    public async Task<EditLockAcquireResult?> TryAcquireForCurrentUserAsync(
        string entityType,
        Guid entityId,
        CancellationToken cancellationToken = default)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
            return null;

        var displayName = await _authors.GetDisplayNameAsync(userId, cancellationToken)
            ?? _httpContextAccessor.HttpContext?.User.Identity?.Name
            ?? userId;

        return await _editLocks.TryAcquireAsync(
            entityType,
            entityId.ToString("D"),
            userId,
            displayName,
            cancellationToken);
    }

    public Task<bool> CurrentUserHoldsAsync(
        string entityType,
        Guid entityId,
        string? lockToken = null,
        CancellationToken cancellationToken = default)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
            return Task.FromResult(false);

        return _editLocks.IsHeldByAsync(
            entityType,
            entityId.ToString("D"),
            userId,
            lockToken,
            cancellationToken);
    }

    public async Task<bool> HeartbeatForCurrentUserAsync(
        string entityType,
        Guid entityId,
        string lockToken,
        CancellationToken cancellationToken = default)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
            return false;

        return await _editLocks.HeartbeatAsync(
            entityType,
            entityId.ToString("D"),
            userId,
            lockToken,
            cancellationToken);
    }

    public async Task ReleaseForCurrentUserAsync(
        string entityType,
        Guid entityId,
        string lockToken,
        CancellationToken cancellationToken = default)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
            return;

        await _editLocks.ReleaseAsync(
            entityType,
            entityId.ToString("D"),
            userId,
            lockToken,
            cancellationToken);
    }
}
