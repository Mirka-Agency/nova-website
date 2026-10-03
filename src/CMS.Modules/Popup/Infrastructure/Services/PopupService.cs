using CMS.Application.Common.Paging;
using CMS.Application.Security;
using CMS.Domain.Exceptions;
using CMS.Modules.Popup.Application.Interfaces;
using CMS.Modules.Popup.Application.Popups;
using CMS.Modules.Popup.Domain.Entities;
using CMS.Modules.Popup.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using DomainValidationException = CMS.Domain.Exceptions.ValidationException;

namespace CMS.Modules.Popup.Infrastructure.Services;

public sealed class PopupService : IPopupService
{
    private readonly PopupDbContext _db;
    private readonly IValidator<SavePopupCommand> _validator;
    private readonly IHtmlContentSanitizer _htmlSanitizer;

    public PopupService(
        PopupDbContext db,
        IValidator<SavePopupCommand> validator,
        IHtmlContentSanitizer htmlSanitizer)
    {
        _db = db;
        _validator = validator;
        _htmlSanitizer = htmlSanitizer;
    }

    public async Task<PagedResult<PopupListItemDto>> ListPagedAsync(
        PopupListRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Popups.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(p => p.Title.Contains(term) || p.Slug.Contains(term));
        }

        if (request.IsActive.HasValue)
            query = query.Where(p => p.IsActive == request.IsActive.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(p => p.SortOrder)
            .ThenByDescending(p => p.CreatedAtUtc)
            .Skip(request.Skip)
            .Take(request.NormalizedPageSize)
            .Select(p => new PopupListItemDto(
                p.Id,
                p.Title,
                p.Slug,
                p.IsActive,
                p.TriggerType,
                p.Frequency,
                p.PageTargetMode,
                p.FormId,
                p.CreatedAtUtc,
                p.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<PopupListItemDto>(items, total, request.NormalizedPage, request.NormalizedPageSize);
    }

    public async Task<IReadOnlyList<PopupOptionDto>> ListOptionsAsync(
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Popups.AsNoTracking().AsQueryable();
        if (excludeId.HasValue)
            query = query.Where(p => p.Id != excludeId.Value);

        return await query
            .OrderBy(p => p.Title)
            .Select(p => new PopupOptionDto(p.Id, p.Title, p.Slug))
            .ToListAsync(cancellationToken);
    }

    public async Task<PopupDetailDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _db.Popups.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        return item is null ? null : MapDetail(item);
    }

    public async Task<Guid> CreateAsync(SavePopupCommand command, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(command, cancellationToken);
        await EnsureCtaTargetExistsAsync(command, null, cancellationToken);
        var slug = await EnsureUniqueSlugAsync(ResolveSlug(command), null, cancellationToken);
        var (content, behavior) = MapCommand(command, slug);
        var item = PopupItem.Create(content, behavior, command.SortOrder);
        _db.Popups.Add(item);
        await _db.SaveChangesAsync(cancellationToken);
        return item.Id;
    }

    public async Task UpdateAsync(Guid id, SavePopupCommand command, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(command, cancellationToken);
        await EnsureCtaTargetExistsAsync(command, id, cancellationToken);
        var item = await _db.Popups.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(PopupItem), id);

        var slug = await EnsureUniqueSlugAsync(ResolveSlug(command), id, cancellationToken);
        var (content, behavior) = MapCommand(command, slug);
        item.Update(content, behavior, command.SortOrder);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _db.Popups.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(PopupItem), id);
        _db.Popups.Remove(item);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ToggleActiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _db.Popups.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(PopupItem), id);
        var isActive = !item.IsActive;
        item.SetActive(isActive);
        await _db.SaveChangesAsync(cancellationToken);
        return isActive;
    }

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        _db.Popups.AsNoTracking().CountAsync(cancellationToken);

    private (PopupContent Content, PopupBehavior Behavior) MapCommand(SavePopupCommand command, string slug)
    {
        var contentHtml = _htmlSanitizer.Sanitize(command.ContentHtml ?? string.Empty);
        var content = new PopupContent(
            command.Title,
            slug,
            command.BodyText,
            command.ImageUrl,
            contentHtml,
            command.IsActive,
            command.FormId,
            command.CtaText,
            command.CtaAction,
            command.CtaUrl,
            command.CtaTargetPopupId,
            command.TriggerType,
            command.TriggerDelaySeconds,
            command.TriggerScrollPercent,
            command.TriggerSelector,
            command.TriggerConfigJson);

        var behavior = new PopupBehavior(
            command.ShowOverlay,
            command.ShowCloseButton,
            command.CloseOnOverlayClick,
            command.CloseOnEscape,
            command.LockBodyScroll,
            command.EnableContentScroll,
            command.Frequency,
            command.PageTargetMode,
            command.PagePaths,
            command.ExtensionSettingsJson);

        return (content, behavior);
    }

    private async Task EnsureCtaTargetExistsAsync(
        SavePopupCommand command,
        Guid? currentId,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(command.CtaAction, Domain.Enums.PopupCtaActions.OpenPopup, StringComparison.OrdinalIgnoreCase)
            || command.CtaTargetPopupId is null)
            return;

        if (currentId.HasValue && command.CtaTargetPopupId == currentId)
            throw new DomainValidationException(new Dictionary<string, string[]>
            {
                [nameof(command.CtaTargetPopupId)] = ["پاپ‌آپ نمی‌تواند خودش را باز کند."]
            });

        var exists = await _db.Popups.AnyAsync(p => p.Id == command.CtaTargetPopupId, cancellationToken);
        if (!exists)
            throw new DomainValidationException(new Dictionary<string, string[]>
            {
                [nameof(command.CtaTargetPopupId)] = ["پاپ‌آپ مقصد یافت نشد."]
            });
    }

    private static PopupDetailDto MapDetail(PopupItem p) =>
        new(
            p.Id,
            p.Title,
            p.Slug,
            p.BodyText,
            p.ImageUrl,
            p.ContentHtml,
            p.IsActive,
            p.FormId,
            p.CtaText,
            p.CtaAction,
            p.CtaUrl,
            p.CtaTargetPopupId,
            p.TriggerType,
            p.TriggerDelaySeconds,
            p.TriggerScrollPercent,
            p.TriggerSelector,
            p.TriggerConfigJson,
            p.ShowOverlay,
            p.ShowCloseButton,
            p.CloseOnOverlayClick,
            p.CloseOnEscape,
            p.LockBodyScroll,
            p.EnableContentScroll,
            p.Frequency,
            p.PageTargetMode,
            p.PagePaths,
            p.ExtensionSettingsJson,
            p.SortOrder,
            p.CreatedAtUtc,
            p.UpdatedAtUtc);

    private async Task ValidateAsync(SavePopupCommand command, CancellationToken cancellationToken)
    {
        var result = await _validator.ValidateAsync(command, cancellationToken);
        if (!result.IsValid)
        {
            throw new DomainValidationException(result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
        }
    }

    private static string ResolveSlug(SavePopupCommand command)
    {
        var slug = Application.Common.SlugGenerator.FromTitle(command.Slug ?? string.Empty, 200);
        if (string.IsNullOrWhiteSpace(slug))
            slug = Application.Common.SlugGenerator.FromTitle(command.Title, 200);

        if (string.IsNullOrWhiteSpace(slug))
            throw new DomainValidationException(new Dictionary<string, string[]>
            {
                [nameof(command.Slug)] = ["نامک الزامی است."]
            });

        return slug;
    }

    private async Task<string> EnsureUniqueSlugAsync(string slug, Guid? excludeId, CancellationToken cancellationToken)
    {
        var candidate = slug;
        var suffix = 2;
        while (await _db.Popups.AnyAsync(
                   p => p.Slug == candidate && (!excludeId.HasValue || p.Id != excludeId.Value),
                   cancellationToken))
        {
            candidate = $"{slug}-{suffix}";
            suffix++;
        }

        return candidate;
    }
}
