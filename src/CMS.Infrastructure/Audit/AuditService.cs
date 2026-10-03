using CMS.Application.Audit;
using CMS.Application.Common.Paging;
using CMS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CMS.Infrastructure.Audit;

public sealed class AuditService : IAuditLogger, IAuditQueryService
{
    private readonly ApplicationDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(ApplicationDbContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(
        string action,
        string entityType,
        string? entityId = null,
        string? details = null,
        CancellationToken cancellationToken = default)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        var userId = user?.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = user?.FindFirstValue(ClaimTypes.Email) ?? user?.Identity?.Name;

        _db.AuditEntries.Add(AuditEntry.Create(action, entityType, entityId, userId, email, details));
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditEntryDto>> ListRecentAsync(int take = 100, CancellationToken cancellationToken = default)
    {
        var limit = Math.Clamp(take, 1, 500);
        return await _db.AuditEntries
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(limit)
            .Select(x => new AuditEntryDto(
                x.Id,
                x.Action,
                x.EntityType,
                x.EntityId,
                x.UserEmail,
                x.Details,
                x.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<AuditEntryDto>> ListPagedAsync(
        PagedRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedPage = request.NormalizedPage;
        var pageSize = request.NormalizedPageSize;
        var totalCount = await _db.AuditEntries.CountAsync(cancellationToken);
        var items = await _db.AuditEntries
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip(request.Skip)
            .Take(pageSize)
            .Select(x => new AuditEntryDto(
                x.Id,
                x.Action,
                x.EntityType,
                x.EntityId,
                x.UserEmail,
                x.Details,
                x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditEntryDto>(items, totalCount, normalizedPage, pageSize);
    }
}
