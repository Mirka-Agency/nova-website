using CMS.Application.Common.Paging;
using CMS.Domain.Exceptions;
using CMS.Modules.Seo.Application.Interfaces;
using CMS.Modules.Seo.Application.Redirects;
using CMS.Modules.Seo.Domain.Entities;
using CMS.Modules.Seo.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using DomainValidationException = CMS.Domain.Exceptions.ValidationException;

namespace CMS.Modules.Seo.Infrastructure.Services;

public sealed class SeoRedirectService : ISeoRedirectService
{
    private readonly SeoDbContext _db;
    private readonly IValidator<SaveSeoRedirectCommand> _validator;

    public SeoRedirectService(SeoDbContext db, IValidator<SaveSeoRedirectCommand> validator)
    {
        _db = db;
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
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _db.Redirects.FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new NotFoundException("ریدایرکت یافت نشد.");
        _db.Redirects.Remove(item);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        var item = await _db.Redirects.FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new NotFoundException("ریدایرکت یافت نشد.");
        item.SetActive(isActive);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<SeoRedirectMatchDto?> ResolveAsync(string path, CancellationToken cancellationToken = default)
    {
        var normalized = SeoRedirect.NormalizePath(path);
        var match = await _db.Redirects.AsNoTracking()
            .Where(r => r.IsActive && r.FromPath == normalized)
            .Select(r => new SeoRedirectMatchDto(r.ToUrl, r.StatusCode))
            .FirstOrDefaultAsync(cancellationToken);
        return match;
    }

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        _db.Redirects.AsNoTracking().CountAsync(cancellationToken);

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
