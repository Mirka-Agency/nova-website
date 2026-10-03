using CMS.Application.Common.Paging;
using CMS.Domain.Exceptions;
using CMS.Modules.Voices.Application.Interfaces;
using CMS.Modules.Voices.Application.VoiceItems;
using CMS.Modules.Voices.Domain.Entities;
using CMS.Modules.Voices.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using DomainValidationException = CMS.Domain.Exceptions.ValidationException;

namespace CMS.Modules.Voices.Infrastructure.Services;

public sealed class VoiceItemService : IVoiceItemService
{
    private readonly VoicesDbContext _db;
    private readonly IValidator<SaveVoiceItemCommand> _validator;

    public VoiceItemService(VoicesDbContext db, IValidator<SaveVoiceItemCommand> validator)
    {
        _db = db;
        _validator = validator;
    }

    public async Task<PagedResult<VoiceItemListItemDto>> ListPagedAsync(
        VoiceItemListRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _db.VoiceItems.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(v =>
                v.CustomerName.Contains(term)
                || (v.Subtitle != null && v.Subtitle.Contains(term))
                || (v.Description != null && v.Description.Contains(term)));
        }

        if (request.IsPublished.HasValue)
            query = query.Where(v => v.IsPublished == request.IsPublished.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(v => v.SortOrder)
            .ThenByDescending(v => v.CreatedAtUtc)
            .Skip(request.Skip)
            .Take(request.NormalizedPageSize)
            .Select(v => new VoiceItemListItemDto(
                v.Id,
                v.CustomerName,
                v.Subtitle,
                v.AudioUrl,
                v.SortOrder,
                v.IsPublished,
                v.CreatedAtUtc,
                v.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<VoiceItemListItemDto>(items, total, request.NormalizedPage, request.NormalizedPageSize);
    }

    public async Task<VoiceItemDetailDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _db.VoiceItems.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
        return item is null ? null : MapDetail(item);
    }

    public async Task<Guid> CreateAsync(SaveVoiceItemCommand command, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(command, cancellationToken);
        var item = VoiceItem.Create(
            command.CustomerName,
            command.Subtitle,
            command.Description,
            command.AudioUrl,
            command.SortOrder,
            command.IsPublished);
        _db.VoiceItems.Add(item);
        await _db.SaveChangesAsync(cancellationToken);
        return item.Id;
    }

    public async Task UpdateAsync(Guid id, SaveVoiceItemCommand command, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(command, cancellationToken);
        var item = await _db.VoiceItems.FirstOrDefaultAsync(v => v.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(VoiceItem), id);

        item.Update(
            command.CustomerName,
            command.Subtitle,
            command.Description,
            command.AudioUrl,
            command.SortOrder,
            command.IsPublished);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _db.VoiceItems.FirstOrDefaultAsync(v => v.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(VoiceItem), id);
        _db.VoiceItems.Remove(item);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task ReorderAsync(IReadOnlyList<Guid> orderedIds, CancellationToken cancellationToken = default)
    {
        if (orderedIds.Count == 0)
            return;

        var items = await _db.VoiceItems
            .Where(v => orderedIds.Contains(v.Id))
            .ToListAsync(cancellationToken);

        for (var i = 0; i < orderedIds.Count; i++)
        {
            var item = items.FirstOrDefault(v => v.Id == orderedIds[i]);
            item?.SetSortOrder(i);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        _db.VoiceItems.AsNoTracking().CountAsync(cancellationToken);

    private static VoiceItemDetailDto MapDetail(VoiceItem v) =>
        new(
            v.Id,
            v.CustomerName,
            v.Subtitle,
            v.Description,
            v.AudioUrl,
            v.SortOrder,
            v.IsPublished,
            v.PublishedAtUtc,
            v.CreatedAtUtc,
            v.UpdatedAtUtc);

    private async Task ValidateAsync(SaveVoiceItemCommand command, CancellationToken cancellationToken)
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
