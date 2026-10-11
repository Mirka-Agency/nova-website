using CMS.Application.Common.Paging;
using CMS.Domain.Exceptions;
using CMS.Modules.Seo.Application.Interfaces;
using CMS.Modules.Seo.Application.Redirects;
using CMS.Modules.Seo.Domain.Entities;
using CMS.Modules.Seo.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using DomainValidationException = CMS.Domain.Exceptions.ValidationException;

namespace CMS.Modules.Seo.Infrastructure.Services;

public sealed class SeoRedirectService : ISeoRedirectService
{
    private const string ActiveMapCacheKey = "cms:seo:redirects:active";
    private static readonly TimeSpan ActiveMapCacheDuration = TimeSpan.FromMinutes(5);

    private readonly SeoDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly IValidator<SaveSeoRedirectCommand> _validator;

    public SeoRedirectService(
        SeoDbContext db,
        IMemoryCache cache,
        IValidator<SaveSeoRedirectCommand> validator)
    {
        _db = db;
        _cache = cache;
        _validator = validator;
    }

    public async Task<PagedResult<SeoRedirectListItemDto>> ListPagedAsync(
        SeoRedirectListRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Redirects.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(r => r.FromPath.Contains(term) || r.ToUrl.Contains(term) || (r.Note != null && r.Note.Contains(term)));
        }

        if (request.IsActive.HasValue)
            query = query.Where(r => r.IsActive == request.IsActive.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(r => r.FromPath)
            .Skip(request.Skip)
            .Take(request.NormalizedPageSize)
            .Select(r => new SeoRedirectListItemDto(
                r.Id,
                r.FromPath,
                r.ToUrl,
                r.StatusCode,
                r.IsActive,
                r.Note,
                r.CreatedAtUtc,
                r.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<SeoRedirectListItemDto>(items, total, request.NormalizedPage, request.NormalizedPageSize);
    }

    public async Task<SeoRedirectDetailDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _db.Redirects.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        return item is null ? null : MapDetail(item);
    }

    public async Task<Guid> CreateAsync(SaveSeoRedirectCommand command, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(command, cancellationToken);
        var path = SeoRedirect.NormalizePath(command.FromPath);
        await EnsureUniquePathAsync(path, null, cancellationToken);

        var item = SeoRedirect.Create(command.FromPath, command.ToUrl, command.StatusCode, command.IsActive, command.Note);
        _db.Redirects.Add(item);
        await _db.SaveChangesAsync(cancellationToken);
        InvalidateActiveMap();
        return item.Id;
    }

    public async Task UpdateAsync(Guid id, SaveSeoRedirectCommand command, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(command, cancellationToken);
        var item = await _db.Redirects.FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new NotFoundException("ریدایرکت یافت نشد.");

        var path = SeoRedirect.NormalizePath(command.FromPath);
        await EnsureUniquePathAsync(path, id, cancellationToken);

        item.Update(command.FromPath, command.ToUrl, command.StatusCode, command.IsActive, command.Note);
        await _db.SaveChangesAsync(cancellationToken);
        InvalidateActiveMap();
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _db.Redirects.FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new NotFoundException("ریدایرکت یافت نشد.");
        _db.Redirects.Remove(item);
        await _db.SaveChangesAsync(cancellationToken);
        InvalidateActiveMap();
    }

    public async Task SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        var item = await _db.Redirects.FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new NotFoundException("ریدایرکت یافت نشد.");
        item.SetActive(isActive);
        await _db.SaveChangesAsync(cancellationToken);
        InvalidateActiveMap();
    }

    public async Task<SeoRedirectMatchDto?> ResolveAsync(string path, CancellationToken cancellationToken = default)
    {
        var normalized = SeoRedirect.NormalizePath(path);
        var map = await GetActiveMapAsync(cancellationToken);
        return map.TryGetValue(normalized, out var match) ? match : null;
    }

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        _db.Redirects.AsNoTracking().CountAsync(cancellationToken);

    private async Task<IReadOnlyDictionary<string, SeoRedirectMatchDto>> GetActiveMapAsync(
        CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(ActiveMapCacheKey, out IReadOnlyDictionary<string, SeoRedirectMatchDto>? cached)
            && cached is not null)
        {
            return cached;
        }

        var rows = await _db.Redirects.AsNoTracking()
            .Where(r => r.IsActive)
            .Select(r => new { r.FromPath, r.ToUrl, r.StatusCode })
            .ToListAsync(cancellationToken);

        IReadOnlyDictionary<string, SeoRedirectMatchDto> map = rows.ToDictionary(
            r => r.FromPath,
            r => new SeoRedirectMatchDto(r.ToUrl, r.StatusCode),
            StringComparer.Ordinal);

        _cache.Set(ActiveMapCacheKey, map, ActiveMapCacheDuration);
        return map;
    }

    private void InvalidateActiveMap() => _cache.Remove(ActiveMapCacheKey);

    private async Task EnsureUniquePathAsync(string path, Guid? excludeId, CancellationToken cancellationToken)
    {
        var exists = await _db.Redirects.AsNoTracking()
            .AnyAsync(r => r.FromPath == path && (!excludeId.HasValue || r.Id != excludeId.Value), cancellationToken);
        if (exists)
            throw new DomainException("این مسیر مبدأ قبلاً ثبت شده است.");
    }

    private async Task ValidateAsync(SaveSeoRedirectCommand command, CancellationToken cancellationToken)
    {
        var result = await _validator.ValidateAsync(command, cancellationToken);
        if (result.IsValid)
            return;

        var errors = result.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
        throw new DomainValidationException(errors);
    }

    private static SeoRedirectDetailDto MapDetail(SeoRedirect item) =>
        new(item.Id, item.FromPath, item.ToUrl, item.StatusCode, item.IsActive, item.Note, item.CreatedAtUtc, item.UpdatedAtUtc);
}
