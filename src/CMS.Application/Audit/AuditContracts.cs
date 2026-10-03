using CMS.Application.Common.Paging;

namespace CMS.Application.Audit;

public sealed record AuditEntryDto(
    Guid Id,
    string Action,
    string EntityType,
    string? EntityId,
    string? UserEmail,
    string? Details,
    DateTime CreatedAtUtc);

public interface IAuditLogger
{
    Task LogAsync(
        string action,
        string entityType,
        string? entityId = null,
        string? details = null,
        CancellationToken cancellationToken = default);
}

public interface IAuditQueryService
{
    Task<IReadOnlyList<AuditEntryDto>> ListRecentAsync(int take = 100, CancellationToken cancellationToken = default);

    Task<PagedResult<AuditEntryDto>> ListPagedAsync(PagedRequest request, CancellationToken cancellationToken = default);
}
