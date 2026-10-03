using CMS.Application.Common.Paging;
using CMS.Domain.Exceptions;
using CMS.Modules.Honors.Application.HonorItems;
using CMS.Modules.Honors.Application.Interfaces;
using CMS.Modules.Honors.Domain.Entities;
using CMS.Modules.Honors.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using DomainValidationException = CMS.Domain.Exceptions.ValidationException;

namespace CMS.Modules.Honors.Infrastructure.Services;

public sealed class HonorItemService : IHonorItemService
{
    private readonly HonorsDbContext _db;
    private readonly IValidator<SaveHonorItemCommand> _validator;

    public HonorItemService(HonorsDbContext db, IValidator<SaveHonorItemCommand> validator)
    {
        _db = db;
        _validator = validator;
    }

    public async Task<PagedResult<HonorItemListItemDto>> ListPagedAsync(
        HonorItemListRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _db.HonorItems.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(h => h.Title.Contains(term) || (h.AltText != null && h.AltText.Contains(term)));
        }

        if (request.IsPublished.HasValue)
            query = query.Where(h => h.IsPublished == request.IsPublished.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(h => h.SortOrder)
            .ThenByDescending(h => h.CreatedAtUtc)
            .Skip(request.Skip)
            .Take(request.NormalizedPageSize)
            .Select(h => new HonorItemListItemDto(
                h.Id,
                h.Title,
                h.ImageUrl,
                h.AltText,
                h.SortOrder,
                h.IsPublished,
                h.CreatedAtUtc,
                h.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<HonorItemListItemDto>(items, total, request.NormalizedPage, request.NormalizedPageSize);
    }

    public async Task<HonorItemDetailDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _db.HonorItems.AsNoTracking().FirstOrDefaultAsync(h => h.Id == id, cancellationToken);
        return item is null ? null : MapDetail(item);
    }

    public async Task<Guid> CreateAsync(SaveHonorItemCommand command, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(command, cancellationToken);
        var item = HonorItem.Create(
            command.Title,
            command.ImageUrl,
            command.AltText,
            command.SortOrder,
            command.IsPublished);
        _db.HonorItems.Add(item);
        await _db.SaveChangesAsync(cancellationToken);
        return item.Id;
    }

    public async Task UpdateAsync(Guid id, SaveHonorItemCommand command, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(command, cancellationToken);
        var item = await _db.HonorItems.FirstOrDefaultAsync(h => h.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(HonorItem), id);

        item.Update(
            command.Title,
            command.ImageUrl,
            command.AltText,
            command.SortOrder,
            command.IsPublished);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _db.HonorItems.FirstOrDefaultAsync(h => h.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(HonorItem), id);
        _db.HonorItems.Remove(item);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task ReorderAsync(IReadOnlyList<Guid> orderedIds, CancellationToken cancellationToken = default)
    {
        if (orderedIds.Count == 0)
            return;

        var items = await _db.HonorItems
            .Where(h => orderedIds.Contains(h.Id))
            .ToListAsync(cancellationToken);

        for (var i = 0; i < orderedIds.Count; i++)
        {
            var item = items.FirstOrDefault(h => h.Id == orderedIds[i]);
            item?.SetSortOrder(i);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        _db.HonorItems.AsNoTracking().CountAsync(cancellationToken);

    private static HonorItemDetailDto MapDetail(HonorItem h) =>
        new(
            h.Id,
            h.Title,
            h.ImageUrl,
            h.AltText,
            h.SortOrder,
            h.IsPublished,
            h.PublishedAtUtc,
            h.CreatedAtUtc,
            h.UpdatedAtUtc);

    private async Task ValidateAsync(SaveHonorItemCommand command, CancellationToken cancellationToken)
    {
        var result = await _validator.ValidateAsync(command, cancellationToken);
        if (!result.IsValid)
        {
            throw new DomainValidationException(result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
        }
    }
}
